using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using GLC_EXPRESS.Models;
using LiteDB;
using Newtonsoft.Json;

namespace GLC_EXPRESS.Services
{
    public static class AppDatabase
    {
        private const string UserKeyPrefix = "Auth.User.";
        private const string UserRolesKeyPrefix = "Auth.UserRoles.";
        private const string DefaultUsernameKey = "Auth.DefaultUsername";
        private const string DefaultPasswordHashKey = "Auth.DefaultPasswordHash";
        private const string DefaultRoles = "Admin";

        private static readonly object SyncRoot = new object();

        public static LiteDatabase Open()
        {
            EnsureInitialized();
            return new LiteDatabase(DatabaseFilePath);
        }

        public static void EnsureInitialized()
        {
            lock (SyncRoot)
            {
                var directoryPath = Path.GetDirectoryName(DatabaseFilePath) ?? AppDomain.CurrentDomain.BaseDirectory;
                Directory.CreateDirectory(directoryPath);

                using (var database = new LiteDatabase(DatabaseFilePath))
                {
                    EnsureIndexes(database);
                    SeedUsers(database);
                    MigrateLegacyCrmData(database);
                }
            }
        }

        public static string DatabaseFilePath
        {
            get
            {
                return ResolveAppDataPath("glc-express-office.db");
            }
        }

        private static void EnsureIndexes(LiteDatabase database)
        {
            var users = database.GetCollection<AuthUserRecord>("users");
            users.EnsureIndex(item => item.UsernameNormalized, true);

            database.GetCollection<ClientRecord>("clients").EnsureIndex(item => item.Id, true);
            database.GetCollection<DriverRecord>("drivers").EnsureIndex(item => item.Id, true);
            database.GetCollection<FleetVehicleRecord>("fleet").EnsureIndex(item => item.Id, true);
            database.GetCollection<TripRecord>("trips").EnsureIndex(item => item.Id, true);
        }

        private static void SeedUsers(LiteDatabase database)
        {
            var users = database.GetCollection<AuthUserRecord>("users");
            var existingUsers = users.FindAll().ToList();
            var configuredUsers = GetConfiguredUsers();

            foreach (var configuredUser in configuredUsers)
            {
                var existingUser = existingUsers.FirstOrDefault(item => string.Equals(item.UsernameNormalized, configuredUser.UsernameNormalized, StringComparison.OrdinalIgnoreCase));

                if (existingUser == null)
                {
                    users.Insert(configuredUser);
                    existingUsers.Add(configuredUser);
                    continue;
                }

                var wasUpdated = false;

                if (!string.Equals(existingUser.Username, configuredUser.Username, StringComparison.Ordinal))
                {
                    existingUser.Username = configuredUser.Username;
                    wasUpdated = true;
                }

                if (!string.Equals(existingUser.PasswordHash, configuredUser.PasswordHash, StringComparison.OrdinalIgnoreCase))
                {
                    existingUser.PasswordHash = configuredUser.PasswordHash;
                    wasUpdated = true;
                }

                if (!HaveSameRoles(existingUser.Roles, configuredUser.Roles))
                {
                    existingUser.Roles = configuredUser.Roles;
                    wasUpdated = true;
                }

                if (wasUpdated)
                {
                    users.Update(existingUser);
                }
            }
        }

        private static void MigrateLegacyCrmData(LiteDatabase database)
        {
            var trips = database.GetCollection<TripRecord>("trips");
            var drivers = database.GetCollection<DriverRecord>("drivers");
            var fleet = database.GetCollection<FleetVehicleRecord>("fleet");
            var clients = database.GetCollection<ClientRecord>("clients");

            if (trips.Count() > 0 || drivers.Count() > 0 || fleet.Count() > 0 || clients.Count() > 0)
            {
                return;
            }

            if (!File.Exists(LegacyJsonFilePath))
            {
                return;
            }

            var json = File.ReadAllText(LegacyJsonFilePath);
            var data = string.IsNullOrWhiteSpace(json)
                ? new CrmDataStore()
                : JsonConvert.DeserializeObject<CrmDataStore>(json) ?? new CrmDataStore();

            NormalizeData(data);

            if (data.Trips.Count == 0 && data.Drivers.Count == 0 && data.FleetVehicles.Count == 0 && data.Clients.Count == 0)
            {
                return;
            }

            database.BeginTrans();

            try
            {
                ReplaceCollection(clients, data.Clients);
                ReplaceCollection(drivers, data.Drivers);
                ReplaceCollection(fleet, data.FleetVehicles);
                ReplaceCollection(trips, data.Trips);
                database.Commit();
            }
            catch
            {
                database.Rollback();
                throw;
            }
        }

        private static void ReplaceCollection<TRecord>(ILiteCollection<TRecord> collection, IEnumerable<TRecord> records)
        {
            collection.DeleteAll();

            var items = records == null ? new List<TRecord>() : records.ToList();

            if (items.Count > 0)
            {
                collection.InsertBulk(items);
            }
        }

        private static List<AuthUserRecord> GetConfiguredUsers()
        {
            var users = new List<AuthUserRecord>();

            foreach (var key in ConfigurationManager.AppSettings.AllKeys)
            {
                if (key == null || !key.StartsWith(UserKeyPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var username = key.Substring(UserKeyPrefix.Length);
                var passwordHash = ConfigurationManager.AppSettings[key];

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(passwordHash))
                {
                    continue;
                }

                users.Add(CreateUser(username, passwordHash, GetConfiguredRoles(username)));
            }

            var defaultUsername = ConfigurationManager.AppSettings[DefaultUsernameKey];
            var defaultPasswordHash = ConfigurationManager.AppSettings[DefaultPasswordHashKey];

            if (!string.IsNullOrWhiteSpace(defaultUsername)
                && !string.IsNullOrWhiteSpace(defaultPasswordHash)
                && !users.Any(item => string.Equals(item.UsernameNormalized, NormalizeLookupValue(defaultUsername), StringComparison.OrdinalIgnoreCase)))
            {
                users.Add(CreateUser(defaultUsername, defaultPasswordHash, GetConfiguredRoles(defaultUsername)));
            }

            return users;
        }

        private static AuthUserRecord CreateUser(string username, string passwordHash, IEnumerable<string> roles)
        {
            var normalizedUsername = (username ?? string.Empty).Trim();

            return new AuthUserRecord
            {
                Username = normalizedUsername,
                UsernameNormalized = NormalizeLookupValue(normalizedUsername),
                PasswordHash = passwordHash.Trim(),
                Roles = NormalizeRoles(roles)
            };
        }

        private static List<string> GetConfiguredRoles(string username)
        {
            var configuredValue = ConfigurationManager.AppSettings[UserRolesKeyPrefix + username];
            return NormalizeRoles(ParseRoles(configuredValue));
        }

        private static IEnumerable<string> ParseRoles(string configuredValue)
        {
            var rawValue = string.IsNullOrWhiteSpace(configuredValue) ? DefaultRoles : configuredValue;
            return rawValue
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim());
        }

        private static List<string> NormalizeRoles(IEnumerable<string> roles)
        {
            var normalizedRoles = (roles ?? Enumerable.Empty<string>())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalizedRoles.Count == 0)
            {
                normalizedRoles.Add("Admin");
            }

            return normalizedRoles;
        }

        private static bool HaveSameRoles(IEnumerable<string> left, IEnumerable<string> right)
        {
            var leftRoles = NormalizeRoles(left);
            var rightRoles = NormalizeRoles(right);

            return leftRoles.Count == rightRoles.Count
                && !leftRoles.Except(rightRoles, StringComparer.OrdinalIgnoreCase).Any();
        }

        private static void NormalizeData(CrmDataStore data)
        {
            data.Trips = data.Trips ?? new List<TripRecord>();
            data.Drivers = data.Drivers ?? new List<DriverRecord>();
            data.FleetVehicles = data.FleetVehicles ?? new List<FleetVehicleRecord>();
            data.Clients = data.Clients ?? new List<ClientRecord>();

            foreach (var vehicle in data.FleetVehicles)
            {
                vehicle.AssignedDriverIds = vehicle.AssignedDriverIds ?? new List<string>();
                vehicle.AssignedDriverNames = vehicle.AssignedDriverNames ?? new List<string>();
            }
        }

        private static string ResolveAppDataPath(string fileName)
        {
            var appDataPath = HostingEnvironment.MapPath("~/App_Data");

            if (string.IsNullOrWhiteSpace(appDataPath))
            {
                appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
            }

            return Path.Combine(appDataPath, fileName);
        }

        private static string NormalizeLookupValue(string value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static string LegacyJsonFilePath
        {
            get
            {
                return ResolveAppDataPath("crm-data.json");
            }
        }
    }
}
