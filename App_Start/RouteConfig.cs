using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Routing;
using Microsoft.AspNet.FriendlyUrls;
using Microsoft.AspNet.FriendlyUrls.Resolvers;

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
            routes.EnableFriendlyUrls(settings, new ResponsiveWebFormsUrlResolver());
        }
    }
    // Site.Master already adapts to mobile. The legacy Site.Mobile.Master has
    // neither our styles nor navigation and must not replace it by user agent.
    public sealed class ResponsiveWebFormsUrlResolver : WebFormsFriendlyUrlResolver
    {
        protected override bool TrySetMobileMasterPage(HttpContextBase httpContext, System.Web.UI.Page page, string mobileSuffix)
        {
            return false;
        }
    }
}
