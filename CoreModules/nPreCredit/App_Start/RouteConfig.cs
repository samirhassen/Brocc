﻿using System.Web.Mvc;
using System.Web.Routing;

namespace nPreCredit
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
                name: "Hb2",
                url: "",
                defaults: new { controller = "Common", action = "Hb" }
            );

            routes.MapRoute(
                name: "Error",
                url: "error",
                defaults: new { controller = "Common", 
                    action = "Error" }
            );

            routes.MapMvcAttributeRoutes();



            NTech.Services.Infrastructure.NTechWs.ApiHostControllerHelper.RegisterRoutes(Controllers.Api.ApiHostController.RoutePrefix, routes);
        }
    }
}
