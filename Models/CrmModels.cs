using System;
using System.Collections.Generic;

namespace GLC_EXPRESS.Models
{
    public class CrmDataStore
    {
        public CrmDataStore()
        {
            Trips = new List<TripRecord>();
            Drivers = new List<DriverRecord>();
            FleetVehicles = new List<FleetVehicleRecord>();
            Clients = new List<ClientRecord>();
        }

        public List<TripRecord> Trips { get; set; }

        public List<DriverRecord> Drivers { get; set; }

        public List<FleetVehicleRecord> FleetVehicles { get; set; }

        public List<ClientRecord> Clients { get; set; }
    }

    public class TripRecord
    {
        public TripRecord()
        {
            Id = Guid.NewGuid().ToString("N");
            CreatedAtUtc = DateTime.UtcNow;
        }

        public string Id { get; set; }

        public string Number { get; set; }

        public string ClientId { get; set; }

        public string ClientName { get; set; }

        public string Status { get; set; }

        public string Country { get; set; }

        public string VehicleId { get; set; }

        public string VehicleName { get; set; }

        public string DriverId { get; set; }

        public string DriverName { get; set; }

        public string StartDate { get; set; }

        public string EndDate { get; set; }

        public string Freight { get; set; }

        public string Prepayment { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    public class DriverRecord
    {
        public DriverRecord()
        {
            Id = Guid.NewGuid().ToString("N");
            CreatedAtUtc = DateTime.UtcNow;
        }

        public string Id { get; set; }

        public string FullName { get; set; }

        public string BirthDate { get; set; }

        public string PhoneNumber { get; set; }

        public string Address { get; set; }

        public string PassportScanPath { get; set; }

        public string LicenseScanPath { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    public class FleetVehicleRecord
    {
        public FleetVehicleRecord()
        {
            Id = Guid.NewGuid().ToString("N");
            AssignedDriverIds = new List<string>();
            AssignedDriverNames = new List<string>();
            CreatedAtUtc = DateTime.UtcNow;
        }

        public string Id { get; set; }

        public string CarBrand { get; set; }

        public string CarModel { get; set; }

        public string LicensePlate { get; set; }

        public string VinCode { get; set; }

        public string TrailerBrand { get; set; }

        public string TrailerModel { get; set; }

        public string TrailerLicensePlate { get; set; }

        public List<string> AssignedDriverIds { get; set; }

        public List<string> AssignedDriverNames { get; set; }

        public string DocumentsScanPath { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    public class ClientRecord
    {
        public ClientRecord()
        {
            Id = Guid.NewGuid().ToString("N");
            CreatedAtUtc = DateTime.UtcNow;
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string Direction { get; set; }

        public string Manager { get; set; }

        public string PhoneNumber { get; set; }

        public string Email { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
