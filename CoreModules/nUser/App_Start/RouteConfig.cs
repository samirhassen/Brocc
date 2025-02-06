using System.Web.Mvc;
using System.Web.Routing;

namespace nUser
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Api",
                url: "api",
                defaults: new { controller = "Common", action = "Api" }
            );

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

            routes.MapRoute(
                name: "ValidCivicRegNr",
                url: "User/ValidCivicRegNr/{value}",
                defaults: new { controller = "User", action = "ValidCivicRegNr" }
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
