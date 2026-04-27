using System;
using System.Text;
using System.Web.UI;
using GLC_EXPRESS.Services;

namespace GLC_EXPRESS
{
    public partial class PrivacyPolicy : Page
    {
        protected string T(string key)
        {
            return PublicSiteLocalizationService.GetText(key);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Title = T("PrivacyPageTitle");
            Response.ContentEncoding = Encoding.UTF8;
            Response.Charset = "utf-8";
        }
    }
}