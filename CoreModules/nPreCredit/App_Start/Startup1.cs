using Microsoft.Owin;
using nPreCredit.Code.AffiliateReporting;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.Eventing;
using Owin;
using Serilog;
using Serilog.Core.Enrichers;
using System;
using System.Linq;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

[assembly: OwinStartup(typeof(nPreCredit.App_Start.Startup1))]

namespace nPreCredit.App_Start
{
    public class Startup1
    {
        public void Configuration(IAppBuilder app)
        {
            var cfg = new LoggerConfiguration();

            if (NEnv.IsVerboseLoggingEnabled)
            {
                cfg.MinimumLevel.Debug();
            }

            var automationUser = new Lazy<NTechSelfRefreshingBearerToken>(() => NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, NEnv.ApplicationAutomationUsernameAndPassword));
            Log.Logger = cfg
                .Enrich.WithMachineName()
                .Enrich.FromLogContext()
                .Enrich.With(
                    new PropertyEnricher("ServiceName", "nPreCredit"),
                    new PropertyEnricher("ServiceVersion", System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString())
                )
                .WriteTo.Sink(new NTechSerilogSink(n => NEnv.ServiceRegistry.Internal[n], bearerToken: automationUser), NEnv.IsVerboseLoggingEnabled
                    ? Serilog.Events.LogEventLevel.Debug
                    : Serilog.Events.LogEventLevel.Information)
                .CreateLogger();

            NLog.Information("{EventType}: {environment} mode", "ServiceStarting", NEnv.IsProduction ? "prod" : "dev");

            if (NEnv.IsVerboseLoggingEnabled)
            {
                var logFolder = NEnv.LogFolder;
                if (logFolder != null)
                    app.Use<NTechVerboseRequestLogMiddleware>(new System.IO.DirectoryInfo(System.IO.Path.Combine(logFolder.FullName, "RawRequests")), "nPreCredit");
            }

            DependancyInjectionConfig.Configure();

            LoginSetupSupport.SetupLogin(app, "nPreCredit", LoginSetupSupport.LoginMode.BothUsersAndApi, NEnv.IsProduction, NEnv.ServiceRegistry, NEnv.ClientCfg);

            //Start the background worker
            NTechEventHandler.CreateAndLoadSubscribers(
                typeof(nPreCredit.Global).Assembly,
                NEnv.EnabledPluginNames,
                additionalPluginFolders: NEnv.PluginSourceFolders,
                assemblyLoader: DependancyInjection.Services.Resolve<NTechExternalAssemblyLoader>());

            app.Use<NTechLoggingMiddleware>("nPreCredit");

            var affiliateReporting = new AffiliateReportingBackgroundTimer();
            affiliateReporting.Start(TimeSpan.FromSeconds(10));

            NTech.Services.Infrastructure.NTechWs.NTechWebserviceRequestValidator
                .InitializeValidationFramework(
                    NEnv.BaseCivicRegNumberParser.IsValid,
                    x => new NTech.Banking.BankAccounts.BankAccountNumberParser(NEnv.ClientCfg.Country.BaseCountry).TryParseFromStringWithDefaults(x, null, out _),
                    NEnv.BaseOrganisationNumberParser.IsValid);

            NTech.ClockFactory.Init();

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RegisterBundles();
            NTechHardenedMvcModelBinder.Register(NEnv.CurrentServiceName);
            GlobalFilters.Filters.Add(new NTechHandleErrorAttribute());
            GlobalFilters.Filters.Add(new NTechAuthorizeAttribute() { ValidateAccessToken = true });
            GlobalFilters.Filters.Add(new ConvertJsonToCamelCaseActionFilterAttribute());

            GlobalContentSecurityPolicyFilters.RegisterGlobalFilters(GlobalFilters.Filters);

            PreCreditContext.InitDatabase();

            ValueProviderFactories.Factories.Remove(ValueProviderFactories.Factories.OfType<JsonValueProviderFactory>().Single());
            ValueProviderFactories.Factories.Add(new Code.JsonNetValueProviderFactory());
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
                    "~/Content/css/other.css",
                    };

            bundles.Add(new StyleBundle("~/Content/css/bundle-base")
                .Include(sharedStyles));

            bundles.Add(new StyleBundle("~/Content/css/bundle-magellan", getCdnUrl("magellan.css"))
                .Include("~/Content/css/magellan.css"));

            bundles.Add(new StyleBundle("~/Content/css/bundle-credit-monitoring")
                .Include("~/Content/css/toastr.css")
                .Include("~/Content/css/credit-monitoring.css"));
        }

        //http://www.asp.net/mvc/overview/performance/bundling-and-minification
        private static void RegisterBundles()
        {
            BundleTable.EnableOptimizations = NEnv.IsBundlingEnabled;
            var bundles = BundleTable.Bundles;

            RegisterStyles(bundles);

            var sharedScripts = new string[]
                {
                    "~/Content/ts/legacy-globals.js",
                    "~/Content/jsexternal/jquery-1.12.4.js",
                    "~/Content/jsexternal/jquery.flexselect.js",
                    "~/Content/jsexternal/jquery-ui-position-only.js",
                    "~/Content/jsexternal/liquidmetal.js",
                    "~/Content/jsexternal/bootstrap.js",
                    "~/Content/jsexternal/toastr.min.js",
                    "~/Content/jsexternal/moment.js",
                    "~/Content/jsexternal/underscore.js"
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
                    "~/Content/jsexternal/angular.min.js",
                    $"~/Content/jsexternal/angular-locale_{getAngularLocale()}.js",
                    "~/Content/jsexternal/angular-resource.min.js",
                    //BEGIN ANGULAR TRANSLATE
                    "~/Content/jsexternal/angular-cookies.min.js",
                    "~/Content/jsexternal/angular-translate.min.js",
                    "~/Content/jsexternal/angular-translate-storage-cookie.min.js",
                    "~/Content/jsexternal/angular-translate-storage-local.min.js",
                    "~/Content/jsexternal/angular-translate-loader-url.min.js",
                    //END ANGULAR TRANSLATE
                    "~/Content/js/ntech_shared/common/*.js",
                    "~/Content/js/ntech_shared/legacy/ntech.js.shared.js",
                    "~/Content/js/ntech-forms.js",
                    "~/Content/js/ntech_shared/components/infrastructure/*.js",
                    "~/Content/js/infrastructure/*.js",
                    "~/Content/js/componentsbase.js",
                    "~/Content/js/ntech_shared/components/components/*.js",
                    "~/Content/js/components/*.js",
                    NEnv.IsCompanyLoansEnabled ? "~/Content/js/components/companyLoans/*.js" : null,
                    NEnv.IsMortgageLoansEnabled? "~/Content/js/components/mortgageLoans/*.js" : null,
                    "~/Content/js/angular-fileupload.js",
                    "~/Content/js/precredit-api-client.js",
                    "~/Content/js/companyloan-precredit-api-client.js"
                }.Where(x => x != null).ToArray();

            bundles.Add(new ScriptBundle("~/Content/js/bundle-base")
                .Include(sharedScripts));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-basewithangular")
                .Include(sharedScripts)
                .Include(angularScripts));

            //Credit management
            bundles.Add(new ScriptBundle("~/Content/js/bundle-creditmanagement-creditapplications")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/CreditManagement/creditapplications.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-creditmanagement-creditapplication")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/CreditManagement/creditapplication.js")
                .Include("~/Content/js/controllers/CreditManagement/creditapplication_customerinfo.js")
                .Include("~/Content/js/controllers/CreditManagement/creditapplication_documentcheckstatus.js"));

            //Credit decision
            bundles.Add(new ScriptBundle("~/Content/js/bundle-creditdecision-creditapplicationsToApprove")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/CreditDecision/creditapplicationsToApprove.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-creditdecision-creditapplicationToApprove")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/CreditDecision/creditapplicationToApprove.js"));

            //Customer service
            bundles.Add(new ScriptBundle("~/Content/js/bundle-customerservice-applicationsearch")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/CustomerService/applicationsearch.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-customerservice-applicationtoservice")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/CustomerService/applicationtoservice.js"));

            //Credit check
            bundles.Add(new ScriptBundle("~/Content/js/bundle-creditcheck-new")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/CreditCheck/decisionDetailsBasisPopupSupport.js")
                .Include("~/Content/js/controllers/CreditCheck/newCreditCheck.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-creditcheck-view")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/CreditCheck/decisionDetailsBasisPopupSupport.js")
                .Include("~/Content/js/controllers/CreditCheck/viewCreditCheck.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-creditapplicationedit-editvalue")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/CreditApplicationEdit/editValue.js"));

            //Fraud check
            bundles.Add(new ScriptBundle("~/Content/js/bundle-fraudcheck-new")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/FraudCheck/sharedbetweenNewAndView.js")
                .Include("~/Content/js/controllers/FraudCheck/new.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-fraudcheck-view")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/FraudCheck/sharedbetweenNewAndView.js")
                .Include("~/Content/js/controllers/FraudCheck/view.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-credithandlerlimitsettings-index")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/CreditHandlerLimitSettings/creditHandlerLimitSettings-index.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-handlecheckpoints-index")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/HandleCheckpoints/index.js"));

            bundles.Add(new ScriptBundle("~/Content/js/libphonenumber")
                     .Include("~/Content/jsexternal/libphonenumber.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-creditmanagementmonitor-index")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/CreditManagementMonitor/CreditManagementMonitor-index.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-testlatestemailslist")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/jsexternal/angular-sanitize.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-documentcheck-new")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/DocumentCheck/newDocumentCheck.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-mortgage-valuation-host")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/MortgageApplicationValuation/mortgage-application-valuation.js"));

            bundles.Add(new ScriptBundle("~/Content/ts/bundle-mortgage-amortization-host")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/MortgageApplicationAmortization/mortgage-application-amortization.js"));

            bundles.Add(new ScriptBundle("~/Content/ts/bundle-api-host")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/ApiHost/apiHost.js"));

            bundles.Add(new ScriptBundle("~/Content/ts/bundle-creditmanagement-archivedapplication")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/ArchivedUnsecuredLoanApplication/archived-unsecured-loan-application.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-component-host")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/component-host.js"));
        }
    }
}