using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Routing;
using Microsoft.AspNet.FriendlyUrls;

namespace GLC_EXPRESS
{
    public static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.MapPageRoute("HomeAliasRoute", "главная", "~/Default.aspx");
            routes.MapPageRoute("PrivacyPolicyRoute", "privacy-policy", "~/PrivacyPolicy.aspx");

            var settings = new FriendlyUrlSettings();
            settings.AutoRedirectMode = RedirectMode.Permanent;
            routes.EnableFriendlyUrls(settings);
        }
    }
}
