using System;
using System.Collections.Generic;
using System.Linq;
using GLC_EXPRESS.Models;
using LiteDB;

namespace GLC_EXPRESS.Services
{
    public static class CrmRepository
    {
        private static readonly object SyncRoot = new object();

        public static CrmDataStore Load()
        {
            lock (SyncRoot)
            {
                using (var database = AppDatabase.Open())
                {
                    var data = new CrmDataStore
                    {
                        Trips = database.GetCollection<TripRecord>("trips").FindAll().ToList(),
                        Drivers = database.GetCollection<DriverRecord>("drivers").FindAll().ToList(),
                        FleetVehicles = database.GetCollection<FleetVehicleRecord>("fleet").FindAll().ToList(),
                        Clients = database.GetCollection<ClientRecord>("clients").FindAll().ToList()
                    };

                    Normalize(data);
                    return data;
                }
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

                using (var database = AppDatabase.Open())
                {
                    database.BeginTrans();

                    try
                    {
                        ReplaceCollection(database.GetCollection<ClientRecord>("clients"), data.Clients);
                        ReplaceCollection(database.GetCollection<DriverRecord>("drivers"), data.Drivers);
                        ReplaceCollection(database.GetCollection<FleetVehicleRecord>("fleet"), data.FleetVehicles);
                        ReplaceCollection(database.GetCollection<TripRecord>("trips"), data.Trips);
                        database.Commit();
                    }
                    catch
                    {
                        database.Rollback();
                        throw;
                    }
                }
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

        private static void ReplaceCollection<TRecord>(ILiteCollection<TRecord> collection, IEnumerable<TRecord> records)
        {
            collection.DeleteAll();

            var items = records == null ? new List<TRecord>() : records.ToList();

            if (items.Count > 0)
            {
                collection.InsertBulk(items);
            }
        }
    }
}
