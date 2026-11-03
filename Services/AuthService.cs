using System;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using GLC_EXPRESS.Models;

namespace GLC_EXPRESS.Services
{
    public static class AuthService
    {
        private const string UserKeyPrefix = "Auth.User.";
        private const string UserRolesKeyPrefix = "Auth.UserRoles.";
        private const string DefaultUsernameKey = "Auth.DefaultUsername";
        private const string DefaultPasswordHashKey = "Auth.DefaultPasswordHash";
        private const string DefaultRoles = "Admin";
        public const string PublicClientRole = "Client";
        private const int PublicPasswordIterations = 180000;

        public static bool ValidateCredentials(string username, string password)
        {
            AuthUserRecord ignoredUser;
            return TryAuthenticate(username, password, out ignoredUser);
        }

        public static bool TryAuthenticate(string username, string password, out AuthUserRecord user)
        {
            user = null;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            var normalizedUsername = username.Trim();
            var passwordHash = ComputeSha256(password);
            user = FindActiveUser(normalizedUsername);

            if (user != null && VerifyPassword(user, password, passwordHash))
            {
                return true;
            }

            if (ValidateConfiguredUsers(normalizedUsername, passwordHash))
            {
                user = FindActiveUser(normalizedUsername);
                return true;
            }

            return ValidateLegacyUser(normalizedUsername, passwordHash);
        }

        public static void RegisterPublicAccount(string fullName, string email, string phone, string password)
        {
            if (!CrmSettingsService.IsPublicRegistrationEnabled())
            {
                throw new InvalidOperationException("Публичная регистрация сейчас отключена.");
            }

            var normalizedName = (fullName ?? string.Empty).Trim();
            var normalizedEmail = (email ?? string.Empty).Trim();
            var normalizedPhone = (phone ?? string.Empty).Trim();
            var emailKey = NormalizeLookupValue(normalizedEmail);

            if (normalizedName.Length < 2 || normalizedName.Length > 120)
            {
                throw new ArgumentException("Укажите имя длиной от 2 до 120 символов.");
            }

            if (normalizedEmail.Length > 254 || !System.Text.RegularExpressions.Regex.IsMatch(
                normalizedEmail,
                @"^[^\s@]+@[^\s@]+\.[^\s@]+$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                throw new ArgumentException("Укажите корректный email.");
            }

            if (normalizedPhone.Length > 40)
            {
                throw new ArgumentException("Номер телефона не должен превышать 40 символов.");
            }

            if (string.IsNullOrEmpty(password) || password.Length < 10 || password.Length > 128)
            {
                throw new ArgumentException("Пароль должен содержать от 10 до 128 символов.");
            }

            var saltBytes = new byte[16];
            using (var random = new RNGCryptoServiceProvider())
            {
                random.GetBytes(saltBytes);
            }

            var salt = Convert.ToBase64String(saltBytes);
            var hash = DerivePasswordHash(password, saltBytes, PublicPasswordIterations);
            var user = new AuthUserRecord
            {
                Username = normalizedEmail,
                UsernameNormalized = emailKey,
                FullName = normalizedName,
                Email = normalizedEmail,
                PasswordHash = hash,
                PasswordSalt = salt,
                PasswordIterations = PublicPasswordIterations,
                Roles = new System.Collections.Generic.List<string> { PublicClientRole },
                IsActive = true
            };

            using (var database = AppDatabase.Open())
            {
                var users = database.GetCollection<AuthUserRecord>("users");
                var clients = database.GetCollection<ClientRecord>("clients");
                if (users.FindOne(item => item.UsernameNormalized == emailKey) != null)
                {
                    throw new InvalidOperationException("Этот email уже зарегистрирован.");
                }

                database.BeginTrans();
                try
                {
                    users.Insert(user);
                    if (!clients.FindAll().Any(item => string.Equals(
                        NormalizeLookupValue(item.Email), emailKey, StringComparison.OrdinalIgnoreCase)))
                    {
                        clients.Insert(new ClientRecord
                        {
                            Name = normalizedName,
                            PhoneNumber = normalizedPhone,
                            Email = normalizedEmail,
                            Direction = "Регистрация на сайте"
                        });
                    }
                    database.Commit();
                }
                catch
                {
                    database.Rollback();
                    throw;
                }
            }
        }

        public static string[] GetRolesForUser(string username)
        {
            var user = FindActiveUser(username);

            if (user == null || user.Roles == null || user.Roles.Count == 0)
            {
                return GetConfiguredRoles(username);
            }

            return user.Roles
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static bool ValidateConfiguredUsers(string username, string passwordHash)
        {
            foreach (var key in ConfigurationManager.AppSettings.AllKeys)
            {
                if (key == null || !key.StartsWith(UserKeyPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var configuredUsername = key.Substring(UserKeyPrefix.Length);
                var configuredPasswordHash = ConfigurationManager.AppSettings[key];

                if (string.IsNullOrWhiteSpace(configuredUsername) || string.IsNullOrWhiteSpace(configuredPasswordHash))
                {
                    continue;
                }

                if (string.Equals(username, configuredUsername, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(passwordHash, configuredPasswordHash, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ValidateLegacyUser(string username, string passwordHash)
        {
            var expectedUsername = ConfigurationManager.AppSettings[DefaultUsernameKey];
            var expectedPasswordHash = ConfigurationManager.AppSettings[DefaultPasswordHashKey];

            if (string.IsNullOrWhiteSpace(expectedUsername) || string.IsNullOrWhiteSpace(expectedPasswordHash))
            {
                return false;
            }

            return string.Equals(username, expectedUsername, StringComparison.OrdinalIgnoreCase)
                && string.Equals(passwordHash, expectedPasswordHash, StringComparison.OrdinalIgnoreCase);
        }

        private static string ComputeSha256(string value)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                var hash = sha256.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);

                foreach (var item in hash)
                {
                    builder.Append(item.ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private static bool VerifyPassword(AuthUserRecord user, string password, string legacyHash)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(user.PasswordSalt))
            {
                return FixedTimeEquals(user.PasswordHash, legacyHash);
            }

            try
            {
                var salt = Convert.FromBase64String(user.PasswordSalt);
                var iterations = user.PasswordIterations > 0 ? user.PasswordIterations : PublicPasswordIterations;
                var candidate = DerivePasswordHash(password, salt, iterations);
                return FixedTimeEquals(user.PasswordHash, candidate);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static string DerivePasswordHash(string password, byte[] salt, int iterations)
        {
            using (var derivation = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                return Convert.ToBase64String(derivation.GetBytes(32));
            }
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            var difference = 0;
            for (var index = 0; index < left.Length; index++)
            {
                difference |= left[index] ^ right[index];
            }

            return difference == 0;
        }

        private static string[] GetConfiguredRoles(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return new string[0];
            }

            var configuredValue = ConfigurationManager.AppSettings[UserRolesKeyPrefix + username.Trim()];
            var rolesValue = string.IsNullOrWhiteSpace(configuredValue) ? DefaultRoles : configuredValue;

            return rolesValue
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static AuthUserRecord FindActiveUser(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            try
            {
                using (var database = AppDatabase.Open())
                {
                    var users = database.GetCollection<AuthUserRecord>("users");
                    var normalizedUsername = NormalizeLookupValue(username);
                    var user = users.FindOne(item => item.UsernameNormalized == normalizedUsername);

                    if (user == null || !user.IsActive)
                    {
                        return null;
                    }

                    return user;
                }
            }
            catch
            {
                return null;
            }
        }

        private static string NormalizeLookupValue(string value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }
    }
}
