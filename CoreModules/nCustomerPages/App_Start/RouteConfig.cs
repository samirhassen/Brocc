using System.Web.Mvc;
using System.Web.Routing;
using nCustomerPages.Controllers;

namespace nCustomerPages;

public static class RouteConfig
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