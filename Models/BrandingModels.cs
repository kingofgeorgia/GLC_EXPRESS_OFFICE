using System;

namespace GLC_EXPRESS.Models
{
    public class BrandingSettingsRecord
    {
        public BrandingSettingsRecord()
        {
            Id = "branding";
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public string Id { get; set; }

        public string LogoPath { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }
}