using System.Web.Mvc;
using System.Web.Routing;
using nSavings.Controllers.Api;

namespace nSavings;

public static class RouteConfig
{
    public static void RegisterRoutes(RouteCollection routes)
    {
        routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

        routes.MapRoute(
            name: "Hb1",
            url: "hb",
            defaults: new { controller = "Common", action = "Hb" }
        );

        routes.MapRoute(
            name: "Hb2",
            url: "",
            defaults: new { controller = "Common", action = "Hb" }
        );

        routes.MapRoute(
            name: "Error",
            url: "error",
            defaults: new { controller = "Common", action = "Error" }
        );

        routes.MapMvcAttributeRoutes();

        ApiHostController.ApiHost.Value.SetupRouting(routes);
    }
}