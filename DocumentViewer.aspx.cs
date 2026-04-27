using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using GLC_EXPRESS.Services;

namespace GLC_EXPRESS
{
    public partial class DocumentViewer : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!EnsureAuthenticated() || !EnsureCrmAccess())
            {
                return;
            }

            if (!IsPostBack)
            {
                LoadDocument();
            }
        }

        private void LoadDocument()
        {
            string storedPath;
            string physicalPath;

            if (!StoredFileService.TryResolvePathFromToken(Server, Request.QueryString["token"], out storedPath, out physicalPath)
                || !File.Exists(physicalPath))
            {
                ShowWarning("Документ не найден.", 404);
                return;
            }

            DocumentContentPanel.Visible = true;
            DocumentFileNameLiteral.Text = HttpUtility.HtmlEncode(Path.GetFileName(physicalPath));

            var inlineUrl = ResolveUrl(StoredFileService.BuildAccessUrl(storedPath, StoredFileService.DocumentKind, false));
            var downloadUrl = ResolveUrl(StoredFileService.BuildAccessUrl(storedPath, StoredFileService.DocumentKind, true));
            var extension = (Path.GetExtension(physicalPath) ?? string.Empty).ToLowerInvariant();

            DownloadDocumentHyperLink.NavigateUrl = downloadUrl;

            StoredFileAuditService.LogAccess(Context, StoredFileService.DocumentKind, storedPath, "viewer-open");
            BindPreview(inlineUrl, extension, Path.GetFileName(physicalPath));
            BindAccessLog(storedPath);
        }

        private void BindPreview(string inlineUrl, string extension, string fileName)
        {
            if (string.IsNullOrWhiteSpace(inlineUrl))
            {
                DocumentPreviewAvailablePanel.Visible = false;
                DocumentPreviewUnavailablePanel.Visible = true;
                return;
            }

            if (IsImage(extension))
            {
                DocumentPreviewAvailablePanel.Visible = true;
                DocumentPreviewUnavailablePanel.Visible = false;
                DocumentPreviewLiteral.Text = string.Format(
                    "<img src=\"{0}\" alt=\"{1}\" class=\"document-preview-image\" />",
                    HttpUtility.HtmlAttributeEncode(inlineUrl),
                    HttpUtility.HtmlAttributeEncode(fileName ?? "Документ"));
                return;
            }

            if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                DocumentPreviewAvailablePanel.Visible = true;
                DocumentPreviewUnavailablePanel.Visible = false;
                DocumentPreviewLiteral.Text = string.Format(
                    "<iframe src=\"{0}\" class=\"document-preview-frame\" title=\"Предпросмотр документа\"></iframe>",
                    HttpUtility.HtmlAttributeEncode(inlineUrl));
                return;
            }

            DocumentPreviewAvailablePanel.Visible = false;
            DocumentPreviewUnavailablePanel.Visible = true;
        }

        private void BindAccessLog(string storedPath)
        {
            var entries = StoredFileAuditService.GetRecentAccessEntries(storedPath, 20);
            DocumentAuditRepeater.DataSource = entries;
            DocumentAuditRepeater.DataBind();
            DocumentAuditRepeater.Visible = entries.Count > 0;
            DocumentAuditEmptyPanel.Visible = entries.Count == 0;
        }

        private void ShowWarning(string message, int statusCode)
        {
            Response.StatusCode = statusCode;
            Response.TrySkipIisCustomErrors = true;
            DocumentContentPanel.Visible = false;
            DocumentAlertPanel.Visible = true;
            DocumentAlertLiteral.Text = HttpUtility.HtmlEncode(message);
        }

        private bool EnsureAuthenticated()
        {
            if (!Request.IsAuthenticated)
            {
                Response.Redirect(FormsAuthentication.LoginUrl + "?ReturnUrl=" + Server.UrlEncode(Request.RawUrl), false);
                Context.ApplicationInstance.CompleteRequest();
                return false;
            }

            return true;
        }

        private bool EnsureCrmAccess()
        {
            var settings = CrmSettingsService.GetCurrent();

            if (Context != null
                && Context.User != null
                && settings.AllowedCrmRoles.Any(role => Context.User.IsInRole(role)))
            {
                return true;
            }

            Response.Redirect("~/AccessDenied.aspx?ReturnUrl=" + Server.UrlEncode(Request.RawUrl), false);
            Context.ApplicationInstance.CompleteRequest();
            return false;
        }

        private static bool IsImage(string extension)
        {
            return string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".gif", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase);
        }
    }
}
