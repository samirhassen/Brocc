using System;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.Owin;
using nScheduler.App_Start;
using NTech;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.Eventing;
using Owin;
using Serilog;
using Serilog.Core.Enrichers;
using Serilog.Events;

[assembly: OwinStartup(typeof(Startup1))]

namespace nScheduler.App_Start
{
    public class Startup1
    {
        public void Configuration(IAppBuilder app)
        {
            var automationUser = new Lazy<NTechSelfRefreshingBearerToken>(() =>
                NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(
                    NEnv.ServiceRegistryNormal,
                    Tuple.Create(NEnv.AutomationUser.Username, NEnv.AutomationUser.Password)));
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithMachineName()
                .Enrich.FromLogContext()
                .Enrich.With(
                    new PropertyEnricher("ServiceName", "nScheduler"),
                    new PropertyEnricher("ServiceVersion",
                        Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString())
                )
                .WriteTo.Sink(
                    new NTechSerilogSink(n => NEnv.ServiceRegistryNormal.Internal.ServiceRootUri(n).ToString(),
                        bearerToken: automationUser), NEnv.IsVerboseLoggingEnabled
                        ? LogEventLevel.Debug
                        : LogEventLevel.Information)
                .CreateLogger();

            NLog.Information("{EventType}: in {environment} mode", "ServiceStarting",
                NEnv.IsProduction ? "prod" : "dev");

            app.Use<NTechLoggingMiddleware>("nScheduler");

            LoginSetupSupport.SetupLogin(app, "nScheduler", LoginSetupSupport.LoginMode.BothUsersAndApi,
                NEnv.IsProduction, NEnv.ServiceRegistryNormal, NEnv.ClientCfg);

            NTechEventHandler.CreateAndLoadSubscribers(
                typeof(Global).Assembly,
                Enumerable.Empty<string>().ToList());

            ClockFactory.Init();

            // Code that runs on application startup
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            RegisterBundles();
            NTechHardenedMvcModelBinder.Register(NEnv.CurrentServiceName);
            GlobalFilters.Filters.Add(new NTechHandleErrorAttribute());
            GlobalFilters.Filters.Add(new NTechAuthorizeAttribute());
            GlobalContentSecurityPolicyFilters.RegisterGlobalFilters(GlobalFilters.Filters);

            SchedulerContext.InitDatabase();
        }

        //http://www.asp.net/mvc/overview/performance/bundling-and-minification
        private static void RegisterBundles()
        {
            BundleTable.EnableOptimizations = NEnv.IsBundlingEnabled;
            var bundles = BundleTable.Bundles;

            RegisterStyles(bundles);

            var sharedScripts = new string[]
            {
                "~/Content/js/jquery-1.12.4.js",
                "~/Content/js/jquery.flexselect.js",
                "~/Content/js/liquidmetal.js",
                "~/Content/js/bootstrap.js"
            };

            bundles.Add(new ScriptBundle("~/Content/js/bundle-base")
                .Include(sharedScripts));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-basewithangular")
                .Include(sharedScripts)
                .Include("~/Content/js/angular.min.js")
                .Include("~/Content/js/angular-locale_sv-se.js")
                .Include("~/Content/js/ntech-forms.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-scheduledjobs-index")
                .Include(sharedScripts)
                .Include("~/Content/js/angular.min.js")
                .Include("~/Content/js/angular-locale_sv-se.js")
                .Include("~/Content/js/ntech-forms.js")
                .Include("~/Content/js/moment.min.js")
                .Include("~/Content/js/controllers/ScheduledJobs/index.js"));
        }

        private static void RegisterStyles(BundleCollection bundles)
        {
            var cdnRootUrl = NEnv.NTechCdnUrl;
            bundles.UseCdn = cdnRootUrl != null;
            Func<string, string> getCdnUrl = n =>
                cdnRootUrl == null ? null : new Uri(new Uri(cdnRootUrl), $"magellan/css/{n}").ToString();

            var sharedStyles = new string[]
            {
                "~/Content/css/bootstrap.min.css",
                "~/Content/css/toastr.css",
            };

            bundles.Add(new StyleBundle("~/Content/css/bundle-base")
                .Include(sharedStyles));

            bundles.Add(new StyleBundle("~/Content/css/bundle-magellan", getCdnUrl("magellan.css"))
                .Include("~/Content/css/magellan.css"));
        }
    }
}