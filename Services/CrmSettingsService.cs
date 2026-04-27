using System;
using System.Collections.Generic;
using System.Linq;
using GLC_EXPRESS.Models;
using Newtonsoft.Json;

namespace GLC_EXPRESS.Services
{
    public static class CrmSettingsService
    {
        private const string SettingsId = "crm";
        private const string DefaultCompanyName = "GLC EXPRESS";
        private const string DefaultCrmTab = "trips";

        private static readonly string[] DefaultAllowedCrmRoles = { "Admin", "Manager", "Dispatcher" };
        private static readonly string[] DefaultTripStatuses = { "Новый", "В работе", "Завершен", "Отложен" };
        private static readonly string[] DefaultDocumentExtensions = { ".pdf", ".png", ".jpg", ".jpeg", ".gif", ".webp" };
        private static readonly string[] DefaultLogoExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".webp" };
        private static readonly string[] DefaultHomePartnerNames = { "Copart", "IAAI", "Manheim", "AUTOPAPA", "Port of Poti", "Port of Batumi" };
        private static readonly string[] DefaultHomePartnerNamesEn = { "Copart", "IAAI", "Manheim", "AUTOPAPA", "Port of Poti", "Port of Batumi" };
        private static readonly string[] DefaultHomePartnerNamesGe = { "Copart", "IAAI", "Manheim", "AUTOPAPA", "ფოთის პორტი", "ბათუმის პორტი" };
        private static readonly string[] AllowedTabs = { "trips", "drivers", "fleet", "cars", "clients", "inquiries", "settings" };
        private const string DefaultHomeContactAddress = "AUTOPAPA, Рустави, Грузия";
        private const string DefaultHomeContactAddressEn = "AUTOPAPA, Rustavi, Georgia";
        private const string DefaultHomeContactAddressGe = "AUTOPAPA, რუსთავი, საქართველო";
        private const string DefaultHomeContactPhone = "+995 577 11 57 57";
        private const string DefaultHomeContactWorkingHours = "09:00 - 18:00";
        private const string DefaultHomeContactWorkingHoursEn = "09:00 - 18:00";
        private const string DefaultHomeContactWorkingHoursGe = "09:00 - 18:00";
        private const string DefaultHomeContactWhatsAppUrl = "https://wa.me/995577115757";

        public static CrmSettingsRecord GetCurrent()
        {
            try
            {
                using (var database = AppDatabase.Open())
                {
                    var collection = database.GetCollection<CrmSettingsRecord>("crm_settings");
                    var settings = collection.FindById(SettingsId) ?? CreateDefault(database);
                    ApplyDefaults(settings);
                    return settings;
                }
            }
            catch
            {
                return CreateDefault(null);
            }
        }

        public static CrmSettingsRecord Save(CrmSettingsRecord candidate)
        {
            using (var database = AppDatabase.Open())
            {
                var collection = database.GetCollection<CrmSettingsRecord>("crm_settings");
                var settings = Normalize(candidate);
                settings.UpdatedAtUtc = DateTime.UtcNow;
                collection.Upsert(settings);

                return settings;
            }
        }

        public static string ExportToJson(CrmSettingsRecord settings)
        {
            var exchangeRecord = CreateExchangeRecord(settings);
            return JsonConvert.SerializeObject(exchangeRecord, Formatting.Indented);
        }

        public static CrmSettingsRecord ImportFromJson(string json, CrmSettingsRecord currentSettings)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException("JSON с настройками пустой.");
            }

            try
            {
                var exchangeRecord = JsonConvert.DeserializeObject<CrmSettingsExchangeRecord>(json);

                if (exchangeRecord == null)
                {
                    throw new InvalidOperationException("Не удалось прочитать JSON с настройками CRM.");
                }

                return Normalize(new CrmSettingsRecord
                {
                    CompanyName = exchangeRecord.CompanyName,
                    LogoPath = currentSettings == null ? string.Empty : currentSettings.LogoPath,
                    DefaultCrmTab = exchangeRecord.DefaultCrmTab,
                    DefaultTripStatus = exchangeRecord.DefaultTripStatus,
                    AllowedCrmRoles = new List<string>(exchangeRecord.AllowedCrmRoles ?? Enumerable.Empty<string>()),
                    TripStatuses = new List<string>(exchangeRecord.TripStatuses ?? Enumerable.Empty<string>()),
                    AllowedDocumentExtensions = new List<string>(exchangeRecord.AllowedDocumentExtensions ?? Enumerable.Empty<string>()),
                    AllowedLogoExtensions = new List<string>(exchangeRecord.AllowedLogoExtensions ?? Enumerable.Empty<string>()),
                    HomePartnerNames = new List<string>(exchangeRecord.HomePartnerNames ?? Enumerable.Empty<string>()),
                    HomePartnerNamesEn = new List<string>(exchangeRecord.HomePartnerNamesEn ?? Enumerable.Empty<string>()),
                    HomePartnerNamesGe = new List<string>(exchangeRecord.HomePartnerNamesGe ?? Enumerable.Empty<string>()),
                    HomeReviews = CloneReviews(exchangeRecord.HomeReviews),
                    HomeReviewsEn = CloneReviews(exchangeRecord.HomeReviewsEn),
                    HomeReviewsGe = CloneReviews(exchangeRecord.HomeReviewsGe),
                    HomeContactAddress = exchangeRecord.HomeContactAddress,
                    HomeContactAddressEn = exchangeRecord.HomeContactAddressEn,
                    HomeContactAddressGe = exchangeRecord.HomeContactAddressGe,
                    HomeContactPhone = exchangeRecord.HomeContactPhone,
                    HomeContactWorkingHours = exchangeRecord.HomeContactWorkingHours,
                    HomeContactWorkingHoursEn = exchangeRecord.HomeContactWorkingHoursEn,
                    HomeContactWorkingHoursGe = exchangeRecord.HomeContactWorkingHoursGe,
                    HomeContactWhatsAppUrl = exchangeRecord.HomeContactWhatsAppUrl,
                    RequireUniqueTripNumbers = exchangeRecord.RequireUniqueTripNumbers,
                    ValidatePrepaymentAgainstFreight = exchangeRecord.ValidatePrepaymentAgainstFreight
                });
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException("Не удалось прочитать JSON с настройками CRM.", exception);
            }
        }

        public static CrmSettingsRecord Normalize(CrmSettingsRecord candidate)
        {
            var settings = new CrmSettingsRecord
            {
                CompanyName = candidate == null ? string.Empty : candidate.CompanyName,
                LogoPath = candidate == null ? string.Empty : candidate.LogoPath,
                DefaultCrmTab = candidate == null ? string.Empty : candidate.DefaultCrmTab,
                DefaultTripStatus = candidate == null ? string.Empty : candidate.DefaultTripStatus,
                AllowedCrmRoles = candidate == null ? new List<string>() : new List<string>(candidate.AllowedCrmRoles ?? Enumerable.Empty<string>()),
                TripStatuses = candidate == null ? new List<string>() : new List<string>(candidate.TripStatuses ?? Enumerable.Empty<string>()),
                AllowedDocumentExtensions = candidate == null ? new List<string>() : new List<string>(candidate.AllowedDocumentExtensions ?? Enumerable.Empty<string>()),
                AllowedLogoExtensions = candidate == null ? new List<string>() : new List<string>(candidate.AllowedLogoExtensions ?? Enumerable.Empty<string>()),
                HomePartnerNames = candidate == null ? new List<string>() : new List<string>(candidate.HomePartnerNames ?? Enumerable.Empty<string>()),
                HomePartnerNamesEn = candidate == null ? new List<string>() : new List<string>(candidate.HomePartnerNamesEn ?? Enumerable.Empty<string>()),
                HomePartnerNamesGe = candidate == null ? new List<string>() : new List<string>(candidate.HomePartnerNamesGe ?? Enumerable.Empty<string>()),
                HomeReviews = candidate == null ? new List<LandingReviewRecord>() : CloneReviews(candidate.HomeReviews),
                HomeReviewsEn = candidate == null ? new List<LandingReviewRecord>() : CloneReviews(candidate.HomeReviewsEn),
                HomeReviewsGe = candidate == null ? new List<LandingReviewRecord>() : CloneReviews(candidate.HomeReviewsGe),
                HomeContactAddress = candidate == null ? string.Empty : candidate.HomeContactAddress,
                HomeContactAddressEn = candidate == null ? string.Empty : candidate.HomeContactAddressEn,
                HomeContactAddressGe = candidate == null ? string.Empty : candidate.HomeContactAddressGe,
                HomeContactPhone = candidate == null ? string.Empty : candidate.HomeContactPhone,
                HomeContactWorkingHours = candidate == null ? string.Empty : candidate.HomeContactWorkingHours,
                HomeContactWorkingHoursEn = candidate == null ? string.Empty : candidate.HomeContactWorkingHoursEn,
                HomeContactWorkingHoursGe = candidate == null ? string.Empty : candidate.HomeContactWorkingHoursGe,
                HomeContactWhatsAppUrl = candidate == null ? string.Empty : candidate.HomeContactWhatsAppUrl,
                RequireUniqueTripNumbers = candidate != null && candidate.RequireUniqueTripNumbers,
                ValidatePrepaymentAgainstFreight = candidate != null && candidate.ValidatePrepaymentAgainstFreight,
                UpdatedAtUtc = candidate == null ? DateTime.UtcNow : candidate.UpdatedAtUtc
            };

            ApplyDefaults(settings);
            return settings;
        }

        private static CrmSettingsRecord CreateDefault(LiteDB.LiteDatabase database)
        {
            var settings = new CrmSettingsRecord
            {
                CompanyName = DefaultCompanyName,
                LogoPath = ResolveLegacyLogoPath(database),
                DefaultCrmTab = DefaultCrmTab,
                DefaultTripStatus = DefaultTripStatuses[0],
                AllowedCrmRoles = new List<string>(DefaultAllowedCrmRoles),
                TripStatuses = new List<string>(DefaultTripStatuses),
                AllowedDocumentExtensions = new List<string>(DefaultDocumentExtensions),
                AllowedLogoExtensions = new List<string>(DefaultLogoExtensions),
                HomePartnerNames = new List<string>(DefaultHomePartnerNames),
                HomePartnerNamesEn = new List<string>(DefaultHomePartnerNamesEn),
                HomePartnerNamesGe = new List<string>(DefaultHomePartnerNamesGe),
                HomeReviews = CreateDefaultHomeReviews("ru"),
                HomeReviewsEn = CreateDefaultHomeReviews("en"),
                HomeReviewsGe = CreateDefaultHomeReviews("ge"),
                HomeContactAddress = DefaultHomeContactAddress,
                HomeContactAddressEn = DefaultHomeContactAddressEn,
                HomeContactAddressGe = DefaultHomeContactAddressGe,
                HomeContactPhone = DefaultHomeContactPhone,
                HomeContactWorkingHours = DefaultHomeContactWorkingHours,
                HomeContactWorkingHoursEn = DefaultHomeContactWorkingHoursEn,
                HomeContactWorkingHoursGe = DefaultHomeContactWorkingHoursGe,
                HomeContactWhatsAppUrl = DefaultHomeContactWhatsAppUrl,
                RequireUniqueTripNumbers = false,
                ValidatePrepaymentAgainstFreight = false,
                UpdatedAtUtc = DateTime.UtcNow
            };

            ApplyDefaults(settings);
            return settings;
        }

        private static void ApplyDefaults(CrmSettingsRecord settings)
        {
            if (settings == null)
            {
                return;
            }

            settings.Id = SettingsId;
            settings.CompanyName = string.IsNullOrWhiteSpace(settings.CompanyName) ? DefaultCompanyName : settings.CompanyName.Trim();
            settings.AllowedCrmRoles = NormalizeTextList(settings.AllowedCrmRoles, DefaultAllowedCrmRoles);
            settings.TripStatuses = NormalizeTextList(settings.TripStatuses, DefaultTripStatuses);
            settings.AllowedDocumentExtensions = NormalizeExtensions(settings.AllowedDocumentExtensions, DefaultDocumentExtensions, false);
            settings.AllowedLogoExtensions = NormalizeExtensions(settings.AllowedLogoExtensions, DefaultLogoExtensions, true);
            settings.HomePartnerNames = NormalizeTextList(settings.HomePartnerNames, DefaultHomePartnerNames);
            settings.HomePartnerNamesEn = NormalizeOptionalTextList(settings.HomePartnerNamesEn);
            settings.HomePartnerNamesGe = NormalizeOptionalTextList(settings.HomePartnerNamesGe);
            settings.HomeReviews = NormalizeReviewList(settings.HomeReviews, CreateDefaultHomeReviews("ru"));
            settings.HomeReviewsEn = NormalizeOptionalReviewList(settings.HomeReviewsEn);
            settings.HomeReviewsGe = NormalizeOptionalReviewList(settings.HomeReviewsGe);
            settings.HomeContactAddress = NormalizeTextValue(settings.HomeContactAddress, DefaultHomeContactAddress);
            settings.HomeContactAddressEn = NormalizeOptionalTextValue(settings.HomeContactAddressEn);
            settings.HomeContactAddressGe = NormalizeOptionalTextValue(settings.HomeContactAddressGe);
            settings.HomeContactPhone = NormalizeTextValue(settings.HomeContactPhone, DefaultHomeContactPhone);
            settings.HomeContactWorkingHours = NormalizeTextValue(settings.HomeContactWorkingHours, DefaultHomeContactWorkingHours);
            settings.HomeContactWorkingHoursEn = NormalizeOptionalTextValue(settings.HomeContactWorkingHoursEn);
            settings.HomeContactWorkingHoursGe = NormalizeOptionalTextValue(settings.HomeContactWorkingHoursGe);
            settings.HomeContactWhatsAppUrl = NormalizeTextValue(settings.HomeContactWhatsAppUrl, DefaultHomeContactWhatsAppUrl);
            settings.DefaultCrmTab = NormalizeDefaultCrmTab(settings.DefaultCrmTab);
            settings.DefaultTripStatus = NormalizeDefaultTripStatus(settings.DefaultTripStatus, settings.TripStatuses);
        }

        private static CrmSettingsExchangeRecord CreateExchangeRecord(CrmSettingsRecord settings)
        {
            var normalizedSettings = Normalize(settings);

            return new CrmSettingsExchangeRecord
            {
                SchemaVersion = 1,
                CompanyName = normalizedSettings.CompanyName,
                DefaultCrmTab = normalizedSettings.DefaultCrmTab,
                DefaultTripStatus = normalizedSettings.DefaultTripStatus,
                AllowedCrmRoles = new List<string>(normalizedSettings.AllowedCrmRoles ?? Enumerable.Empty<string>()),
                TripStatuses = new List<string>(normalizedSettings.TripStatuses ?? Enumerable.Empty<string>()),
                AllowedDocumentExtensions = new List<string>(normalizedSettings.AllowedDocumentExtensions ?? Enumerable.Empty<string>()),
                AllowedLogoExtensions = new List<string>(normalizedSettings.AllowedLogoExtensions ?? Enumerable.Empty<string>()),
                HomePartnerNames = new List<string>(normalizedSettings.HomePartnerNames ?? Enumerable.Empty<string>()),
                HomePartnerNamesEn = new List<string>(normalizedSettings.HomePartnerNamesEn ?? Enumerable.Empty<string>()),
                HomePartnerNamesGe = new List<string>(normalizedSettings.HomePartnerNamesGe ?? Enumerable.Empty<string>()),
                HomeReviews = CloneReviews(normalizedSettings.HomeReviews),
                HomeReviewsEn = CloneReviews(normalizedSettings.HomeReviewsEn),
                HomeReviewsGe = CloneReviews(normalizedSettings.HomeReviewsGe),
                HomeContactAddress = normalizedSettings.HomeContactAddress,
                HomeContactAddressEn = normalizedSettings.HomeContactAddressEn,
                HomeContactAddressGe = normalizedSettings.HomeContactAddressGe,
                HomeContactPhone = normalizedSettings.HomeContactPhone,
                HomeContactWorkingHours = normalizedSettings.HomeContactWorkingHours,
                HomeContactWorkingHoursEn = normalizedSettings.HomeContactWorkingHoursEn,
                HomeContactWorkingHoursGe = normalizedSettings.HomeContactWorkingHoursGe,
                HomeContactWhatsAppUrl = normalizedSettings.HomeContactWhatsAppUrl,
                RequireUniqueTripNumbers = normalizedSettings.RequireUniqueTripNumbers,
                ValidatePrepaymentAgainstFreight = normalizedSettings.ValidatePrepaymentAgainstFreight
            };
        }

        public static List<string> GetLocalizedHomePartnerNames(CrmSettingsRecord settings, string languageCode)
        {
            var normalizedSettings = Normalize(settings);
            var localizedValues = ResolveLocalizedTextList(normalizedSettings, languageCode, normalizedSettings.HomePartnerNamesEn, normalizedSettings.HomePartnerNamesGe, normalizedSettings.HomePartnerNames);
            return new List<string>(localizedValues);
        }

        public static List<LandingReviewRecord> GetLocalizedHomeReviews(CrmSettingsRecord settings, string languageCode)
        {
            var normalizedSettings = Normalize(settings);
            var localizedValues = ResolveLocalizedReviewList(normalizedSettings, languageCode, normalizedSettings.HomeReviewsEn, normalizedSettings.HomeReviewsGe, normalizedSettings.HomeReviews);
            return CloneReviews(localizedValues);
        }

        public static string GetLocalizedHomeContactAddress(CrmSettingsRecord settings, string languageCode)
        {
            var normalizedSettings = Normalize(settings);
            return ResolveLocalizedTextValue(languageCode, normalizedSettings.HomeContactAddressEn, normalizedSettings.HomeContactAddressGe, normalizedSettings.HomeContactAddress);
        }

        public static string GetLocalizedHomeContactWorkingHours(CrmSettingsRecord settings, string languageCode)
        {
            var normalizedSettings = Normalize(settings);
            return ResolveLocalizedTextValue(languageCode, normalizedSettings.HomeContactWorkingHoursEn, normalizedSettings.HomeContactWorkingHoursGe, normalizedSettings.HomeContactWorkingHours);
        }

        private static List<LandingReviewRecord> CloneReviews(IEnumerable<LandingReviewRecord> reviews)
        {
            return (reviews ?? Enumerable.Empty<LandingReviewRecord>())
                .Select(item => new LandingReviewRecord
                {
                    Quote = item == null ? string.Empty : item.Quote,
                    Author = item == null ? string.Empty : item.Author
                })
                .ToList();
        }

        private static List<LandingReviewRecord> NormalizeReviewList(IEnumerable<LandingReviewRecord> reviews, IEnumerable<LandingReviewRecord> defaults)
        {
            var result = NormalizeOptionalReviewList(reviews);

            return result.Count > 0 ? result : CloneReviews(defaults);
        }

        private static string NormalizeTextValue(string value, string defaultValue)
        {
            var normalized = (value ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(normalized) ? defaultValue : normalized;
        }

        private static string NormalizeOptionalTextValue(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        private static List<LandingReviewRecord> CreateDefaultHomeReviews(string languageCode)
        {
            switch (PublicSiteLocalizationService.NormalizeLanguageCode(languageCode))
            {
                case "en":
                    return new List<LandingReviewRecord>
                    {
                        new LandingReviewRecord
                        {
                            Quote = "It was crucial for us to understand the real project cost before bidding. The team provided a restoration forecast, helped us win the lot, and brought the vehicle to final handover without chaos.",
                            Author = "Private client, Tbilisi"
                        },
                        new LandingReviewRecord
                        {
                            Quote = "We appreciated the logistics transparency: loading photos, a clear delivery timeline, and one manager who stayed in touch at every stage.",
                            Author = "Client, Almaty"
                        },
                        new LandingReviewRecord
                        {
                            Quote = "For a partner model, predictability matters most. GLC Express handles the auction, shipping, and documents together, which saves months of setup time.",
                            Author = "Partner, Rustavi"
                        }
                    };
                case "ge":
                    return new List<LandingReviewRecord>
                    {
                        new LandingReviewRecord
                        {
                            Quote = "ჩვენთვის კრიტიკულად მნიშვნელოვანი იყო ტენდერამდე პროექტის რეალური ღირებულების დანახვა. გუნდმა აღდგენის პროგნოზი მოგვცა, ლოტის მოგებაში დაგვეხმარა და ავტომობილი მშვიდად მოგვიყვანა საბოლოო გადაცემამდე.",
                            Author = "კერძო კლიენტი, თბილისი"
                        },
                        new LandingReviewRecord
                        {
                            Quote = "ძალიან მოგვეწონა ლოჯისტიკის გამჭვირვალობა: დატვირთვის ფოტოები, გასაგები მიწოდების ვადა და ერთი მენეჯერი, რომელიც ყველა ეტაპზე გვიკავშირდებოდა.",
                            Author = "კლიენტი, ალმათი"
                        },
                        new LandingReviewRecord
                        {
                            Quote = "პარტნიორული ფორმატისთვის ყველაზე მნიშვნელოვანი პროცესის პროგნოზირებადობაა. GLC Express ერთიანად ფარავს აუქციონს, გადაზიდვას და დოკუმენტებს, რაც გაშვების თვეებს ზოგავს.",
                            Author = "პარტნიორი, რუსთავი"
                        }
                    };
                default:
                    return new List<LandingReviewRecord>
                    {
                        new LandingReviewRecord
                        {
                            Quote = "Критично было видеть реальную стоимость проекта до торгов. Команда показала прогноз по восстановлению, помогла выиграть лот и довела машину до выдачи без хаоса.",
                            Author = "Частный клиент, Тбилиси"
                        },
                        new LandingReviewRecord
                        {
                            Quote = "Понравилась прозрачность логистики: фотографии погрузки, понятный срок доставки и один менеджер, который держал связь на всех этапах.",
                            Author = "Заказчик, Алматы"
                        },
                        new LandingReviewRecord
                        {
                            Quote = "Для партнерского формата важнее всего предсказуемость процесса. GLC Express закрывает и аукционную часть, и доставку, и документы — это экономит месяцы запуска.",
                            Author = "Партнер, Рустави"
                        }
                    };
            }
        }

        private static List<string> NormalizeTextList(IEnumerable<string> values, IEnumerable<string> defaults)
        {
            var result = new List<string>();

            foreach (var value in values ?? Enumerable.Empty<string>())
            {
                var trimmed = (value ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(trimmed)
                    || result.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var defaultMatch = (defaults ?? Enumerable.Empty<string>())
                    .FirstOrDefault(item => string.Equals(item, trimmed, StringComparison.OrdinalIgnoreCase));

                result.Add(string.IsNullOrWhiteSpace(defaultMatch) ? trimmed : defaultMatch);
            }

            if (result.Count > 0)
            {
                return result;
            }

            return new List<string>((defaults ?? Enumerable.Empty<string>()).Where(item => !string.IsNullOrWhiteSpace(item)));
        }

        private static List<string> NormalizeOptionalTextList(IEnumerable<string> values)
        {
            return NormalizeTextList(values, Enumerable.Empty<string>());
        }

        private static List<LandingReviewRecord> NormalizeOptionalReviewList(IEnumerable<LandingReviewRecord> reviews)
        {
            var result = new List<LandingReviewRecord>();

            foreach (var review in reviews ?? Enumerable.Empty<LandingReviewRecord>())
            {
                var quote = review == null ? string.Empty : (review.Quote ?? string.Empty).Trim();
                var author = review == null ? string.Empty : (review.Author ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(quote) || string.IsNullOrWhiteSpace(author))
                {
                    continue;
                }

                if (result.Any(item => string.Equals(item.Quote, quote, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(item.Author, author, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                result.Add(new LandingReviewRecord
                {
                    Quote = quote,
                    Author = author
                });
            }

            return result;
        }

        private static IEnumerable<string> ResolveLocalizedTextList(CrmSettingsRecord settings, string languageCode, IEnumerable<string> englishValues, IEnumerable<string> georgianValues, IEnumerable<string> russianValues)
        {
            switch (PublicSiteLocalizationService.NormalizeLanguageCode(languageCode))
            {
                case "en":
                    return englishValues != null && englishValues.Any() ? englishValues : russianValues;
                case "ge":
                    return georgianValues != null && georgianValues.Any() ? georgianValues : russianValues;
                default:
                    return russianValues;
            }
        }

        private static IEnumerable<LandingReviewRecord> ResolveLocalizedReviewList(CrmSettingsRecord settings, string languageCode, IEnumerable<LandingReviewRecord> englishValues, IEnumerable<LandingReviewRecord> georgianValues, IEnumerable<LandingReviewRecord> russianValues)
        {
            switch (PublicSiteLocalizationService.NormalizeLanguageCode(languageCode))
            {
                case "en":
                    return englishValues != null && englishValues.Any() ? englishValues : russianValues;
                case "ge":
                    return georgianValues != null && georgianValues.Any() ? georgianValues : russianValues;
                default:
                    return russianValues;
            }
        }

        private static string ResolveLocalizedTextValue(string languageCode, string englishValue, string georgianValue, string russianValue)
        {
            switch (PublicSiteLocalizationService.NormalizeLanguageCode(languageCode))
            {
                case "en":
                    return string.IsNullOrWhiteSpace(englishValue) ? russianValue : englishValue;
                case "ge":
                    return string.IsNullOrWhiteSpace(georgianValue) ? russianValue : georgianValue;
                default:
                    return russianValue;
            }
        }

        private static List<string> NormalizeExtensions(IEnumerable<string> values, IEnumerable<string> defaults, bool restrictToDefaults)
        {
            var defaultList = (defaults ?? Enumerable.Empty<string>())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var result = new List<string>();

            foreach (var value in values ?? Enumerable.Empty<string>())
            {
                var trimmed = (value ?? string.Empty).Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                if (!trimmed.StartsWith(".", StringComparison.Ordinal))
                {
                    trimmed = "." + trimmed.TrimStart('.');
                }

                if (trimmed.Length < 2)
                {
                    continue;
                }

                if (restrictToDefaults && !defaultList.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!result.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                {
                    result.Add(trimmed);
                }
            }

            return result.Count > 0 ? result : defaultList;
        }

        private static string NormalizeDefaultCrmTab(string value)
        {
            var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            return AllowedTabs.Contains(normalized, StringComparer.OrdinalIgnoreCase) ? normalized : DefaultCrmTab;
        }

        private static string NormalizeDefaultTripStatus(string candidate, IList<string> statuses)
        {
            var normalizedStatuses = statuses ?? new List<string>();

            if (normalizedStatuses.Count == 0)
            {
                return DefaultTripStatuses[0];
            }

            var match = normalizedStatuses.FirstOrDefault(item => string.Equals(item, candidate, StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrWhiteSpace(match) ? normalizedStatuses[0] : match;
        }

        private static string ResolveLegacyLogoPath(LiteDB.LiteDatabase database)
        {
            if (database == null)
            {
                return string.Empty;
            }

            var branding = database.GetCollection<BrandingSettingsRecord>("branding_settings").FindById("branding");
            return branding == null ? string.Empty : branding.LogoPath;
        }
    }
}