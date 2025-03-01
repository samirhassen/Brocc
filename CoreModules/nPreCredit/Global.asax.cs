using nPreCredit.Code;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using NWebsec.Csp;
using System;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace nPreCredit
{
    public class Global : HttpApplication
    {
        public override void Init()
        {
            base.Init();
            NTechHttpHardening.HandleCachingAndInformationLeakHeader(this, false);
        }

        private void Application_Start(object sender, EventArgs e)
        {
            AutoMapperHelper.Initialize(cfg => cfg.AddMaps(new[] { typeof(Global) }));

            // Code that runs on application startup
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
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

        private void Application_EndRequest(object sender, EventArgs e)
        {
        }

        protected void Application_Error()
        {
            NTechHttpHardening.HandleGlobal_Asax_ApplicationError(Server);
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
                    "~/Content/ts/legacy-globals.ts",
                    "~/Content/jsexternal/jquery-1.12.4.ts",
                    "~/Content/jsexternal/jquery.flexselect.ts",
                    "~/Content/jsexternal/jquery-ui-position-only.ts",
                    "~/Content/jsexternal/liquidmetal.ts",
                    "~/Content/jsexternal/bootstrap.ts",
                    "~/Content/jsexternal/toastr.min.ts",
                    "~/Content/jsexternal/moment.ts",
                    "~/Content/jsexternal/underscore.ts"
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
                    "~/Content/ts/ntech_shared/common/*.ts",
                    "~/Content/ts/ntech_shared/legacy/ntech.js.shared.ts",
                    "~/Content/ts/ntech-forms.ts",
                    "~/Content/ts/ntech_shared/components/infrastructure/*.ts",
                    "~/Content/ts/infrastructure/*.ts",
                    "~/Content/ts/componentsbase.ts",
                    "~/Content/ts/ntech_shared/components/components/*.ts",
                    "~/Content/ts/components/*.ts",
                    NEnv.IsCompanyLoansEnabled ? "~/Content/ts/components/companyLoans/*.ts" : null,
                    NEnv.IsMortgageLoansEnabled? "~/Content/ts/components/mortgageLoans/*.ts" : null,
                    "~/Content/ts/angular-fileupload.ts",
                    "~/Content/ts/precredit-api-client.ts",
                    "~/Content/ts/companyloan-precredit-api-client.ts"
                }.Where(x => x != null).ToArray();

            bundles.Add(new ScriptBundle("~/Content/ts/bundle-base")
                .Include(sharedScripts));

            bundles.Add(new ScriptBundle("~/Content/ts/bundle-basewithangular")
                .Include(sharedScripts)
                .Include(angularScripts));

            //Credit management
            bundles.Add(new ScriptBundle("~/Content/ts/bundle-creditmanagement-creditapplications")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/ts/controllers/CreditManagement/creditapplications.ts"));

            bundles.Add(new ScriptBundle("~/Content/ts/bundle-creditmanagement-creditapplication")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/ts/controllers/CreditManagement/creditapplication.ts")
                .Include("~/Content/ts/controllers/CreditManagement/creditapplication_customerinfo.ts")
                .Include("~/Content/ts/controllers/CreditManagement/creditapplication_documentcheckstatus.ts"));

            //Credit decision
            bundles.Add(new ScriptBundle("~/Content/ts/bundle-creditdecision-creditapplicationsToApprove")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/ts/controllers/CreditDecision/creditapplicationsToApprove.ts"));

            bundles.Add(new ScriptBundle("~/Content/ts/bundle-creditdecision-creditapplicationToApprove")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/ts/controllers/CreditDecision/creditapplicationToApprove.ts"));

            //Customer service
            bundles.Add(new ScriptBundle("~/Content/ts/bundle-customerservice-applicationsearch")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/ts/controllers/CustomerService/applicationsearch.ts"));

            bundles.Add(new ScriptBundle("~/Content/ts/bundle-customerservice-applicationtoservice")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/ts/controllers/CustomerService/applicationtoservice.ts"));

            //Credit check
            bundles.Add(new ScriptBundle("~/Content/ts/bundle-creditcheck-new")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/ts/controllers/CreditCheck/decisionDetailsBasisPopupSupport.ts")
                .Include("~/Content/ts/controllers/CreditCheck/newCreditCheck.ts"));

            bundles.Add(new ScriptBundle("~/Content/ts/bundle-creditcheck-view")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/ts/controllers/CreditCheck/decisionDetailsBasisPopupSupport.ts")
                .Include("~/Content/ts/controllers/CreditCheck/viewCreditCheck.ts"));

            bundles.Add(new ScriptBundle("~/Content/ts/bundle-creditapplicationedit-editvalue")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/ts/controllers/CreditApplicationEdit/editValue.ts"));

            //Fraud check
            bundles.Add(new ScriptBundle("~/Content/ts/bundle-fraudcheck-new")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/ts/controllers/FraudCheck/sharedbetweenNewAndView.ts")
                .Include("~/Content/ts/controllers/FraudCheck/new.ts"));

            bundles.Add(new ScriptBundle("~/Content/ts/bundle-fraudcheck-view")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/ts/controllers/FraudCheck/sharedbetweenNewAndView.ts")
                .Include("~/Content/ts/controllers/FraudCheck/view.ts"));

            bundles.Add(new ScriptBundle("~/Content/ts/bundle-credithandlerlimitsettings-index")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/ts/controllers/CreditHandlerLimitSettings/creditHandlerLimitSettings-index.ts"));

            bundles.Add(new ScriptBundle("~/Content/ts/bundle-handlecheckpoints-index")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/ts/controllers/HandleCheckpoints/index.ts"));

            bundles.Add(new ScriptBundle("~/Content/ts/libphonenumber")
                     .Include("~/Content/jsexternal/libphonenumber.ts"));

            bundles.Add(new ScriptBundle("~/Content/ts/bundle-creditmanagementmonitor-index")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/ts/controllers/CreditManagementMonitor/CreditManagementMonitor-index.ts"));

            bundles.Add(new ScriptBundle("~/Content/ts/bundle-testlatestemailslist")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/jsexternal/angular-sanitize.ts"));

            bundles.Add(new ScriptBundle("~/Content/ts/bundle-documentcheck-new")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/ts/controllers/DocumentCheck/newDocumentCheck.ts"));

            bundles.Add(new ScriptBundle("~/Content/ts/bundle-mortgage-valuation-host")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/ts/controllers/MortgageApplicationValuation/mortgage-application-valuation.ts"));

            bundles.Add(new ScriptBundle("~/Content/ts/bundle-mortgage-amortization-host")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/ts/controllers/MortgageApplicationAmortization/mortgage-application-amortization.ts"));

            bundles.Add(new ScriptBundle("~/Content/ts/bundle-api-host")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/ts/controllers/ApiHost/apiHost.ts"));

            bundles.Add(new ScriptBundle("~/Content/ts/bundle-creditmanagement-archivedapplication")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/ts/controllers/ArchivedUnsecuredLoanApplication/archived-unsecured-loan-application.ts"));

            bundles.Add(new ScriptBundle("~/Content/ts/bundle-component-host")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/ts/controllers/component-host.ts"));
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