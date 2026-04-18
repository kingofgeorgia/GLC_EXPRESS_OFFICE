using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace GLC_EXPRESS.Services
{
    public static class AuthService
    {
        private const string UserKeyPrefix = "Auth.User.";
        private const string DefaultUsernameKey = "Auth.DefaultUsername";
        private const string DefaultPasswordHashKey = "Auth.DefaultPasswordHash";

        public static bool ValidateCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            var normalizedUsername = username.Trim();
            var passwordHash = ComputeSha256(password);

            if (ValidateConfiguredUsers(normalizedUsername, passwordHash))
            {
                return true;
            }

            return ValidateLegacyUser(normalizedUsername, passwordHash);
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
    }
}
