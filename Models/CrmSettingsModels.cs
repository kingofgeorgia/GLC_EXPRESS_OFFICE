using System;
using System.Collections.Generic;

namespace GLC_EXPRESS.Models
{
    [Serializable]
    public class LandingReviewRecord
    {
        public string Quote { get; set; }

        public string Author { get; set; }
    }

    public class CrmSettingsRecord
    {
        public CrmSettingsRecord()
        {
            Id = "crm";
            CompanyName = "GLC EXPRESS";
            DefaultCrmTab = "trips";
            AllowedCrmRoles = new List<string>();
            TripStatuses = new List<string>();
            AllowedDocumentExtensions = new List<string>();
            AllowedLogoExtensions = new List<string>();
            HomePartnerNames = new List<string>();
            HomePartnerNamesEn = new List<string>();
            HomePartnerNamesGe = new List<string>();
            HomeReviews = new List<LandingReviewRecord>();
            HomeReviewsEn = new List<LandingReviewRecord>();
            HomeReviewsGe = new List<LandingReviewRecord>();
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public string Id { get; set; }

        public string CompanyName { get; set; }

        public string LogoPath { get; set; }

        public string DefaultCrmTab { get; set; }

        public string DefaultTripStatus { get; set; }

        public List<string> AllowedCrmRoles { get; set; }

        public List<string> TripStatuses { get; set; }

        public List<string> AllowedDocumentExtensions { get; set; }

        public List<string> AllowedLogoExtensions { get; set; }

        public List<string> HomePartnerNames { get; set; }

        public List<string> HomePartnerNamesEn { get; set; }

        public List<string> HomePartnerNamesGe { get; set; }

        public List<LandingReviewRecord> HomeReviews { get; set; }

        public List<LandingReviewRecord> HomeReviewsEn { get; set; }

        public List<LandingReviewRecord> HomeReviewsGe { get; set; }

        public string HomeContactAddress { get; set; }

        public string HomeContactAddressEn { get; set; }

        public string HomeContactAddressGe { get; set; }

        public string HomeContactPhone { get; set; }

        public string HomeContactWorkingHours { get; set; }

        public string HomeContactWorkingHoursEn { get; set; }

        public string HomeContactWorkingHoursGe { get; set; }

        public string HomeContactWhatsAppUrl { get; set; }

        public bool RequireUniqueTripNumbers { get; set; }

        public bool ValidatePrepaymentAgainstFreight { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }

    public class CrmSettingsExchangeRecord
    {
        public CrmSettingsExchangeRecord()
        {
            SchemaVersion = 2;
            AllowedCrmRoles = new List<string>();
            TripStatuses = new List<string>();
            AllowedDocumentExtensions = new List<string>();
            AllowedLogoExtensions = new List<string>();
            HomePartnerNames = new List<string>();
            HomePartnerNamesEn = new List<string>();
            HomePartnerNamesGe = new List<string>();
            HomeReviews = new List<LandingReviewRecord>();
            HomeReviewsEn = new List<LandingReviewRecord>();
            HomeReviewsGe = new List<LandingReviewRecord>();
        }

        public int SchemaVersion { get; set; }

        public string CompanyName { get; set; }

        public string DefaultCrmTab { get; set; }

        public string DefaultTripStatus { get; set; }

        public List<string> AllowedCrmRoles { get; set; }

        public List<string> TripStatuses { get; set; }

        public List<string> AllowedDocumentExtensions { get; set; }

        public List<string> AllowedLogoExtensions { get; set; }

        public List<string> HomePartnerNames { get; set; }

        public List<string> HomePartnerNamesEn { get; set; }

        public List<string> HomePartnerNamesGe { get; set; }

        public List<LandingReviewRecord> HomeReviews { get; set; }

        public List<LandingReviewRecord> HomeReviewsEn { get; set; }

        public List<LandingReviewRecord> HomeReviewsGe { get; set; }

        public string HomeContactAddress { get; set; }

        public string HomeContactAddressEn { get; set; }

        public string HomeContactAddressGe { get; set; }

        public string HomeContactPhone { get; set; }

        public string HomeContactWorkingHours { get; set; }

        public string HomeContactWorkingHoursEn { get; set; }

        public string HomeContactWorkingHoursGe { get; set; }

        public string HomeContactWhatsAppUrl { get; set; }

        public bool RequireUniqueTripNumbers { get; set; }

        public bool ValidatePrepaymentAgainstFreight { get; set; }
    }
}