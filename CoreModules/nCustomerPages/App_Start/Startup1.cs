using System;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using nCustomerPages.App_Start;
using nCustomerPages.Controllers;
using NTech;
using NTech.Services.Infrastructure;
using Owin;
using Serilog;
using Serilog.Core.Enrichers;
using Serilog.Events;

[assembly: OwinStartup(typeof(Startup1))]

namespace nCustomerPages.App_Start
{
    public class Startup1
    {
        public void Configuration(IAppBuilder app)
        {
            var automationUser = new Lazy<NTechSelfRefreshingBearerToken>(() =>
                NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry,
                    NEnv.SystemUserUserNameAndPassword));
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithMachineName()
                .Enrich.FromLogContext()
                .Enrich.With(
                    new PropertyEnricher("ServiceName", "nCustomerPages"),
                    new PropertyEnricher("ServiceVersion",
                        Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString())
                )
                .WriteTo.Sink(new NTechSerilogSink(n => NEnv.ServiceRegistry.Internal[n], bearerToken: automationUser),
                    NEnv.IsVerboseLoggingEnabled
                        ? LogEventLevel.Debug
                        : LogEventLevel.Information)
                .CreateLogger();

            NLog.Information("{EventType}: {environment} mode", "ServiceStarting", NEnv.IsProduction ? "prod" : "dev");

            app.Use<NTechLoggingMiddleware>("nCustomerPages");

            if (NEnv.IsCreditTokenAuthenticationModeEnabled)
            {
                app.UseCookieAuthentication(new CookieAuthenticationOptions
                {
                    AuthenticationType = CreditTokenAuthenticationController.AuthType,
                    CookieName =
                        $".AuthCookie.{CreditTokenAuthenticationController.AuthType}.{(NEnv.IsProduction ? "P" : "T")}",
                    ExpireTimeSpan = TimeSpan.FromHours(1),
                    SlidingExpiration = true,
                    LoginPath = new PathString("/access-denied")
                });
            }

            if (NEnv.IsDirectEidAuthenticationModeEnabled &&
                CommonElectronicIdLoginProvider.IsProviderEnabled)
            {
                app.UseCookieAuthentication(new CookieAuthenticationOptions
                {
                    AuthenticationType = CommonElectronicIdLoginProvider.AuthTypeNameShared,
                    CookieName =
                        $".AuthCookie.{CommonElectronicIdLoginProvider.AuthTypeNameShared}.{(NEnv.IsProduction ? "P" : "T")}",
                    ExpireTimeSpan = TimeSpan.FromHours(1),
                    SlidingExpiration = true,
                    LoginPath = new PathString("/access-denied")
                });
            }

            ClockFactory.Init();

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

            string GetCdnUrl(string n) => cdnRootUrl == null
                ? null
                : new Uri(new Uri(cdnRootUrl), $"magellan-customerpages/css/{n}").ToString();

            var sharedStyles = new[]
            {
            "~/Content/css/bootstrap.min.css",
            "~/Content/css/toastr.css",
        };

            bundles.Add(new StyleBundle("~/Content/css/bundle-base")
                .Include(sharedStyles));

            bundles.Add(new StyleBundle("~/Content/css/bundle-magellan-customerpages",
                    GetCdnUrl("magellan-customerpages.css"))
                .Include("~/Content/css/magellan-customerpages.css"));

            bundles.Add(new StyleBundle("~/Content/css/bundle-ml-magellan-customerpages",
                    GetCdnUrl("ml-magellan-customerpages.css"))
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

            var sharedScripts = new[]
            {
            "~/Content/js/jquery-1.12.4.js",
            "~/Content/js/bootstrap.js",
            "~/Content/js/toastr.min.js",
            "~/Content/js/moment.js",
            "~/Content/js/underscore.js"
        };

            var angularLocale = NEnv.ClientCfg.Country.BaseCountry switch
            {
                "SE" => "sv-se",
                "FI" => "fi-fi",
                _ => throw new NotImplementedException()
            };

            var angularScripts = new[]
            {
            "~/Content/js/angular.min.js",
            $"~/Content/js/angular-locale_{angularLocale}.js",
            "~/Content/js/angular-resource.min.js",
            "~/Content/js/angular-route.js",
            //BEGIN ANGULAR TRANSLATE
            "~/Content/js/translate/angular-cookies.min.js",
            "~/Content/js/translate/angular-translate.min.js",
            "~/Content/js/translate/angular-translate-storage-cookie.min.js",
            "~/Content/js/translate/angular-translate-storage-local.min.js",
            "~/Content/js/translate/angular-translate-loader-url.min.js",
            //END ANGULAR TRANSLATE
            "~/Content/js/ntech_shared/common/*.js",
            "~/Content/js/ntech_shared/legacy/ntech.js.shared.js",
            "~/Content/js/ntech-forms.js",
            //Components
            "~/Content/js/ntech_shared/components/infrastructure/*.js",
            "~/Content/js/infrastructure/*.js",
            "~/Content/js/componentsbase.js",
            "~/Content/js/ntech_shared/components/components/*.js",
            "~/Content/js/components/*.js",
            "~/Content/js/customerpages-api-client.js"
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
                .Include(
                    "~/Content/js/controllers/SavingsStandardApplication/savings-standardapplication-index.js"));

            bundles.Add(new ScriptBundle("~/Content/js/bundle-savings-standardapplication-signingdocumentpreview")
                .Include(sharedScripts)
                .Include("~/Content/js/pdfobject.min.js")
                .Include(angularScripts)
                .Include(
                    "~/Content/js/controllers/SavingsStandardApplication/savings-standardapplication-signingdocumentpreview.js"));

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
                .Include("~/Content/js/controllers/SecureMessages/secureMessages-index.js"));
        }

    }
}