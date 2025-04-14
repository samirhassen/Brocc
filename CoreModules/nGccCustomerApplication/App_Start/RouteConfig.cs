using nGccCustomerApplication.Controllers.EmbeddedCustomerApplication;
using System.Web.Mvc;
using System.Web.Routing;

namespace nGccCustomerApplication
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

            EmbeddedCustomerApplicationController.RegisterRoutes(routes);
            AnonymousEmbeddedCustomerApplicationController.RegisterRoutes(routes);

            routes.MapMvcAttributeRoutes();            
        }
    }
}
