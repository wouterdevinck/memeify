using System.Web.Mvc;
using System.Web.Routing;

// ReSharper disable CheckNamespace

namespace Memeify.Web {

    public class RouteConfig {

        public static void RegisterRoutes(RouteCollection routes) {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.MapRoute("Default", "{controller}/{action}/{id}", new { controller = "Home", action = "Index", id = UrlParameter.Optional });
        }

    }

}

// ReSharper restore CheckNamespace