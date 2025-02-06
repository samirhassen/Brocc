using nBackOffice.Controllers;
using System.Web.Mvc;
using System.Web.Routing;

namespace nBackOffice
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

            EmbeddedBackOfficeController.RegisterRoutes(routes);

            routes.MapMvcAttributeRoutes();

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}/{id2}",
                defaults: new { controller = "Secure", action = "NavMenu", id = UrlParameter.Optional, id2 = UrlParameter.Optional }
            );
        }
    }
}
