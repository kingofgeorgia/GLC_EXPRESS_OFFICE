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

            if (user != null && string.Equals(user.PasswordHash, passwordHash, StringComparison.OrdinalIgnoreCase))
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
