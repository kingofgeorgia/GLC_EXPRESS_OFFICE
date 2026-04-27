using System;
using System.Web.UI;
using GLC_EXPRESS.Services;

namespace GLC_EXPRESS
{
    public partial class About : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Title = T("NavAbout");
        }

        protected string T(string key)
        {
            return PublicSiteLocalizationService.GetText(key);
        }
    }
}