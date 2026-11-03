using System;
using System.Web;
using System.Web.UI;
using GLC_EXPRESS.Services;

namespace GLC_EXPRESS
{
    public partial class Register : Page
    {
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            if (Session != null)
            {
                ViewStateUserKey = Session.SessionID;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            ApplyLocalization();
            UpdateRegistrationAvailability();
        }

        protected void RegisterButton_Click(object sender, EventArgs e)
        {
            RegistrationErrorPanel.Visible = false;

            if (!CrmSettingsService.IsPublicRegistrationEnabled())
            {
                UpdateRegistrationAvailability();
                return;
            }

            if (!Page.IsValid)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(Request.Form["registration-company"]))
            {
                ShowError(T("RegisterError"));
                return;
            }

            try
            {
                AuthService.RegisterPublicAccount(
                    FullNameTextBox.Text,
                    EmailTextBox.Text,
                    PhoneTextBox.Text,
                    PasswordTextBox.Text);

                RegistrationFormPanel.Visible = false;
                RegistrationSuccessPanel.Visible = true;
            }
            catch (InvalidOperationException exception)
            {
                ShowError(exception.Message.IndexOf("email уже зарегистрирован", StringComparison.OrdinalIgnoreCase) >= 0
                    ? T("RegisterEmailExists")
                    : T("RegisterError"));
            }
            catch (ArgumentException)
            {
                ShowError(T("RegisterError"));
            }
            catch
            {
                ShowError(T("RegisterError"));
            }
        }

        protected void PrivacyConsentValidator_ServerValidate(object source, System.Web.UI.WebControls.ServerValidateEventArgs args)
        {
            args.IsValid = PrivacyConsentCheckBox.Checked;
        }

        protected string T(string key)
        {
            return PublicSiteLocalizationService.GetText(key);
        }

        protected string GetPrivacyPolicyUrl()
        {
            return PublicSiteLocalizationService.ApplyLanguageToUrl(ResolveUrl("~/privacy-policy"));
        }

        private void ApplyLocalization()
        {
            Title = T("RegisterTitle");
            FullNameRequiredValidator.ErrorMessage = T("RegisterNameRequired");
            EmailRequiredValidator.ErrorMessage = T("RegisterEmailRequired");
            EmailFormatValidator.ErrorMessage = T("RegisterEmailInvalid");
            PasswordRequiredValidator.ErrorMessage = T("RegisterPasswordRequired");
            ConfirmPasswordRequiredValidator.ErrorMessage = T("RegisterPasswordConfirmRequired");
            PasswordMatchValidator.ErrorMessage = T("RegisterPasswordMismatch");
            PrivacyConsentValidator.ErrorMessage = T("RegisterConsentRequired");
            RegisterButton.Text = T("RegisterSubmit");
        }

        private void UpdateRegistrationAvailability()
        {
            var enabled = CrmSettingsService.IsPublicRegistrationEnabled();
            RegistrationDisabledPanel.Visible = !enabled;
            if (!enabled)
            {
                RegistrationFormPanel.Visible = false;
            }
        }

        private void ShowError(string message)
        {
            RegistrationErrorLiteral.Text = HttpUtility.HtmlEncode(message);
            RegistrationErrorPanel.Visible = true;
        }
    }
}
