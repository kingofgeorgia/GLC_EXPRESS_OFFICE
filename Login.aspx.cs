using System;
using System.Web.UI;
using System.Web.Security;
using GLC_EXPRESS.Services;

namespace GLC_EXPRESS
{
    public partial class Login : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.IsAuthenticated)
            {
                RedirectToTarget();
            }
        }

        protected void SignInButton_Click(object sender, EventArgs e)
        {
            ErrorPanel.Visible = false;

            if (!Page.IsValid)
            {
                return;
            }

            if (!AuthService.ValidateCredentials(UsernameTextBox.Text, PasswordTextBox.Text))
            {
                ErrorPanel.Visible = true;
                ErrorLiteral.Text = "Invalid username or password.";
                return;
            }

            FormsAuthentication.SetAuthCookie(UsernameTextBox.Text.Trim(), RememberMeCheckBox.Checked);
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

        private static bool UrlIsLocalToHost(string url)
        {
            return !string.IsNullOrWhiteSpace(url) && url.StartsWith("/", StringComparison.Ordinal) && !url.StartsWith("//", StringComparison.Ordinal);
        }
    }
}
