using nCustomer.DbModel;
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

namespace nCustomer
{
    public class MvcApplication : HttpApplication
    {
        public override void Init()
        {
            base.Init();
            NTechHttpHardening.HandleCachingAndInformationLeakHeader(this, false);
        }

        private void Application_Start(object sender, EventArgs e)
        {
            //AreaRegistration.RegisterAllAreas();
            //GlobalConfiguration.Configure(WebApiConfig.Register);
            //RouteConfig.RegisterRoutes(RouteTable.Routes);
            //RegisterBundles();
            //NTechHardenedMvcModelBinder.Register(NEnv.CurrentServiceName);
            //GlobalFilters.Filters.Add(new NTechHandleErrorAttribute());
            //GlobalFilters.Filters.Add(new NTechAuthorizeAttribute());
            //GlobalFilters.Filters.Add(new ConvertJsonToCamelCaseActionFilterAttribute());
            //GlobalContentSecurityPolicyFilters.RegisterGlobalFilters(GlobalFilters.Filters);

            //ValueProviderFactories.Factories.Remove(ValueProviderFactories.Factories.OfType<JsonValueProviderFactory>().Single());
            //ValueProviderFactories.Factories.Add(new Code.JsonNetValueProviderFactory());

            ////Make sure the db is created
            //CustomersContext.InitDatabase();
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
                    "~/Content/css/other.css"
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
                    "~/Content/jsexternal/jquery-1.12.4.js",
                    "~/Content/jsexternal/bootstrap.js",
                    "~/Content/jsexternal/toastr.min.js",
                    "~/Content/jsexternal/moment.js",
                    "~/Content/jsexternal/underscore.js"
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
                    "~/Content/js/country-functions-fi.js",
                    "~/Content/js/ntech-forms.js",
                    "~/Content/js/ntech_shared/components/infrastructure/*.js",
                    "~/Content/js/infrastructure/*.js",
                    "~/Content/js/componentsbase.js",
                    "~/Content/js/ntech_shared/components/components/*.js",
                    "~/Content/js/components/*.js",
                    "~/Content/js/angular-fileupload.js",
                    "~/Content/js/customer-api-client.js"
                };

            bundles.Add(new ScriptBundle("~/Content/js/bundle-base")
                .Include(sharedScripts));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-basewithangular")
                .Include(sharedScripts)
                .Include(angularScripts));

            //Customer card
            bundles.Add(new ScriptBundle("~/Content/js/bundle-customercard-view")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/component-host.js")
                .Include("~/Content/js/controllers/CustomerCard/customerItemPrettyPrinter.js")
                .Include("~/Content/js/controllers/CustomerCard/customerCard.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-customercard-conflicts")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/CustomerCard/customerItemPrettyPrinter.js")
                .Include("~/Content/js/controllers/CustomerCard/resolveConflicts.js"));

            bundles.Add(new ScriptBundle("~/Content/js/libphonenumber")
                     .Include("~/Content/jsexternal/libphonenumber.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-kycmanagement-manage")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/KycManagement/kycmanagement-manage.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-kycmanagement-fatcacrs")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/KycManagement/kycmanagement-fatcacrs.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-api-host")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/ApiHost/apiHost.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-addcustomer")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/CustomerCard/addCustomer.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-component-host")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/component-host.js"));
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