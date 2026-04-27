using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using GLC_EXPRESS.Models;
using GLC_EXPRESS.Services;

namespace GLC_EXPRESS
{
    public partial class _Default : Page
    {
        private CrmSettingsRecord _crmSettings;

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            ApplyLocalization();
            BindHomePageContent();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                HideHomeLeadAlert();
            }
        }

        protected void HomeLeadSubmitButton_Click(object sender, EventArgs e)
        {
            HideHomeLeadAlert();

            if (!Page.IsValid)
            {
                return;
            }

            try
            {
                HomeInquiryService.Save(
                    Server,
                    HomeLeadNameTextBox.Text,
                    HomeLeadEmailTextBox.Text,
                    HomeLeadPhoneTextBox.Text,
                    HomeLeadMessengerTextBox.Text,
                    HomeLeadDirectionTextBox.Text,
                    HomeLeadCargoTypeDropDownList.SelectedValue,
                    HomeLeadCommentTextBox.Text,
                    HomeLeadAttachmentUpload == null ? null : HomeLeadAttachmentUpload.PostedFile);

                HomeLeadNameTextBox.Text = string.Empty;
                HomeLeadEmailTextBox.Text = string.Empty;
                HomeLeadPhoneTextBox.Text = string.Empty;
                HomeLeadMessengerTextBox.Text = string.Empty;
                HomeLeadDirectionTextBox.Text = string.Empty;
                HomeLeadCommentTextBox.Text = string.Empty;
                HomeLeadCargoTypeDropDownList.ClearSelection();
                ShowHomeLeadAlert(T("HomeLeadAlertSuccess"), "success");
            }
            catch (InvalidOperationException exception)
            {
                ShowHomeLeadAlert(PublicSiteLocalizationService.LocalizeHomeInquiryError(exception.Message), "warning");
            }
        }

        protected string T(string key)
        {
            return PublicSiteLocalizationService.GetText(key);
        }

        private void ShowHomeLeadAlert(string message, string alertType)
        {
            HomeLeadAlertPanel.Visible = true;
            HomeLeadAlertPanel.CssClass = "alert alert-" + alertType + " landing-alert";
            HomeLeadAlertLiteral.Text = HttpUtility.HtmlEncode(message);
        }

        private void HideHomeLeadAlert()
        {
            HomeLeadAlertPanel.Visible = false;
            HomeLeadAlertPanel.CssClass = "alert landing-alert";
            HomeLeadAlertLiteral.Text = string.Empty;
        }

        private void ApplyLocalization()
        {
            Title = T("HomePageTitle");

            HomeLeadNameRequiredValidator.ErrorMessage = T("FormNameRequired");
            HomeLeadEmailRequiredValidator.ErrorMessage = T("FormEmailRequired");
            HomeLeadEmailFormatValidator.ErrorMessage = T("FormEmailInvalid");
            HomeLeadMessengerTextBox.Attributes["placeholder"] = T("FormMessengerPlaceholder");
            HomeLeadDirectionTextBox.Attributes["placeholder"] = T("FormDirectionPlaceholder");
            HomeLeadCommentTextBox.Attributes["placeholder"] = T("FormCommentPlaceholder");
            HomeLeadSubmitButton.Text = T("FormSubmit");

            SetCargoTypeText(string.Empty, "FormCargoUnselected");
            SetCargoTypeText("Седан", "FormCargoSedan");
            SetCargoTypeText("Внедорожник", "FormCargoSuv");
            SetCargoTypeText("Пикап", "FormCargoPickup");
            SetCargoTypeText("Микроавтобус", "FormCargoVan");
            SetCargoTypeText("Мотоцикл", "FormCargoMotorcycle");
            SetCargoTypeText("Эксклюзивные авто", "FormCargoExclusive");
            SetCargoTypeText("Катеры", "FormCargoBoats");
            SetCargoTypeText("Багги", "FormCargoBuggy");
        }

        private void SetCargoTypeText(string value, string key)
        {
            if (HomeLeadCargoTypeDropDownList == null)
            {
                return;
            }

            ListItem item = HomeLeadCargoTypeDropDownList.Items.FindByValue(value);
            if (item != null)
            {
                item.Text = T(key);
            }
        }

        private void BindHomePageContent()
        {
            var settings = CurrentCrmSettings;
            var languageCode = PublicSiteLocalizationService.GetCurrentLanguageCode();

            HomePartnersRepeater.DataSource = CrmSettingsService.GetLocalizedHomePartnerNames(settings, languageCode);
            HomePartnersRepeater.DataBind();

            HomeReviewsRepeater.DataSource = CrmSettingsService.GetLocalizedHomeReviews(settings, languageCode);
            HomeReviewsRepeater.DataBind();

            HomeContactPhoneLiteral.Text = HttpUtility.HtmlEncode(settings.HomeContactPhone);
            HomeContactAddressLiteral.Text = HttpUtility.HtmlEncode(CrmSettingsService.GetLocalizedHomeContactAddress(settings, languageCode));
            HomeContactHoursLiteral.Text = HttpUtility.HtmlEncode(CrmSettingsService.GetLocalizedHomeContactWorkingHours(settings, languageCode));
            HomeWhatsAppLink.HRef = string.IsNullOrWhiteSpace(settings.HomeContactWhatsAppUrl) ? string.Empty : settings.HomeContactWhatsAppUrl;
        }

        private CrmSettingsRecord CurrentCrmSettings
        {
            get
            {
                return _crmSettings ?? (_crmSettings = CrmSettingsService.GetCurrent());
            }
        }
    }
}