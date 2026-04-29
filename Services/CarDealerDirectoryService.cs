using System;
using System.Collections.Generic;
using System.Linq;
using GLC_EXPRESS.Models;

namespace GLC_EXPRESS.Services
{
    public static class CarDealerDirectoryService
    {
        private static readonly object SyncRoot = new object();

        public static List<string> GetDealerNames()
        {
            lock (SyncRoot)
            {
                using (var database = AppDatabase.Open())
                {
                    return database.GetCollection<CarDealerRecord>("car_dealers")
                        .FindAll()
                        .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Name))
                        .Select(item => item.Name.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(item => item)
                        .ToList();
                }
            }
        }

        public static void EnsureRegistered(string dealerName)
        {
            var normalizedName = NormalizeName(dealerName);

            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return;
            }

            lock (SyncRoot)
            {
                using (var database = AppDatabase.Open())
                {
                    var collection = database.GetCollection<CarDealerRecord>("car_dealers");
                    var existingDealer = collection.FindOne(item => item.NameNormalized == normalizedName);

                    if (existingDealer != null)
                    {
                        if (!string.Equals((existingDealer.Name ?? string.Empty).Trim(), dealerName.Trim(), StringComparison.Ordinal))
                        {
                            existingDealer.Name = dealerName.Trim();
                            collection.Update(existingDealer);
                        }

                        return;
                    }

                    collection.Insert(new CarDealerRecord
                    {
                        Name = dealerName.Trim(),
                        NameNormalized = normalizedName
                    });
                }
            }
        }

        public static void EnsureRegistered(IEnumerable<string> dealerNames)
        {
            var items = (dealerNames ?? Enumerable.Empty<string>())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (items.Count == 0)
            {
                return;
            }

            lock (SyncRoot)
            {
                using (var database = AppDatabase.Open())
                {
                    var collection = database.GetCollection<CarDealerRecord>("car_dealers");
                    var existingNames = new HashSet<string>(collection.FindAll()
                        .Where(item => item != null && !string.IsNullOrWhiteSpace(item.NameNormalized))
                        .Select(item => item.NameNormalized), StringComparer.OrdinalIgnoreCase);

                    foreach (var item in items)
                    {
                        var normalizedName = NormalizeName(item);

                        if (string.IsNullOrWhiteSpace(normalizedName) || existingNames.Contains(normalizedName))
                        {
                            continue;
                        }

                        collection.Insert(new CarDealerRecord
                        {
                            Name = item,
                            NameNormalized = normalizedName
                        });

                        existingNames.Add(normalizedName);
                    }
                }
            }
        }

        private static string NormalizeName(string dealerName)
        {
            return string.IsNullOrWhiteSpace(dealerName)
                ? string.Empty
                : dealerName.Trim().ToUpperInvariant();
        }
    }
}