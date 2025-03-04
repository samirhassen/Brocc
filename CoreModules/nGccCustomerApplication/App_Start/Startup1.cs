using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using Serilog;
using Serilog.Core.Enrichers;
using nGccCustomerApplication.Code;
using NTech.Services.Infrastructure;
using System.Net;
using System.Web.Optimization;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;

[assembly: OwinStartup(typeof(nGccCustomerApplication.App_Start.Startup1))]

namespace nGccCustomerApplication.App_Start
{
    public class Startup1
    {
        public void Configuration(IAppBuilder app)
        {
            if (!NEnv.IsProduction)
            {
                ServicePointManager.ServerCertificateValidationCallback =
                    (sender, certificate, chain, sslPolicyErrors) => true;
            }
            var automationUser = new Lazy<NTechSelfRefreshingBearerToken>(() => NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, NEnv.SystemUserCredentials));
            Log.Logger = new LoggerConfiguration()
                            .Enrich.WithMachineName()
                            .Enrich.FromLogContext()
                            .Enrich.With(
                                new PropertyEnricher("ServiceName", "nGccCustomerApplication"),
                                new PropertyEnricher("ServiceVersion", System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString())
                            )
                            .WriteTo.Sink(new NTechSerilogSink(n => NEnv.ServiceRegistry.Internal[n], bearerToken: automationUser), NEnv.IsVerboseLoggingEnabled
                                ? Serilog.Events.LogEventLevel.Debug
                                : Serilog.Events.LogEventLevel.Information)
                            .CreateLogger();

            NLog.Information("{EventType}: {environment} mode", "ServiceStarting", NEnv.IsProduction ? "prod" : "dev");

            app.Use<NTechLoggingMiddleware>("nGccCustomerApplication");

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            NTechHardenedMvcModelBinder.Register(NEnv.CurrentServiceName);
            GlobalFilters.Filters.Add(new NTechHandleErrorAttribute());
            RegisterBundles();
        }

        //http://www.asp.net/mvc/overview/performance/bundling-and-minification
        private static void RegisterBundles()
        {
            BundleTable.EnableOptimizations = NEnv.IsBundlingEnabled;
            var bundles = BundleTable.Bundles;
            bundles.UseCdn = false;

            bundles.Add(new StyleBundle("~/Content/css/bundle-balanzia-application")
                .Include("~/Content/css/reset.css")
                .Include("~/Content/css/balanzia-application.css"));

            bundles.Add(new StyleBundle("~/Content/css/bundle-balanzia-wrapper-direct")
                .Include("~/Content/css/reset.css")
                .Include("~/Content/css/balanzia-wrapper-direct.css"));

            var sharedJs = new string[]
            {
                "~/Content/js/jquery-1.12.4.min.js",
                "~/Content/js/angular.min.js",
                "~/Content/js/angular-locale_fi-fi.js",
                "~/Content/js/moment.min.js",
                "~/Content/js/ntech-forms.js",
                "~/Content/js/angular-cookies.min.js",
                "~/Content/js/angular-translate.min.js",
                "~/Content/js/angular-translate-storage-cookie.min.js",
                "~/Content/js/angular-translate-storage-local.min.js",
                "~/Content/js/angular-translate-loader-url.min.js",
                "~/Content/js/country-functions-fi.js"
            };

            bundles.Add(new ScriptBundle("~/Content/js/bundle-balanzia-application")
                .Include(sharedJs)
                .Include("~/Content/js/application.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-balanzia-wrapper-direct")
                .Include(sharedJs)
                .Include("~/Content/js/angular-fileupload.js")
                .Include("~/Content/js/wrapper-direct/main-additionalquestions.js")
                .Include("~/Content/js/wrapper-direct/main-documentcheck.js")
                .Include("~/Content/js/wrapper-direct/main-documentsource.js")
                .Include("~/Content/js/wrapper-direct/main.js"));
        }
    }
}
