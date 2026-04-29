using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
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
                    NormalizeMalformedCrmDocuments(database);
                    EnsureIndexes(database);
                    SeedUsers(database);
                    MigrateLegacyCrmData(database);
                    ProtectLegacyUploadedFiles(database);
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
            database.GetCollection<BrandingSettingsRecord>("branding_settings").EnsureIndex(item => item.Id, true);
            database.GetCollection<CrmSettingsRecord>("crm_settings").EnsureIndex(item => item.Id, true);
            database.GetCollection<HomeInquiryRecord>("home_inquiries").EnsureIndex(item => item.Id, true);
            database.GetCollection<HomeInquiryRecord>("home_inquiries").EnsureIndex(item => item.CreatedAtUtc, false);
            database.GetCollection<HomeInquiryRecord>("home_inquiries").EnsureIndex(item => item.Status, false);
            database.GetCollection<HomeInquiryRecord>("home_inquiries").EnsureIndex(item => item.AssignedManager, false);
            database.GetCollection<CarDealerRecord>("car_dealers").EnsureIndex(item => item.Id, true);
            database.GetCollection<CarDealerRecord>("car_dealers").EnsureIndex(item => item.NameNormalized, true);
            database.GetCollection<StoredFileAccessLogRecord>("file_access_logs").EnsureIndex(item => item.StoredPath, false);
            database.GetCollection<StoredFileAccessLogRecord>("file_access_logs").EnsureIndex(item => item.AccessedAtUtc, false);

            database.GetCollection<ClientRecord>("clients").EnsureIndex(item => item.Id, true);
            database.GetCollection<ClientRecord>("clients").EnsureIndex(item => item.SourceInquiryId, false);
            database.GetCollection<CarRecord>("cars").EnsureIndex(item => item.Id, true);
            database.GetCollection<CarRecord>("cars").EnsureIndex(item => item.SourceInquiryId, false);
            database.GetCollection<CarRecord>("cars").EnsureIndex(item => item.CreatedAtUtc, false);
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
            var cars = database.GetCollection<CarRecord>("cars");
            var clients = database.GetCollection<ClientRecord>("clients");

            if (trips.Count() > 0 || drivers.Count() > 0 || fleet.Count() > 0 || cars.Count() > 0 || clients.Count() > 0)
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

            if (data.Trips.Count == 0 && data.Drivers.Count == 0 && data.FleetVehicles.Count == 0 && data.Cars.Count == 0 && data.Clients.Count == 0)
            {
                return;
            }

            database.BeginTrans();

            try
            {
                ReplaceCollection(clients, data.Clients);
                ReplaceCollection(cars, data.Cars);
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

        private static void NormalizeMalformedCrmDocuments(LiteDatabase database)
        {
            database.BeginTrans();

            try
            {
                NormalizeCollectionDocuments(
                    database.GetCollection<BsonDocument>("clients"),
                    new[] { "Name", "Direction", "Manager", "PhoneNumber", "Email" });

                NormalizeCollectionDocuments(
                    database.GetCollection<BsonDocument>("drivers"),
                    new[] { "FullName", "BirthDate", "PhoneNumber", "Address", "PassportScanPath", "LicenseScanPath" });

                NormalizeCollectionDocuments(
                    database.GetCollection<BsonDocument>("fleet"),
                    new[] { "CarBrand", "CarModel", "LicensePlate", "VinCode", "TrailerBrand", "TrailerModel", "TrailerLicensePlate", "DocumentsScanPath" },
                    new Dictionary<string, string>
                    {
                        { "VehicleNumber", "LicensePlate" }
                    },
                    new[] { "AssignedDriverIds", "AssignedDriverNames" });

                NormalizeCollectionDocuments(
                    database.GetCollection<BsonDocument>("trips"),
                    new[] { "Number", "ClientId", "ClientName", "Status", "Country", "VehicleId", "VehicleName", "DriverId", "DriverName", "StartDate", "EndDate", "Freight", "Prepayment" },
                    new Dictionary<string, string>
                    {
                        { "TripNumber", "Number" },
                        { "FleetId", "VehicleId" }
                    });

                NormalizeCollectionDocuments(
                    database.GetCollection<BsonDocument>("cars"),
                    new[] { "SourceInquiryId", "ClientId", "ClientName", "TripNumber", "Forwarder", "Dealer", "Year", "Brand", "Model", "Vin", "Location", "Title", "Key", "Inspection", "ReExport", "Status", "StartPrice", "Invoice", "PortCost", "LoadingCost", "TowTruckCost", "ParkingCost", "InspectionCost", "ReExportCost", "ExpertiseCost", "DeliveryCost", "Volume", "Power", "Comment", "FirstName", "LastName", "Passport", "Address" });

                NormalizeCollectionDocuments(
                    database.GetCollection<BsonDocument>("car_dealers"),
                    new[] { "Name", "NameNormalized" });

                NormalizeCollectionDocuments(
                    database.GetCollection<BsonDocument>("home_inquiries"),
                    new[] { "Name", "Email", "Phone", "Messenger", "Direction", "CargoType", "ClientComment", "Source", "Status", "AssignedManager", "AttachmentPath" });

                NormalizeCollectionDocuments(
                    database.GetCollection<BsonDocument>("file_access_logs"),
                    new[] { "Kind", "StoredPath", "FileName", "Username", "Action", "IpAddress", "UserAgent" });

                database.Commit();
            }
            catch
            {
                database.Rollback();
                throw;
            }
        }

        private static void NormalizeCollectionDocuments(
            ILiteCollection<BsonDocument> collection,
            IEnumerable<string> stringFields,
            IDictionary<string, string> legacyFieldMap = null,
            IEnumerable<string> stringArrayFields = null)
        {
            foreach (var document in collection.FindAll().ToList())
            {
                var originalId = document.ContainsKey("_id") ? document["_id"] : BsonValue.Null;
                var idWasChanged = NormalizeDocumentId(document);
                var wasUpdated = idWasChanged;

                if (legacyFieldMap != null)
                {
                    foreach (var fieldMap in legacyFieldMap)
                    {
                        wasUpdated |= PromoteLegacyField(document, fieldMap.Key, fieldMap.Value);
                    }
                }

                if (stringFields != null)
                {
                    foreach (var fieldName in stringFields)
                    {
                        wasUpdated |= NormalizeStringField(document, fieldName);
                    }
                }

                if (stringArrayFields != null)
                {
                    foreach (var fieldName in stringArrayFields)
                    {
                        wasUpdated |= NormalizeStringArrayField(document, fieldName);
                    }
                }

                if (!wasUpdated)
                {
                    continue;
                }

                if (idWasChanged)
                {
                    if (!originalId.IsNull)
                    {
                        collection.Delete(originalId);
                    }

                    collection.Insert(document);
                    continue;
                }

                collection.Update(document);
            }
        }

        private static bool NormalizeDocumentId(BsonDocument document)
        {
            if (!document.ContainsKey("_id") || document["_id"].IsNull)
            {
                document["_id"] = Guid.NewGuid().ToString("N");
                return true;
            }

            return NormalizeStringField(document, "_id");
        }

        private static bool PromoteLegacyField(BsonDocument document, string sourceField, string targetField)
        {
            if (!document.ContainsKey(sourceField))
            {
                return false;
            }

            var shouldCopy = !document.ContainsKey(targetField)
                || document[targetField].IsNull
                || (document[targetField].IsString && string.IsNullOrWhiteSpace(document[targetField].AsString));

            if (shouldCopy)
            {
                document[targetField] = document[sourceField];
            }

            document.Remove(sourceField);
            return true;
        }

        private static bool NormalizeStringField(BsonDocument document, string fieldName)
        {
            if (!document.ContainsKey(fieldName))
            {
                return false;
            }

            var value = document[fieldName];

            if (value.IsNull || value.IsString)
            {
                return false;
            }

            document[fieldName] = ConvertBsonValueToString(value);
            return true;
        }

        private static bool NormalizeStringArrayField(BsonDocument document, string fieldName)
        {
            if (!document.ContainsKey(fieldName))
            {
                return false;
            }

            var value = document[fieldName];

            if (value.IsArray)
            {
                var normalizedValues = new BsonArray();
                var wasUpdated = false;

                foreach (var item in value.AsArray)
                {
                    if (item.IsNull)
                    {
                        wasUpdated = true;
                        continue;
                    }

                    if (item.IsString)
                    {
                        normalizedValues.Add(item.AsString);
                        continue;
                    }

                    normalizedValues.Add(ConvertBsonValueToString(item));
                    wasUpdated = true;
                }

                if (!wasUpdated)
                {
                    return false;
                }

                document[fieldName] = normalizedValues;
                return true;
            }

            var wrappedValues = new BsonArray();

            if (!value.IsNull)
            {
                wrappedValues.Add(value.IsString ? value.AsString : ConvertBsonValueToString(value));
            }

            document[fieldName] = wrappedValues;
            return true;
        }

        private static string ConvertBsonValueToString(BsonValue value)
        {
            if (value == null || value.IsNull)
            {
                return string.Empty;
            }

            if (value.IsString)
            {
                return value.AsString;
            }

            if (value.IsObjectId)
            {
                return value.AsObjectId.ToString();
            }

            if (value.IsGuid)
            {
                return value.AsGuid.ToString("D");
            }

            if (value.IsDateTime)
            {
                return value.AsDateTime.ToString("o", CultureInfo.InvariantCulture);
            }

            if (value.IsBoolean)
            {
                return value.AsBoolean ? "true" : "false";
            }

            if (value.IsInt32)
            {
                return value.AsInt32.ToString(CultureInfo.InvariantCulture);
            }

            if (value.IsInt64)
            {
                return value.AsInt64.ToString(CultureInfo.InvariantCulture);
            }

            if (value.IsDouble)
            {
                return value.AsDouble.ToString(CultureInfo.InvariantCulture);
            }

            if (value.IsDecimal)
            {
                return value.AsDecimal.ToString(CultureInfo.InvariantCulture);
            }

            return Convert.ToString(value.RawValue, CultureInfo.InvariantCulture) ?? value.ToString();
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
            data.Cars = data.Cars ?? new List<CarRecord>();
            data.Clients = data.Clients ?? new List<ClientRecord>();

            foreach (var vehicle in data.FleetVehicles)
            {
                vehicle.AssignedDriverIds = vehicle.AssignedDriverIds ?? new List<string>();
                vehicle.AssignedDriverNames = vehicle.AssignedDriverNames ?? new List<string>();
            }

            foreach (var car in data.Cars)
            {
                car.ChangeHistory = car.ChangeHistory ?? new List<CarChangeLogRecord>();
            }
        }

        private static void ProtectLegacyUploadedFiles(LiteDatabase database)
        {
            var legacyRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads");
            var protectedRootPath = ResolveAppDataPath("ProtectedUploads");

            if (!Directory.Exists(legacyRootPath))
            {
                return;
            }

            Directory.CreateDirectory(protectedRootPath);

            MigrateDriverFiles(database, legacyRootPath, protectedRootPath);
            MigrateFleetFiles(database, legacyRootPath, protectedRootPath);
            MigrateBrandingFiles(database, legacyRootPath, protectedRootPath);
            MoveRemainingLegacyUploadFiles(legacyRootPath, protectedRootPath);
        }

        private static void MigrateDriverFiles(LiteDatabase database, string legacyRootPath, string protectedRootPath)
        {
            var drivers = database.GetCollection<DriverRecord>("drivers");

            foreach (var driver in drivers.FindAll().ToList())
            {
                var wasUpdated = false;
                var migratedPassportPath = MigrateStoredUploadPath(driver.PassportScanPath, legacyRootPath, protectedRootPath);
                var migratedLicensePath = MigrateStoredUploadPath(driver.LicenseScanPath, legacyRootPath, protectedRootPath);

                if (!string.Equals(driver.PassportScanPath, migratedPassportPath, StringComparison.OrdinalIgnoreCase))
                {
                    driver.PassportScanPath = migratedPassportPath;
                    wasUpdated = true;
                }

                if (!string.Equals(driver.LicenseScanPath, migratedLicensePath, StringComparison.OrdinalIgnoreCase))
                {
                    driver.LicenseScanPath = migratedLicensePath;
                    wasUpdated = true;
                }

                if (wasUpdated)
                {
                    drivers.Update(driver);
                }
            }
        }

        private static void MigrateFleetFiles(LiteDatabase database, string legacyRootPath, string protectedRootPath)
        {
            var fleet = database.GetCollection<FleetVehicleRecord>("fleet");

            foreach (var vehicle in fleet.FindAll().ToList())
            {
                var migratedDocumentsPath = MigrateStoredUploadPath(vehicle.DocumentsScanPath, legacyRootPath, protectedRootPath);

                if (string.Equals(vehicle.DocumentsScanPath, migratedDocumentsPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                vehicle.DocumentsScanPath = migratedDocumentsPath;
                fleet.Update(vehicle);
            }
        }

        private static void MigrateBrandingFiles(LiteDatabase database, string legacyRootPath, string protectedRootPath)
        {
            var branding = database.GetCollection<BrandingSettingsRecord>("branding_settings");
            var settings = branding.FindById("branding");

            if (settings == null)
            {
                return;
            }

            var migratedLogoPath = MigrateStoredUploadPath(settings.LogoPath, legacyRootPath, protectedRootPath);

            if (string.Equals(settings.LogoPath, migratedLogoPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            settings.LogoPath = migratedLogoPath;
            settings.UpdatedAtUtc = DateTime.UtcNow;
            branding.Update(settings);
        }

        private static string MigrateStoredUploadPath(string storedPath, string legacyRootPath, string protectedRootPath)
        {
            var normalizedPath = StoredFileService.NormalizeStoredPath(storedPath);

            if (string.IsNullOrWhiteSpace(normalizedPath)
                || !normalizedPath.StartsWith("~/Uploads/", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedPath;
            }

            var relativePath = normalizedPath.Substring("~/Uploads/".Length).Replace("/", "\\");
            var sourcePath = Path.Combine(legacyRootPath, relativePath);
            var targetPath = Path.Combine(protectedRootPath, relativePath);
            var targetDirectory = Path.GetDirectoryName(targetPath);

            if (!string.IsNullOrWhiteSpace(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            if (File.Exists(sourcePath))
            {
                if (!File.Exists(targetPath))
                {
                    File.Move(sourcePath, targetPath);
                }
                else
                {
                    File.Delete(sourcePath);
                }
            }

            return "~/App_Data/ProtectedUploads/" + relativePath.Replace("\\", "/");
        }

        private static void MoveRemainingLegacyUploadFiles(string legacyRootPath, string protectedRootPath)
        {
            foreach (var sourcePath in Directory.GetFiles(legacyRootPath, "*", SearchOption.AllDirectories))
            {
                if (string.Equals(Path.GetFileName(sourcePath), "Web.config", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var relativePath = sourcePath.Substring(legacyRootPath.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var targetPath = Path.Combine(protectedRootPath, relativePath);
                var targetDirectory = Path.GetDirectoryName(targetPath);

                if (!string.IsNullOrWhiteSpace(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                if (!File.Exists(targetPath))
                {
                    File.Move(sourcePath, targetPath);
                }
                else
                {
                    File.Delete(sourcePath);
                }
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
