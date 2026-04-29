using System;
using System.Web;
using System.Web.Security;
using System.Web.UI;

namespace GLC_EXPRESS
{
    public partial class Logout : Page
    {
        private const string AutoLoginPreferenceCookieName = "glc-auto-login";

        protected void Page_Load(object sender, EventArgs e)
        {
            FormsAuthentication.SignOut();
            ClearAutoLoginPreference();
            Response.Redirect("~/", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void ClearAutoLoginPreference()
        {
            if (Response == null)
            {
                return;
            }

            var cookie = new HttpCookie(AutoLoginPreferenceCookieName, string.Empty);
            cookie.HttpOnly = false;
            cookie.Path = "/";
            cookie.Expires = DateTime.UtcNow.AddDays(-1);
            Response.Cookies.Add(cookie);
        }
    }
}
