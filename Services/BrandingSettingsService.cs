using System;
using GLC_EXPRESS.Models;

namespace GLC_EXPRESS.Services
{
    public static class BrandingSettingsService
    {
        private const string SettingsId = "branding";

        public static BrandingSettingsRecord GetCurrent()
        {
            try
            {
                using (var database = AppDatabase.Open())
                {
                    var settings = database.GetCollection<BrandingSettingsRecord>("branding_settings")
                        .FindById(SettingsId);

                    return settings ?? new BrandingSettingsRecord();
                }
            }
            catch
            {
                return new BrandingSettingsRecord();
            }
        }

        public static BrandingSettingsRecord SaveLogoPath(string logoPath)
        {
            using (var database = AppDatabase.Open())
            {
                var collection = database.GetCollection<BrandingSettingsRecord>("branding_settings");
                var settings = collection.FindById(SettingsId) ?? new BrandingSettingsRecord();

                settings.LogoPath = logoPath;
                settings.UpdatedAtUtc = DateTime.UtcNow;
                collection.Upsert(settings);

                return settings;
            }
        }
    }
}