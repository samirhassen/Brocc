using Microsoft.Owin;
using nCredit.Code;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.Eventing;
using Owin;
using Serilog;
using Serilog.Core.Enrichers;
using System;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

[assembly: OwinStartup(typeof(nCredit.App_Start.Startup1))]

namespace nCredit.App_Start
{
    public class Startup1
    {
        public void Configuration(IAppBuilder app)
        {
            app.Use(async (context, next) =>
            {
                await next.Invoke();

                // Equivalent to Application_EndRequest in Global.asax
                var httpContext = context.Get<HttpContextBase>(typeof(HttpContextBase).FullName)
                                  ?? new HttpContextWrapper(HttpContext.Current);

                if (httpContext.Items[LoginSetupSupport.NTech401JsonItemName] != null)
                {
                    //IIS is trying to destroy the response ...
                    var body = (string)httpContext.Items[LoginSetupSupport.NTech401JsonItemName];
                    var response = httpContext.Response;
                    response.Clear();
                    response.TrySkipIisCustomErrors = true;
                    response.StatusCode = 401;
                    response.ContentType = "application/json";
                    response.Write(body);
                }
            });

            var automationUser = new Lazy<NTechSelfRefreshingBearerToken>(() => NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, NEnv.ApplicationAutomationUsernameAndPassword));
            Log.Logger = new LoggerConfiguration()
                                       .Enrich.WithMachineName()
                                       .Enrich.FromLogContext()
                                       .Enrich.With(
                                           new PropertyEnricher("ServiceName", "nCredit"),
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
                    app.Use<NTechVerboseRequestLogMiddleware>(new System.IO.DirectoryInfo(System.IO.Path.Combine(logFolder.FullName, "RawRequests")), "nCredit");
            }

            app.Use<NTechLoggingMiddleware>("nCredit");

            LoginSetupSupport.SetupLogin(app, "nCredit", LoginSetupSupport.LoginMode.BothUsersAndApi, NEnv.IsProduction, NEnv.ServiceRegistry, NEnv.ClientCfg);

            //Start the background worker
            NTechEventHandler.CreateAndLoadSubscribers(
                typeof(nCredit.Global).Assembly,
                NEnv.EnabledPluginNames,
                additionalPluginFolders: NEnv.PluginSourceFolders);

            NTech.ClockFactory.Init();

            AutoMapperHelper.Initialize(cfg => cfg.AddMaps(new[] { typeof(Global) }));

            // Code that runs on application startup
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            RegisterBundles();
            ValueProviderFactories.Factories.Remove(ValueProviderFactories.Factories.OfType<JsonValueProviderFactory>().FirstOrDefault());
            ValueProviderFactories.Factories.Add(new JsonNetValueProviderFactory());
            NTechHardenedMvcModelBinder.Register(NEnv.CurrentServiceName);
            GlobalFilters.Filters.Add(new NTechHandleErrorAttribute());
            GlobalFilters.Filters.Add(new NTechAuthorizeAttribute() { ValidateAccessToken = true });
            GlobalFilters.Filters.Add(new ConvertJsonToCamelCaseActionFilterAttribute());

            GlobalContentSecurityPolicyFilters.RegisterGlobalFilters(GlobalFilters.Filters);

            ModelBinders.Binders.Add(typeof(decimal), new Code.CommaAndDotDecimalModelBinder());
            ModelBinders.Binders.Add(typeof(decimal?), new Code.CommaAndDotDecimalModelBinder());

            CreditContext.InitDatabase();
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
        }

        //http://www.asp.net/mvc/overview/performance/bundling-and-minification
        private static void RegisterBundles()
        {
            BundleTable.EnableOptimizations = NEnv.IsBundlingEnabled;
            var bundles = BundleTable.Bundles;

            RegisterStyles(bundles);

            var sharedScripts = new string[]
                {
                    "~/Content/jsexternal/jquery-1.12.4.js",
                    "~/Content/jsexternal/jquery.flexselect.js",
                    "~/Content/jsexternal/liquidmetal.js",
                    "~/Content/jsexternal/bootstrap.js",
                    "~/Content/jsexternal/toastr.min.js",
                    "~/Content/jsexternal/moment.js",
                    "~/Content/jsexternal/underscore.js",
                    "~/Content/jsexternal/download.js"
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
                    "~/Content/js/components-old/customerinfo.js",
                    NEnv.IsCompanyLoansEnabled? "~/Content/js/components/companyLoans/*.js" : null,
                    "~/Content/js/angular-fileupload.js",
                    "~/Content/js/credit-api-client.js"
                }.Where(x => x != null).ToArray();

            bundles.Add(new ScriptBundle("~/Content/js/bundle-base")
                .Include(sharedScripts));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-basewithangular")
                .Include(sharedScripts)
                .Include(angularScripts));

            bundles.Add(new ScriptBundle("~/Content/js/libphonenumber")
                     .Include("~/Content/jsexternal/libphonenumber.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-manualpayment-index")
             .Include(sharedScripts)
             .Include(angularScripts)
             .Include("~/Content/js/controllers/ManualPayment/index.js")
             );

            bundles.Add(new ScriptBundle("~/Content/js/bundle-outgoingpayments-index")
             .Include(sharedScripts)
             .Include(angularScripts)
             .Include("~/Content/js/controllers/OutgoingPayments/index.js")
             );

            bundles.Add(new ScriptBundle("~/Content/js/bundle-changereferenceinterest-index")
             .Include(sharedScripts)
             .Include(angularScripts)
             .Include("~/Content/js/controllers/ChangeReferenceInterestRate/index.js")
             );

            bundles.Add(new ScriptBundle("~/Content/js/bundle-terminationlettercandidates")
                     .Include(sharedScripts)
                     .Include(angularScripts)
                     .Include("~/Content/js/controllers/TerminationLetterCandidates/index.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-precollection-worklists")
                     .Include(sharedScripts)
                     .Include(angularScripts)
                     .Include("~/Content/js/components-old/notificationslist.js")
                     .Include("~/Content/js/controllers/PreCollection/worklists.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-precollection-phonelist")
                     .Include(sharedScripts)
                     .Include(angularScripts)
                     .Include("~/Content/js/controllers/PreCollection/phonelist.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-debtcollectioncandidates")
                     .Include(sharedScripts)
                     .Include(angularScripts)
                     .Include("~/Content/js/controllers/DebtCollectionCandidates/index.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-reports")
                     .Include(sharedScripts)
                     .Include(angularScripts)
                     .Include("~/Content/js/controllers/Reports/index.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-correctandclosecredit")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/CorrectAndCloseCredit/index.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-precollectionmanagement-history")
                 .Include(sharedScripts)
                 .Include(angularScripts)
                 .Include("~/Content/js/controllers/PreCollectionManagement/history.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-einvoicefi-importmessagefile")
                 .Include(sharedScripts)
                 .Include(angularScripts)
                 .Include("~/Content/js/controllers/EInvoiceFi/importIncomingMessageFile.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-einvoicefi-errorlist")
                 .Include(sharedScripts)
                 .Include(angularScripts)
                 .Include("~/Content/js/controllers/EInvoiceFi/errorList.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-changetermsmanagement-index")
                     .Include(sharedScripts)
                     .Include(angularScripts)
                     .Include("~/Content/js/controllers/ChangeTermsManagement/index.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-api-host")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/ApiHost/apiHost.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-component-host")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/component-host.js"));
        }

    }
}
