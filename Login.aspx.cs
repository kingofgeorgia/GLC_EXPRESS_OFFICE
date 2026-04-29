using System;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using GLC_EXPRESS.Models;
using GLC_EXPRESS.Services;

namespace GLC_EXPRESS
{
    public partial class Login : Page
    {
        private const string AutoLoginPreferenceCookieName = "glc-auto-login";

        protected void Page_Load(object sender, EventArgs e)
        {
            ApplyLocalization();

            if (!IsPostBack)
            {
                RememberMeCheckBox.Checked = HasAutoLoginPreference();
            }

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
            StoreAutoLoginPreference(RememberMeCheckBox.Checked);
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

        private bool HasAutoLoginPreference()
        {
            return Request != null
                && Request.Cookies[AutoLoginPreferenceCookieName] != null
                && string.Equals(Request.Cookies[AutoLoginPreferenceCookieName].Value, "1", StringComparison.Ordinal);
        }

        private void StoreAutoLoginPreference(bool enabled)
        {
            if (Response == null)
            {
                return;
            }

            var cookie = new HttpCookie(AutoLoginPreferenceCookieName, enabled ? "1" : string.Empty);
            cookie.HttpOnly = false;
            cookie.Path = "/";

            if (enabled)
            {
                cookie.Expires = DateTime.UtcNow.AddDays(30);
            }
            else
            {
                cookie.Expires = DateTime.UtcNow.AddDays(-1);
            }

            Response.Cookies.Add(cookie);
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
