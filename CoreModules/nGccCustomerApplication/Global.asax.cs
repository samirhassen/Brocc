using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.Http;
using System.Web.Optimization;
using nGccCustomerApplication.Code;
using NTech.Services.Infrastructure;

namespace nGccCustomerApplication
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