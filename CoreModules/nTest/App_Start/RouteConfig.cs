using System.Web.Mvc;
using System.Web.Routing;

namespace nTest
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute("loggedRequest", "Api/LoggedRequest/{*subRoute}", new { controller = "LoggedRequest", action = "ReceiveRequest" });

            routes.MapRoute(
                name: "Hb",
                url: "hb",
                defaults: new { controller = "Common", action = "Hb" }
            );

            routes.MapRoute(
                name: "Error",
                url: "error",
                defaults: new { controller = "Common", action = "Error" }
            );

            routes.MapMvcAttributeRoutes();

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}",
                defaults: new { controller = "Common", action = "Hb" }
            );
        }
    }
}
