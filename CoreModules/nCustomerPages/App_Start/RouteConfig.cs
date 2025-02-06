using nCustomerPages.Controllers;
using System.Web.Mvc;
using System.Web.Routing;

namespace nCustomerPages
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

            EmbeddedCustomerPagesController.RegisterRoutes(routes);
            AnonymousEmbeddedCustomerPagesController.RegisterRoutes(routes);

            routes.MapMvcAttributeRoutes();
        }
    }
}
