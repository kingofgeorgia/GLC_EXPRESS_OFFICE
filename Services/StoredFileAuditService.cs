using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using GLC_EXPRESS.Models;

namespace GLC_EXPRESS.Services
{
    public static class StoredFileAuditService
    {
        public static void LogAccess(HttpContext context, string kind, string storedPath, string action)
        {
            var normalizedPath = StoredFileService.NormalizeStoredPath(storedPath);

            if (string.IsNullOrWhiteSpace(normalizedPath) || string.IsNullOrWhiteSpace(action))
            {
                return;
            }

            try
            {
                using (var database = AppDatabase.Open())
                {
                    database.GetCollection<StoredFileAccessLogRecord>("file_access_logs").Insert(new StoredFileAccessLogRecord
                    {
                        Kind = (kind ?? string.Empty).Trim(),
                        StoredPath = normalizedPath,
                        FileName = Path.GetFileName(normalizedPath),
                        Username = ResolveUsername(context),
                        Action = action.Trim(),
                        IpAddress = ResolveIpAddress(context),
                        UserAgent = ResolveUserAgent(context)
                    });
                }
            }
            catch
            {
            }
        }

        public static List<StoredFileAccessLogRecord> GetRecentAccessEntries(string storedPath, int take)
        {
            var normalizedPath = StoredFileService.NormalizeStoredPath(storedPath);

            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return new List<StoredFileAccessLogRecord>();
            }

            var pageSize = take <= 0 ? 10 : take;

            try
            {
                using (var database = AppDatabase.Open())
                {
                    return database.GetCollection<StoredFileAccessLogRecord>("file_access_logs")
                        .Find(item => item.StoredPath == normalizedPath)
                        .OrderByDescending(item => item.AccessedAtUtc)
                        .Take(pageSize)
                        .ToList();
                }
            }
            catch
            {
                return new List<StoredFileAccessLogRecord>();
            }
        }

        private static string ResolveUsername(HttpContext context)
        {
            if (context == null || context.User == null || context.User.Identity == null || !context.User.Identity.IsAuthenticated)
            {
                return "Anonymous";
            }

            return context.User.Identity.Name;
        }

        private static string ResolveIpAddress(HttpContext context)
        {
            return context == null || context.Request == null ? string.Empty : (context.Request.UserHostAddress ?? string.Empty).Trim();
        }

        private static string ResolveUserAgent(HttpContext context)
        {
            return context == null || context.Request == null ? string.Empty : (context.Request.UserAgent ?? string.Empty).Trim();
        }
    }
}