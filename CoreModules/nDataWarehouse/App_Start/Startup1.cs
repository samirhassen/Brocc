using Microsoft.Owin;
using nDataWarehouse.Code;
using NTech.Services.Infrastructure;
using NWebsec.Csp;
using Owin;
using Serilog;
using Serilog.Core.Enrichers;
using System;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

[assembly: OwinStartup(typeof(nDataWarehouse.App_Start.Startup1))]

namespace nDataWarehouse.App_Start
{
    public class Startup1
    {
        public void Configuration(IAppBuilder app)
        {
            var automationUser = new Lazy<NTechSelfRefreshingBearerToken>(() => NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, NEnv.AutomationUsernameAndPassword));
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithMachineName()
                .Enrich.FromLogContext()
                .Enrich.With(
                    new PropertyEnricher("ServiceName", "nDataWarehouse"),
                    new PropertyEnricher("ServiceVersion", System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString())
                )
                .WriteTo.Sink(new NTechSerilogSink(n => NEnv.ServiceRegistry.Internal[n], bearerToken: automationUser), NEnv.IsVerboseLoggingEnabled
                    ? Serilog.Events.LogEventLevel.Debug
                    : Serilog.Events.LogEventLevel.Information)
                .CreateLogger();

            NLog.Information("{EventType}: in {environment} mode", "ServiceStarting", NEnv.IsProduction ? "prod" : "dev");

            app.Use<NTechLoggingMiddleware>("nDataWarehouse");

            LoginSetupSupport.SetupLogin(app, "nDataWarehouse", LoginSetupSupport.LoginMode.BothUsersAndApi, NEnv.IsProduction, NEnv.ServiceRegistry, NEnv.ClientCfg);

            NTech.ClockFactory.Init();

            // Code that runs on application startup
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            RegisterBundles();
            NTechHardenedMvcModelBinder.Register(NEnv.CurrentServiceName);
            GlobalFilters.Filters.Add(new NTechHandleErrorAttribute());
            GlobalFilters.Filters.Add(new NTechAuthorizeAttribute() { ValidateAccessToken = true });
            GlobalContentSecurityPolicyFilters.RegisterGlobalFilters(GlobalFilters.Filters);

            var support = new DwSupport();
            support.SetupDb();

            AnalyticsContext.InitDatabase();
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

            bundles.Add(new StyleBundle("~/Content/css/bundle-dashboard")
                .Include("~/Content/css/dashboard.css"));

            bundles.Add(new StyleBundle("~/Content/css/bundle-reports")
                .Include("~/Content/css/reports.css"));
        }

        //http://www.asp.net/mvc/overview/performance/bundling-and-minification
        private static void RegisterBundles()
        {
            BundleTable.EnableOptimizations = NEnv.IsBundlingEnabled;
            var bundles = BundleTable.Bundles;

            RegisterStyles(bundles);

            Func<string> getAngularLocale = () =>
            {
                var c = NEnv.ClientCfg.Country.BaseCountry;
                if (c == "SE")
                    return "sv-se";
                else if (c == "FI")
                    return "fi-fi";
                else
                    throw new NotImplementedException();
            };

            var sharedScripts = new string[]
                {
                    "~/Content/jsexternal/jquery.min.js",
                    "~/Content/jsexternal/moment.min.js",
                    "~/Content/jsexternal/jquery.flexselect.js",
                    "~/Content/jsexternal/toastr.min.js",
                    "~/Content/jsexternal/underscore.js"
                };

            var angularScripts = new string[]
                {
                    "~/Content/jsexternal/angular.min.js",
                    $"~/Content/jsexternal/angular-locale_{getAngularLocale()}.js",
                    "~/Content/jsexternal/angular-resource.min.js",
                    "~/Content/jsexternal/angular-route.js",
                    //BEGIN ANGULAR TRANSLATE
                    "~/Content/jsexternal/angular-cookies.min.js",
                    "~/Content/jsexternal/angular-translate.min.js",
                    "~/Content/jsexternal/angular-translate-storage-cookie.min.js",
                    "~/Content/jsexternal/angular-translate-storage-local.min.js",
                    "~/Content/jsexternal/angular-translate-loader-url.min.js",
                    //END ANGULAR TRANSLATE
                    "~/Content/jstranspiled/ntech_shared/common/*.js",
                    "~/Content/jstranspiled/ntech_shared/legacy/ntech.js.shared.js",
                    "~/Content/js/ntech-forms.js",
                    "~/Content/jstranspiled/ntech_shared/components/infrastructure/*.js",
                    "~/Content/jstranspiled/infrastructure/*.js",
                    "~/Content/jstranspiled/componentsbase.js",
                     "~/Content/jstranspiled/ntech_shared/components/components/*.js",
                    "~/Content/jstranspiled/components/*.js",
                    "~/Content/jstranspiled/dw-api-client.js"
                };

            bundles.Add(new ScriptBundle("~/Content/js/bundle-dashboard-settings")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/jsexternal/liquidmetal.js")
                .Include("~/Content/js/controllers/dashboard-settings.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-dashboard")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/jsexternal/Chart.min.js")
                .Include("~/Content/js/controllers/dashboard.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-view-reports")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/view-reports.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-api-host")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/jstranspiled/controllers/ApiHost/apiHost.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-spa-host")
                .Include(new string[] //CSP refuses to work with jquery on this page only in the whole system. No idea why.
                {
                    "~/Content/jsexternal/moment.min.js",
                    "~/Content/jsexternal/toastr.min.js",
                    "~/Content/jsexternal/underscore.js"
                })
                .Include(angularScripts)
                .Include("~/Content/jsexternal/Chart.min.js")
                .Include("~/Content/jstranspiled/controllers/SpaHost/spaHost.js"));
        }

        /// <summary>
        /// Endpoint generated using NWebSec to catch reports of CSP-violations and log to file.
        /// Will be triggered by (autogenerated): report-uri /WebResource.axd?cspReport=true
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void NWebsecHttpHeaderSecurityModule_CspViolationReported(object sender, CspViolationReportEventArgs e)
        {
            var violationReport = e.ViolationReport;
            var logFolder = NEnv.LogFolder;
            GlobalContentSecurityPolicyFilters.LogToFile(violationReport, logFolder);
        }
    }
}
