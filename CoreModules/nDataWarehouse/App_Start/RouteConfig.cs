using System.Web.Mvc;
using System.Web.Routing;

namespace nDataWarehouse
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

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

            Controllers.ApiHostController.ApiHost.Value.SetupRouting(routes);
            Controllers.SpaHostController.SetupRouting(routes);
        }
    }
}
