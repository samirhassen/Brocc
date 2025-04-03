using Microsoft.Owin;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.Eventing;
using Owin;
using Serilog;
using Serilog.Core.Enrichers;
using System;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

[assembly: OwinStartup(typeof(nTest.App_Start.Startup1))]

namespace nTest.App_Start
{
    public class Startup1
    {
        public void Configuration(IAppBuilder app)
        {
            if (NEnv.IsProduction)
                throw new Exception("nTest is not allowed to run in production!");

            var automationUser = new Lazy<NTechSelfRefreshingBearerToken>(() => NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, NEnv.AutomationUsernameAndPassword));
            Log.Logger = new LoggerConfiguration()
                                       .Enrich.WithMachineName()
                                       .Enrich.FromLogContext()
                                       .Enrich.With(
                                           new PropertyEnricher("ServiceName", "nTest"),
                                           new PropertyEnricher("ServiceVersion", System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString())
                                       )
                                       .WriteTo.Sink(new NTechSerilogSink(n => NEnv.ServiceRegistry.Internal[n], bearerToken: automationUser), NEnv.IsVerboseLoggingEnabled
                                           ? Serilog.Events.LogEventLevel.Debug
                                           : Serilog.Events.LogEventLevel.Information)
                                       .CreateLogger();

            NLog.Information("{EventType}: {environment} mode", "ServiceStarting", NEnv.IsProduction ? "prod" : "dev");

            app.Use<NTechLoggingMiddleware>("nTest");

            LoginSetupSupport.SetupLogin(app, "nTest", LoginSetupSupport.LoginMode.BothUsersAndApi, NEnv.IsProduction, NEnv.ServiceRegistry, NEnv.ClientCfg);

            //Start the background worker
            NTechEventHandler.CreateAndLoadSubscribers(
                typeof(nTest.Global).Assembly,
                new System.Collections.Generic.List<string>());


            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            RegisterBundles();
            NTechHardenedMvcModelBinder.Register(NEnv.CurrentServiceName);
            GlobalFilters.Filters.Add(new NTech.Services.Infrastructure.NTechHandleErrorAttribute());
            GlobalFilters.Filters.Add(new NTech.Services.Infrastructure.NTechAuthorizeAttribute());
            GlobalContentSecurityPolicyFilters.RegisterGlobalFilters(GlobalFilters.Filters);

            TimeMachine.SharedInstance.Init();
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
                    "~/Content/js/bootstrap.js",
                    "~/Content/js/moment.min.js",
                    "~/Content/js/toastr.min.js"
                };

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
            var angularScripts = new string[]
                {
                    "~/Content/js/angular.min.js",
                    $"~/Content/js/angular-locale_{getAngularLocale()}.js",
                    "~/Content/js/angular-resource.min.js",
                    "~/Content/js/ntech.js.shared.js",
                    "~/Content/js/ntech-forms.js",
                    "~/Content/js-transpiled/ntest-api-client.js"
                };

            bundles.Add(new ScriptBundle("~/Content/js/bundle-base")
                .Include(sharedScripts));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-basewithangular")
                .Include(sharedScripts)
                .Include(angularScripts));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-main-addcustomapplication")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/angular-jsoneditor.js")
                .Include("~/Content/js/controllers/main-addcustomapplication.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-main-addcustomsavingsapplication")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/angular-jsoneditor.js")
                .Include("~/Content/js/controllers/main-addcustom-savings-application.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-phonenrs")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/libphonenumber.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-mortgageloans-createloan")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/angular-jsoneditor.js")
                .Include("~/Content/js-transpiled/controllers/MortgageLoan/mortgageloans-createloan.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-mortgageloan-create-application")
            .Include(sharedScripts)
            .Include(angularScripts)
            .Include("~/Content/js/angular-jsoneditor.js")
            .Include("~/Content/js-transpiled/controllers/MortgageLoan/mortgageloan-create-application.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-create-direct-debit")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/create-dd-files.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-companyloan-create-application")
            .Include(sharedScripts)
            .Include(angularScripts)
            .Include("~/Content/js/angular-jsoneditor.js")
            .Include("~/Content/js-transpiled/controllers/CompanyLoan/create-application.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-companyloan-create-loan")
            .Include(sharedScripts)
            .Include(angularScripts)
            .Include("~/Content/js/angular-jsoneditor.js")
            .Include("~/Content/js-transpiled/controllers/CompanyLoan/create-loan.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-edit-testentity")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/angular-jsoneditor.js")
                .Include("~/Content/js-transpiled/controllers/Main/edit-testentity.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-main-index")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/angular-jsoneditor.js")
                .Include("~/Content/js-transpiled/controllers/Main/main-index.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-main-paymentPlanCalculation")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js-transpiled/controllers/Main/main-paymentPlanCalculation.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-edit-creditcollateral")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/angular-jsoneditor.js")
                .Include("~/Content/js-transpiled/controllers/Main/main-edit-creditcollateral.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-create-payment-file")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js-transpiled/controllers/Main/create-payment-file.js"));
        }
    }
}
