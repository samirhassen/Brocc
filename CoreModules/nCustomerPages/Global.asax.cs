using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace nCustomerPages
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
          // Code that runs on application startup
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            RegisterBundles();
            NTechHardenedMvcModelBinder.Register(NEnv.CurrentServiceName);
            GlobalFilters.Filters.Add(new NTechHandleErrorAttribute());
        }

        private static void RegisterStyles(BundleCollection bundles)
        {
            var cdnRootUrl = NEnv.NTechCdnUrl;
            bundles.UseCdn = cdnRootUrl != null;
            Func<string, string> getCdnUrl = n =>
                cdnRootUrl == null ? null : new Uri(new Uri(cdnRootUrl), $"magellan-customerpages/css/{n}").ToString();

            var sharedStyles = new string[]
                    {
                    "~/Content/css/bootstrap.min.css",
                    "~/Content/css/toastr.css",
                    };

            bundles.Add(new StyleBundle("~/Content/css/bundle-base")
                .Include(sharedStyles));

            bundles.Add(new StyleBundle("~/Content/css/bundle-magellan-customerpages", getCdnUrl("magellan-customerpages.css"))
                .Include("~/Content/css/magellan-customerpages.css"));

            bundles.Add(new StyleBundle("~/Content/css/bundle-ml-magellan-customerpages", getCdnUrl("ml-magellan-customerpages.css"))
                .Include("~/Content/css/ml-magellan-customerpages.css"));

            bundles.Add(new StyleBundle("~/Content/css/embedded-customerpages-imitation")
                .Include(sharedStyles)
                .Include("~/Content/css/embedded-customerpages-imitation.css"));
        }

        //http://www.asp.net/mvc/overview/performance/bundling-and-minification
        private static void RegisterBundles()
        {
            BundleTable.EnableOptimizations = NEnv.IsBundlingEnabled;
            var bundles = BundleTable.Bundles;

            RegisterStyles(bundles);

            bundles.Add(new ScriptBundle("~/Content/js/bundle-layout-support")
                .Include("~/Content/js/layout-support.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-handle-angular-accessdenied")
                .Include("~/Content/js/handle-angular-accessdenied.js"));

            var sharedScripts = new string[]
                {
                    "~/Content/js/jquery-1.12.4.js",
                    "~/Content/js/bootstrap.js",
                    "~/Content/js/toastr.min.js",
                    "~/Content/js/moment.js",
                    "~/Content/js/underscore.js"
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
                    "~/Content/js/angular-route.js",
                    //BEGIN ANGULAR TRANSLATE
                    "~/Content/js/translate/angular-cookies.min.js",
                    "~/Content/js/translate/angular-translate.min.js",
                    "~/Content/js/translate/angular-translate-storage-cookie.min.js",
                    "~/Content/js/translate/angular-translate-storage-local.min.js",
                    "~/Content/js/translate/angular-translate-loader-url.min.js",
                    //END ANGULAR TRANSLATE
                     "~/Content/js-transpiled/ntech_shared/common/*.js",
                    "~/Content/js-transpiled/ntech_shared/legacy/ntech.js.shared.js",
                    "~/Content/js/ntech-forms.js",
                    //Components
                    "~/Content/js-transpiled/ntech_shared/components/infrastructure/*.js",
                    "~/Content/js-transpiled/infrastructure/*.js",
                    "~/Content/js-transpiled/componentsbase.js",
                    "~/Content/js-transpiled/ntech_shared/components/components/*.js",
                    "~/Content/js-transpiled/components/*.js",
                    "~/Content/js-transpiled/customerpages-api-client.js"
                };

            bundles.Add(new ScriptBundle("~/Content/js/bundle-base")
                .Include(sharedScripts));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-basewithangular")
                .Include(sharedScripts)
                .Include(angularScripts));

            //Translation only
            bundles.Add(new ScriptBundle("~/Content/js/bundle-angular-translateonly")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/translation-only.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-savings-standardapplication-index")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js-transpiled/controllers/SavingsStandardApplication/savings-standardapplication-index.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-savings-standardapplication-signingdocumentpreview")
                .Include(sharedScripts)
                .Include("~/Content/js/pdfobject.min.js")
                .Include(angularScripts)
                .Include("~/Content/js/controllers/SavingsStandardApplication/savings-standardapplication-signingdocumentpreview.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-login-with-eid-signature")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/EidSignatureLogin/login-with-eid-signature.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-savingsoverview")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/SavingsOverview/savingsoverview-index.js")
                .Include("~/Content/js/controllers/SavingsOverview/savingsoverview-accountdetails.js")
                .Include("~/Content/js/controllers/SavingsOverview/savingsoverview-withdrawals.js")
                .Include("~/Content/js/controllers/SavingsOverview/savingsoverview-message.js")
                .Include("~/Content/js/controllers/SavingsOverview/savingsoverview-withdrawalaccounts.js")
                .Include("~/Content/js/controllers/SavingsOverview/savingsoverview-accountdocuments.js")
                .Include("~/Content/js/controllers/SavingsOverview/savingsoverview-closures.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-contactinfo")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/ContactInfo/contactinfo-index.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-creditoverview")
                .Include(sharedScripts)
                .Include(angularScripts)
                .Include("~/Content/js/controllers/CreditOverview/creditoverview-index.js")
                .Include("~/Content/js/controllers/CreditOverview/creditoverview-creditdetails.js")
                .Include("~/Content/js/controllers/CreditOverview/creditoverview-opennotifications.js")
                .Include("~/Content/js/controllers/CreditOverview/creditoverview-accountdocuments.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-secureMessages")
              .Include(sharedScripts)
              .Include(angularScripts)
              .Include("~/Content/js-transpiled/controllers/SecureMessages/secureMessages-index.js"));
        }

        private void Application_EndRequest(object sender, EventArgs e)
        {
            if (HttpContext.Current.Items[CustomerPagesAuthorizeAttribute.Force401HackItemName] != null)
            {
                HttpContext.Current.Response.Clear();
                HttpContext.Current.Response.TrySkipIisCustomErrors = true;
                HttpContext.Current.Response.StatusCode = 401;
                HttpContext.Current.Response.ContentType = "application/json";
                HttpContext.Current.Response.Write(JsonConvert.SerializeObject(new
                {
                    errorMessage = "Unauthorized",
                    errorCode = "unauthorized"
                }));
            }
            if (HttpContext.Current.Items[CustomerPagesAuthorizeAttribute.Force403HackItemName] != null)
            {
                HttpContext.Current.Response.Clear();
                HttpContext.Current.Response.StatusCode = 403;
                HttpContext.Current.Response.ContentType = "application/json";
                HttpContext.Current.Response.TrySkipIisCustomErrors = true;
                HttpContext.Current.Response.Write(JsonConvert.SerializeObject(new
                {
                    errorMessage = "Forbidden",
                    errorCode = "forbidden"
                }));
            }
        }
    }
}