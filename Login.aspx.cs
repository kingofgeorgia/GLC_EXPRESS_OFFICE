using System;
using System.Web.Security;
using System.Web.UI;
using GLC_EXPRESS.Models;
using GLC_EXPRESS.Services;

namespace GLC_EXPRESS
{
    public partial class Login : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            ApplyLocalization();

            if (Request.IsAuthenticated)
            {
                RedirectToTarget();
            }
        }

        protected void SignInButton_Click(object sender, EventArgs e)
        {
            ErrorPanel.Visible = false;
            AuthUserRecord user;

            if (!Page.IsValid)
            {
                return;
            }

            if (!AuthService.TryAuthenticate(UsernameTextBox.Text, PasswordTextBox.Text, out user))
            {
                ErrorPanel.Visible = true;
                ErrorLiteral.Text = T("LoginInvalidCredentials");
                return;
            }

            var username = user == null ? UsernameTextBox.Text.Trim() : user.Username;
            FormsAuthentication.SetAuthCookie(username, RememberMeCheckBox.Checked);
            RedirectToTarget();
        }

        private void RedirectToTarget()
        {
            var returnUrl = Request.QueryString["ReturnUrl"];

            if (!string.IsNullOrWhiteSpace(returnUrl) && UrlIsLocalToHost(returnUrl))
            {
                Response.Redirect(returnUrl, false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            Response.Redirect("~/orders", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected string T(string key)
        {
            return PublicSiteLocalizationService.GetText(key);
        }

        private void ApplyLocalization()
        {
            Title = T("AuthSignIn");
            UsernameRequiredValidator.ErrorMessage = T("LoginUsernameRequired");
            PasswordRequiredValidator.ErrorMessage = T("LoginPasswordRequired");
            SignInButton.Text = T("AuthSignIn");
        }

        private static bool UrlIsLocalToHost(string url)
        {
            return !string.IsNullOrWhiteSpace(url) && url.StartsWith("/", StringComparison.Ordinal) && !url.StartsWith("//", StringComparison.Ordinal);
        }
    }
}
