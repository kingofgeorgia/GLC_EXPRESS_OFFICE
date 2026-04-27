using System;
using System.Web;
using System.Web.UI;
using GLC_EXPRESS.Models;
using GLC_EXPRESS.Services;

namespace GLC_EXPRESS
{
    public partial class SiteMaster : MasterPage
    {
        private CrmSettingsRecord _crmSettings;

        protected void Page_Load(object sender, EventArgs e)
        {
            ApplyBranding();
        }

        protected string GetSiteBrandName()
        {
            return CurrentCrmSettings.CompanyName;
        }

        protected string GetMainContainerCss()
        {
            if (IsHomePage())
            {
                return "container-fluid body-content landing-body-content";
            }

            return IsCrmPage()
                ? "container-fluid body-content crm-body-content"
                : "container body-content";
        }

        protected string GetNavbarContainerCss()
        {
            return "container-fluid site-navbar-shell";
        }

        protected string T(string key)
        {
            return PublicSiteLocalizationService.GetText(key);
        }

        protected string GetCurrentHtmlLanguage()
        {
            return PublicSiteLocalizationService.GetCurrentHtmlLanguage();
        }

        protected bool ShouldShowLanguageSwitcher()
        {
            return !IsCrmPage();
        }

        protected string GetLanguageSwitchUrl(string languageCode)
        {
            return PublicSiteLocalizationService.BuildCurrentPageLanguageUrl(Request, languageCode);
        }

        protected string GetLanguageLinkCss(string languageCode)
        {
            return PublicSiteLocalizationService.IsCurrentLanguage(languageCode) ? "active" : string.Empty;
        }

        protected string GetOrdersUrl()
        {
            return PublicSiteLocalizationService.ApplyLanguageToUrl(ResolveUrl("~/orders"));
        }

        protected string GetLoginUrl()
        {
            return PublicSiteLocalizationService.ApplyLanguageToUrl(ResolveUrl("~/Login"));
        }

        protected string GetLogoutUrl()
        {
            return PublicSiteLocalizationService.ApplyLanguageToUrl(ResolveUrl("~/Logout"));
        }

        protected string GetCrmTabUrl(string tabName)
        {
            var normalizedTab = (tabName ?? string.Empty).Trim().ToLowerInvariant();
            var url = string.IsNullOrWhiteSpace(normalizedTab)
                ? GetOrdersUrl()
                : ResolveUrl("~/orders?tab=" + HttpUtility.UrlEncode(normalizedTab));

            return PublicSiteLocalizationService.ApplyLanguageToUrl(url);
        }

        protected string GetHomeRootUrl()
        {
            return PublicSiteLocalizationService.ApplyLanguageToUrl(ResolveUrl("~/"));
        }

        protected string GetHomeSectionUrl(string sectionId)
        {
            var anchor = string.IsNullOrWhiteSpace(sectionId) ? string.Empty : "#" + sectionId.Trim();
            return PublicSiteLocalizationService.ApplyLanguageToUrl(ResolveUrl("~/") + anchor);
        }

        protected string GetPrivacyPolicyUrl()
        {
            return PublicSiteLocalizationService.ApplyLanguageToUrl(ResolveUrl("~/privacy-policy"));
        }

        private void ApplyBranding()
        {
            var settings = CurrentCrmSettings;
            var siteBrandName = GetSiteBrandName();
            var hasLogo = settings != null && !string.IsNullOrWhiteSpace(settings.LogoPath);

            NavbarBrandTextLiteral.Text = HttpUtility.HtmlEncode(siteBrandName);
            NavbarLogoImage.AlternateText = siteBrandName;
            NavbarLogoImage.Visible = hasLogo;
            NavbarBrandTextLiteral.Visible = !hasLogo;

            if (!hasLogo)
            {
                NavbarLogoImage.ImageUrl = string.Empty;
                return;
            }

            var logoUrl = StoredFileService.BuildAccessUrl(settings.LogoPath, StoredFileService.LogoKind, false);

            if (string.IsNullOrWhiteSpace(logoUrl))
            {
                NavbarLogoImage.Visible = false;
                NavbarBrandTextLiteral.Visible = true;
                NavbarLogoImage.ImageUrl = string.Empty;
                return;
            }

            NavbarLogoImage.ImageUrl = ResolveUrl(logoUrl);
        }

        private CrmSettingsRecord CurrentCrmSettings
        {
            get
            {
                return _crmSettings ?? (_crmSettings = CrmSettingsService.GetCurrent());
            }
        }

        private bool IsCrmPage()
        {
            var currentPath = Request == null ? string.Empty : (Request.AppRelativeCurrentExecutionFilePath ?? string.Empty);
            return string.Equals(currentPath, "~/orders.aspx", StringComparison.OrdinalIgnoreCase)
                || string.Equals(currentPath, "~/orders", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsHomePage()
        {
            var currentPath = Request == null ? string.Empty : (Request.AppRelativeCurrentExecutionFilePath ?? string.Empty);
            var absolutePath = Request != null && Request.Url != null ? Request.Url.AbsolutePath ?? string.Empty : string.Empty;

            return string.Equals(currentPath, "~/", StringComparison.OrdinalIgnoreCase)
                || string.Equals(currentPath, "~/Default.aspx", StringComparison.OrdinalIgnoreCase)
                || string.Equals(currentPath, "~/Default", StringComparison.OrdinalIgnoreCase)
                || string.Equals(currentPath, "~/главная", StringComparison.OrdinalIgnoreCase)
                || string.Equals(absolutePath, "/", StringComparison.OrdinalIgnoreCase)
                || absolutePath.EndsWith("/главная", StringComparison.OrdinalIgnoreCase);
        }
    }
}