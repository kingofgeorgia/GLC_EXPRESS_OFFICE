using System;
using System.Web.UI;
using GLC_EXPRESS.Models;
using GLC_EXPRESS.Services;

namespace GLC_EXPRESS
{
    public partial class Contact : Page
    {
        private CrmSettingsRecord _crmSettings;

        protected void Page_Load(object sender, EventArgs e)
        {
            Title = T("NavContacts");
        }

        protected string T(string key)
        {
            return PublicSiteLocalizationService.GetText(key);
        }

        protected string HomeContactAddress
        {
            get { return CrmSettingsService.GetLocalizedHomeContactAddress(CurrentCrmSettings, PublicSiteLocalizationService.GetCurrentLanguageCode()); }
        }

        protected string HomeContactPhone
        {
            get { return CurrentCrmSettings.HomeContactPhone; }
        }

        protected string HomeContactWorkingHours
        {
            get { return CrmSettingsService.GetLocalizedHomeContactWorkingHours(CurrentCrmSettings, PublicSiteLocalizationService.GetCurrentLanguageCode()); }
        }

        protected string HomeWhatsAppUrl
        {
            get { return string.IsNullOrWhiteSpace(CurrentCrmSettings.HomeContactWhatsAppUrl) ? "#" : CurrentCrmSettings.HomeContactWhatsAppUrl; }
        }

        protected string GetContactFormUrl()
        {
            return PublicSiteLocalizationService.ApplyLanguageToUrl("Default.aspx#contacts");
        }

        private CrmSettingsRecord CurrentCrmSettings
        {
            get { return _crmSettings ?? (_crmSettings = CrmSettingsService.GetCurrent()); }
        }
    }
}