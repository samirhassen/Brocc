using System.Web.Mvc;
using System.Web.Routing;

namespace nCustomer
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

            routes.MapRoute(
                name: "CreateCustomer",
                url: "CreateCustomer",
                defaults: new { controller = "Customer", action = "CreateCustomer" }
            );

            routes.MapMvcAttributeRoutes();

            Controllers.Api.ApiHostController.ApiHost.Value.SetupRouting(routes);

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}",
                defaults: new { controller = "Common", action = "Hb" }
            );
        }
    }
}
