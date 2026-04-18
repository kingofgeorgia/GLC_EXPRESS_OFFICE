using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using GLC_EXPRESS.Models;
using Newtonsoft.Json;

namespace GLC_EXPRESS.Services
{
    public static class CrmRepository
    {
        private static readonly object SyncRoot = new object();

        public static CrmDataStore Load()
        {
            lock (SyncRoot)
            {
                EnsureStorage();

                var json = File.ReadAllText(DataFilePath);
                var data = string.IsNullOrWhiteSpace(json)
                    ? new CrmDataStore()
                    : JsonConvert.DeserializeObject<CrmDataStore>(json) ?? new CrmDataStore();

                Normalize(data);
                return data;
            }
        }

        public static void Save(CrmDataStore data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            lock (SyncRoot)
            {
                Normalize(data);
                EnsureStorage();
                File.WriteAllText(DataFilePath, JsonConvert.SerializeObject(data, Formatting.Indented));
            }
        }

        private static void EnsureStorage()
        {
            var directory = Path.GetDirectoryName(DataFilePath);

            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException("CRM storage directory could not be resolved.");
            }

            Directory.CreateDirectory(directory);

            if (!File.Exists(DataFilePath))
            {
                File.WriteAllText(DataFilePath, JsonConvert.SerializeObject(new CrmDataStore(), Formatting.Indented));
            }
        }

        private static void Normalize(CrmDataStore data)
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

        private static string DataFilePath
        {
            get
            {
                var context = HttpContext.Current;

                if (context == null)
                {
                    throw new InvalidOperationException("CRM repository requires an active HTTP context.");
                }

                return context.Server.MapPath("~/App_Data/crm-data.json");
            }
        }
    }
}
