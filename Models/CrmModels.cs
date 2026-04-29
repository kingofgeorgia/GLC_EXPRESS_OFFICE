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
            Cars = new List<CarRecord>();
            Clients = new List<ClientRecord>();
        }

        public List<TripRecord> Trips { get; set; }

        public List<DriverRecord> Drivers { get; set; }

        public List<FleetVehicleRecord> FleetVehicles { get; set; }

        public List<CarRecord> Cars { get; set; }

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

        public string SourceInquiryId { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    public class CarDealerRecord
    {
        public CarDealerRecord()
        {
            Id = Guid.NewGuid().ToString("N");
            CreatedAtUtc = DateTime.UtcNow;
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string NameNormalized { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    public class CarChangeLogRecord
    {
        public string FieldName { get; set; }

        public string PreviousValue { get; set; }

        public string NewValue { get; set; }

        public string ChangedBy { get; set; }

        public DateTime ChangedAtUtc { get; set; }
    }

    public class CarRecord
    {
        public CarRecord()
        {
            Id = Guid.NewGuid().ToString("N");
            ChangeHistory = new List<CarChangeLogRecord>();
            CreatedAtUtc = DateTime.UtcNow;
        }

        public string Id { get; set; }

        public string SourceInquiryId { get; set; }

        public string ClientId { get; set; }

        public string ClientName { get; set; }

        public string TripNumber { get; set; }

        public string Forwarder { get; set; }

        public string Dealer { get; set; }

        public string Year { get; set; }

        public string Brand { get; set; }

        public string Model { get; set; }

        public string Vin { get; set; }

        public string Location { get; set; }

        public string Title { get; set; }

        public string Key { get; set; }

        public string Inspection { get; set; }

        public string ReExport { get; set; }

        public string Status { get; set; }

        public string StartPrice { get; set; }

        public string Invoice { get; set; }

        public string PortCost { get; set; }

        public string LoadingCost { get; set; }

        public string TowTruckCost { get; set; }

        public string ParkingCost { get; set; }

        public string InspectionCost { get; set; }

        public string ReExportCost { get; set; }

        public string ExpertiseCost { get; set; }

        public string DeliveryCost { get; set; }

        public string Volume { get; set; }

        public string Power { get; set; }

        public string Comment { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Passport { get; set; }

        public string Address { get; set; }

        public List<CarChangeLogRecord> ChangeHistory { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
