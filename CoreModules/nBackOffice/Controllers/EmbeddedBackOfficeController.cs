using Newtonsoft.Json.Linq;
using NTech;
using NTech.Services.Infrastructure;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;

namespace nBackOffice.Controllers
{
    [NTechAuthorize()]
    public class EmbeddedBackOfficeController : Controller
    {
        /*        
        Given a angular app started with 
        ng build --base-href='/s/' --watch  
        That has at least the routes: '/' and '/app'
        You can surf to /s/ and /s/app/
        The reason this controller is needed is so /s/app doesnt 404 since mvc will by default expect that to be a request for the static file
        /s/app/index.html (which is why /s just works since it loads /s/index.html ... which is also why we replace with that)

        This should probably be dynamic so we can have several plugins but that can be added as needed. The s prefix would then be from config         
         */
        [AllowAnonymous]
        public ActionResult Content()
        {
            this.Response.Headers.Remove("ETag");
            this.Response.Headers["Cache-Control"] = "max-age=0, no-cache, no-store, must-revalidate";
            this.Response.Headers["Pragma"] = "no-cache";
            this.Response.Headers["Expires"] = "Wed, 12 Jan 1980 05:00:00 GMT";
            return File(Server.MapPath("/s/index.html"), "text/html");
        }

        [AllowAnonymous]
        public ActionResult LoginLooping()
        {
            //For developers debugging this: Check the angular app LoginManager. This happens when we redirect back to nUser
            //shotly after just doing so to prevent infinite loops. Typical case is that the user has an account but not enough permissions
            return Content("Login is looping. Check for configuration errors or contact support if you are a user seeing this.");
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("s/assets/{*pathInfo}");

            //https://stackoverflow.com/questions/4684012/how-to-ignore-all-of-a-particular-file-extension-in-mvc-routing
            Action<string> ignoreExtension = ext => routes.IgnoreRoute("{*ext}", new { ext = @".*\." + ext + "(/.*)?" });
            ignoreExtension("woff");
            ignoreExtension("woff2");
            ignoreExtension("eot");
            ignoreExtension("svg");
            ignoreExtension("ttf");
            ignoreExtension("ico");

            routes.MapRoute(
                name: "LoginLooping",
                url: "s/login-looping",
                defaults: new { controller = "EmbeddedBackOffice", action = "LoginLooping" });

            routes.MapRoute(
                name: "EmbeddedBackOffice",
                url: "s/{*path}",
                defaults: new { controller = "EmbeddedBackOffice", action = "Content" }
            );
        }

        //Used to bootstrap the login process.
        [Route("api/embedded-backoffice/fetch-auth-config")]
        [AllowAnonymous]
        public ActionResult FetchAuthConfig()
        {
            return new JsonNetActionResult
            {
                Data = new
                {
                    UserModuleRootUrl = NEnv.ServiceRegistry.Internal.ServiceRootUri("nUser")
                }
            };
        }

        [Route("api/embedded-backoffice/fetch-config")]
        [NTechAuthorize()]
        public ActionResult FetchConfig()
        {
            //TODO: Module search
            var clientConfig = NEnv.ClientCfg;

            var activeServiceNames = NEnv.ServiceRegistry.Internal.Keys
                .Concat(NEnv.ServiceRegistry.External.Keys).DistinctPreservingOrder()
                .ToList();

            var config = new
            {
                IsTest = !NEnv.IsProduction,
                ReleaseNumber = NTechSimpleSettings.GetValueFromClientResourceFile("CurrentReleaseMetadata.txt", "releaseNumber"),
                LogoutUrl = Url.Action("Logout", "Secure"),
                BackOfficeUrl = new Uri(NEnv.ServiceRegistry.External["nBackoffice"]).ToString(),
                ServiceRegistry = NEnv.ServiceRegistry.External.ToDictionary(x => x.Key, x => x.Value),
                Skinning = NEnv.IsSkinningEnabled ? new
                {
                    LogoUrl = Url.Content("~/Skinning/img/menu-header-logo.png")
                } : null,
                Client = new
                {
                    ClientName = clientConfig.ClientName,
                    BaseCountry = clientConfig.Country.BaseCountry,
                    BaseCurrency = clientConfig.Country.BaseCurrency
                },
                ActiveServiceNames = activeServiceNames,
                ActiveFeatures = clientConfig.ActiveFeatures?.Select(x => x.ToLowerInvariant())?.ToHashSet(),
                Settings = clientConfig.Settings,
                CurrentDateAndTime = ClockFactory.SharedInstance.Now.ToString("O"),
                HasEmailProvider = NTech.Services.Infrastructure.Email.NTechEmailServiceFactory.HasEmailProvider
            };
            return new JsonNetActionResult
            {
                Data = config
            };
        }

        [Route("api/embedded-backoffice/settings")]
        [NTechAuthorize()]
        public ActionResult FetchSettings()
        {
            var settingsModelRaw = NHttp.Begin(
                    NEnv.ServiceRegistry.Internal.ServiceRootUri("nCustomer"),
                    NHttp.GetCurrentAccessToken()).PostJson("Api/Settings/FetchModels", new { })
                .ParseAsRawJson();

            return new JsonNetActionResult
            {
                Data = new
                {
                    Model = JObject.Parse(settingsModelRaw)
                }
            };
        }

        public static void AddCsp(GlobalFilterCollection filters)
        {
            GlobalContentSecurityPolicyFilters.RegisterGlobalFilters(filters, overrideBaseUriSetup: x =>
            {
                //Needed since the embedded angular app needs to set a relative /s/ base url which requires self
                x.Add(new NWebsec.Mvc.HttpHeaders.Csp.CspBaseUriAttribute { Self = true });
                x.Add(new NWebsec.Mvc.HttpHeaders.Csp.CspBaseUriReportOnlyAttribute { Self = true });
                x.Add(new NWebsec.Mvc.HttpHeaders.Csp.CspFrameSrcAttribute { CustomSources = "*.kreditz.com" }); //To allow embedding their iframe
            },
            //When running angular in development mode this is needed. In prod or when running angular in prod mode (like on dev/at) this is not needed.
            scriptsAllowUnsafeEval: NEnv.IsDevelopingOnLocalHost);
        }
    }
}