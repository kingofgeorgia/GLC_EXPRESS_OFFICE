using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Security;
using GLC_EXPRESS.Services;

namespace GLC_EXPRESS
{
    public class FileAsset : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            if (context == null)
            {
                return;
            }

            var kind = (context.Request.QueryString["kind"] ?? string.Empty).Trim().ToLowerInvariant();

            if (!string.Equals(kind, StoredFileService.DocumentKind, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(kind, StoredFileService.LogoKind, StringComparison.OrdinalIgnoreCase))
            {
                WriteStatus(context, 404);
                return;
            }

            if (!IsAuthorized(context, kind))
            {
                if (!context.Request.IsAuthenticated && string.Equals(kind, StoredFileService.DocumentKind, StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Redirect(FormsAuthentication.LoginUrl + "?ReturnUrl=" + HttpUtility.UrlEncode(context.Request.RawUrl), false);
                    context.ApplicationInstance.CompleteRequest();
                    return;
                }

                WriteStatus(context, 403);
                return;
            }

            string storedPath;
            string physicalPath;

            if (!StoredFileService.TryResolvePathFromToken(context.Server, context.Request.QueryString["token"], out storedPath, out physicalPath)
                || !File.Exists(physicalPath))
            {
                WriteStatus(context, 404);
                return;
            }

            var fileName = Path.GetFileName(physicalPath);
            var contentType = MimeMapping.GetMimeMapping(fileName);
            var forceDownload = string.Equals(context.Request.QueryString["download"], "1", StringComparison.OrdinalIgnoreCase);
            var dispositionType = forceDownload ? "attachment" : "inline";
            var safeFileName = (fileName ?? "file").Replace("\"", string.Empty);

            context.Response.Clear();
            context.Response.TrySkipIisCustomErrors = true;
            context.Response.ContentType = contentType;
            context.Response.AddHeader("X-Content-Type-Options", "nosniff");
            context.Response.AddHeader("Content-Disposition", dispositionType + "; filename=\"" + safeFileName + "\"");

            if (string.Equals(kind, StoredFileService.LogoKind, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Cache.SetCacheability(HttpCacheability.Public);
                context.Response.Cache.SetMaxAge(TimeSpan.FromMinutes(30));
            }
            else
            {
                context.Response.Cache.SetCacheability(HttpCacheability.Private);
                context.Response.Cache.SetNoStore();
                StoredFileAuditService.LogAccess(context, kind, storedPath, forceDownload ? "download" : "inline-open");
            }

            context.Response.TransmitFile(physicalPath);
        }

        public bool IsReusable
        {
            get { return true; }
        }

        private static bool IsAuthorized(HttpContext context, string kind)
        {
            if (string.Equals(kind, StoredFileService.LogoKind, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var settings = CrmSettingsService.GetCurrent();

            return context != null
                && context.User != null
                && context.User.Identity != null
                && context.User.Identity.IsAuthenticated
                && settings.AllowedCrmRoles.Any(role => context.User.IsInRole(role));
        }

        private static void WriteStatus(HttpContext context, int statusCode)
        {
            context.Response.StatusCode = statusCode;
            context.Response.TrySkipIisCustomErrors = true;
        }
    }
}