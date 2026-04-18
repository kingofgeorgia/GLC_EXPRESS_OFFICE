using System;
using System.Security.Principal;
using System.Threading;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using GLC_EXPRESS.Services;

namespace GLC_EXPRESS
{
    public class Global : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AppDatabase.EnsureInitialized();
        }

        void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            if (Context == null || Context.User == null || Context.User.Identity == null || !Context.User.Identity.IsAuthenticated)
            {
                return;
            }

            var roles = AuthService.GetRolesForUser(Context.User.Identity.Name);
            var principal = new GenericPrincipal(Context.User.Identity, roles);

            Context.User = principal;
            Thread.CurrentPrincipal = principal;
        }
    }
}
