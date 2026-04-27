using System;
using System.IO;
using System.Text;
using System.Web;

namespace GLC_EXPRESS.Services
{
    public static class StoredFileService
    {
        public const string DocumentKind = "document";
        public const string LogoKind = "logo";

        private const string ProtectedUploadsRoot = "~/App_Data/ProtectedUploads";
        private const string LegacyUploadsRoot = "~/Uploads";

        public static string BuildProtectedAppRelativePath(string relativeFolder, string fileName)
        {
            var normalizedFolder = (relativeFolder ?? string.Empty)
                .Trim()
                .TrimStart('~', '/')
                .Replace("\\", "/");

            var normalizedFileName = (fileName ?? string.Empty)
                .Trim()
                .TrimStart('~', '/')
                .Replace("\\", "/");

            if (string.IsNullOrWhiteSpace(normalizedFileName))
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(normalizedFolder)
                ? ProtectedUploadsRoot + "/" + normalizedFileName
                : ProtectedUploadsRoot + "/" + normalizedFolder + "/" + normalizedFileName;
        }

        public static string BuildAccessUrl(string storedPath, string kind, bool download)
        {
            var normalizedPath = NormalizeStoredPath(storedPath);

            if (string.IsNullOrWhiteSpace(normalizedPath) || !IsAllowedVirtualPath(normalizedPath))
            {
                return string.Empty;
            }

            var token = HttpServerUtility.UrlTokenEncode(Encoding.UTF8.GetBytes(normalizedPath));

            if (string.IsNullOrWhiteSpace(token))
            {
                return string.Empty;
            }

            var downloadSuffix = download ? "&download=1" : string.Empty;

            return string.Format(
                "~/FileAsset.ashx?kind={0}&token={1}{2}",
                HttpUtility.UrlEncode(kind ?? string.Empty),
                HttpUtility.UrlEncode(token),
                downloadSuffix);
        }

        public static string BuildDocumentViewerUrl(string storedPath)
        {
            var normalizedPath = NormalizeStoredPath(storedPath);

            if (string.IsNullOrWhiteSpace(normalizedPath) || !IsAllowedVirtualPath(normalizedPath))
            {
                return string.Empty;
            }

            var token = HttpServerUtility.UrlTokenEncode(Encoding.UTF8.GetBytes(normalizedPath));

            if (string.IsNullOrWhiteSpace(token))
            {
                return string.Empty;
            }

            return string.Format("~/DocumentViewer?token={0}", HttpUtility.UrlEncode(token));
        }

        public static bool TryResolvePathFromToken(HttpServerUtility server, string token, out string storedPath, out string physicalPath)
        {
            storedPath = string.Empty;
            physicalPath = string.Empty;

            if (server == null || string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            byte[] rawBytes;

            try
            {
                rawBytes = HttpServerUtility.UrlTokenDecode(token);
            }
            catch
            {
                return false;
            }

            if (rawBytes == null || rawBytes.Length == 0)
            {
                return false;
            }

            storedPath = Encoding.UTF8.GetString(rawBytes);
            return TryMapStoredPath(server, storedPath, out physicalPath);
        }

        public static bool TryMapStoredPath(HttpServerUtility server, string storedPath, out string physicalPath)
        {
            physicalPath = string.Empty;

            if (server == null)
            {
                return false;
            }

            var normalizedPath = NormalizeStoredPath(storedPath);

            if (string.IsNullOrWhiteSpace(normalizedPath) || !IsAllowedVirtualPath(normalizedPath))
            {
                return false;
            }

            var candidatePath = Path.GetFullPath(server.MapPath(normalizedPath));
            var protectedRoot = Path.GetFullPath(server.MapPath(ProtectedUploadsRoot));
            var legacyRoot = Path.GetFullPath(server.MapPath(LegacyUploadsRoot));

            if (!IsUnderRoot(candidatePath, protectedRoot) && !IsUnderRoot(candidatePath, legacyRoot))
            {
                return false;
            }

            physicalPath = candidatePath;
            return true;
        }

        public static string NormalizeStoredPath(string storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath))
            {
                return string.Empty;
            }

            var normalizedPath = storedPath.Trim().Replace("\\", "/");

            if (normalizedPath.StartsWith("~/", StringComparison.Ordinal))
            {
                return normalizedPath;
            }

            if (normalizedPath.StartsWith("~", StringComparison.Ordinal))
            {
                return normalizedPath.Insert(1, "/");
            }

            if (normalizedPath.StartsWith("/", StringComparison.Ordinal))
            {
                return "~" + normalizedPath;
            }

            return "~/" + normalizedPath.TrimStart('/');
        }

        private static bool IsAllowedVirtualPath(string normalizedPath)
        {
            return normalizedPath.StartsWith(ProtectedUploadsRoot + "/", StringComparison.OrdinalIgnoreCase)
                || normalizedPath.StartsWith(LegacyUploadsRoot + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUnderRoot(string fullPath, string rootPath)
        {
            var normalizedRoot = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var normalizedFullPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return normalizedFullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedFullPath, rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
        }
    }
}