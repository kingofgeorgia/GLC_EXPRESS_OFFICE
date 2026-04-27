using System;
using System.Linq;
using System.Web.UI;
using GLC_EXPRESS.Services;

namespace GLC_EXPRESS
{
    public partial class AccessDenied : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Title = T("AccessDeniedTitle");
        }

        protected string T(string key)
        {
            return PublicSiteLocalizationService.GetText(key);
        }

        protected string GetAllowedRolesText()
        {
            return string.Join(", ", CrmSettingsService.GetCurrent().AllowedCrmRoles.ToArray());
        }

        protected string GetHomeUrl()
        {
            return PublicSiteLocalizationService.ApplyLanguageToUrl("Default.aspx");
        }

        protected string GetLogoutUrl()
        {
            return PublicSiteLocalizationService.ApplyLanguageToUrl("Logout.aspx");
        }
    }
}