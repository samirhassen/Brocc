﻿using System;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using nSavings.Code;
using nSavings.DbModel;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using NWebsec.Csp;

namespace nSavings;

public class Global : HttpApplication
{
    public override void Init()
    {
        base.Init();
        NTechHttpHardening.HandleCachingAndInformationLeakHeader(this, false);
    }

    public static void Application_Start(object sender, EventArgs e)
    {
        // Code that runs on application startup
        AreaRegistration.RegisterAllAreas();
        GlobalConfiguration.Configure(WebApiConfig.Register);
        RouteConfig.RegisterRoutes(RouteTable.Routes);
        RegisterBundles();
        ValueProviderFactories.Factories.Remove(ValueProviderFactories.Factories.OfType<JsonValueProviderFactory>()
            .FirstOrDefault());
        ValueProviderFactories.Factories.Add(new JsonNetValueProviderFactory());
        NTechHardenedMvcModelBinder.Register(NEnv.CurrentServiceName);
        GlobalFilters.Filters.Add(new NTechHandleErrorAttribute());
        GlobalFilters.Filters.Add(new NTechAuthorizeAttribute { ValidateAccessToken = true });
        GlobalFilters.Filters.Add(new ConvertJsonToCamelCaseActionFilterAttribute());

        GlobalContentSecurityPolicyFilters.RegisterGlobalFilters(GlobalFilters.Filters);

        SavingsContext.InitDatabase();
    }

    private static void RegisterStyles(BundleCollection bundles)
    {
        var cdnRootUrl = NEnv.NTechCdnUrl;
        bundles.UseCdn = cdnRootUrl != null;

        var sharedStyles = new[]
        {
            "~/Content/css/bootstrap.min.css",
            "~/Content/css/toastr.css",
            "~/Content/css/other.css",
        };

        bundles.Add(new StyleBundle("~/Content/css/bundle-base")
            .Include(sharedStyles));

        bundles.Add(new StyleBundle("~/Content/css/bundle-magellan", GetCdnUrl("magellan.css"))
            .Include("~/Content/css/magellan.css"));
        return;

        string GetCdnUrl(string n) =>
            cdnRootUrl == null ? null : new Uri(new Uri(cdnRootUrl), $"magellan/css/{n}").ToString();
    }

    //http://www.asp.net/mvc/overview/performance/bundling-and-minification
    private static void RegisterBundles()
    {
        BundleTable.EnableOptimizations = NEnv.IsBundlingEnabled;
        var bundles = BundleTable.Bundles;

        RegisterStyles(bundles);

        var sharedScripts = new[]
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

        var angularScripts = new[]
        {
            "~/Content/jsexternal/angular.min.js",
            $"~/Content/jsexternal/angular-locale_{GetAngularLocale()}.js",
            "~/Content/jsexternal/angular-resource.min.js",
            "~/Content/jsexternal/angular-route.js",
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
            "~/Content/js/savings-api-client.js"
        };

        bundles.Add(new ScriptBundle("~/Content/js/bundle-base")
            .Include(sharedScripts));

        bundles.Add(new ScriptBundle("~/Content/js/bundle-basewithangular")
            .Include(sharedScripts)
            .Include(angularScripts));

        bundles.Add(new ScriptBundle("~/Content/js/bundle-accountcreationremarks")
            .Include(sharedScripts)
            .Include(angularScripts)
            .Include("~/Content/js/controllers/AccountCreationRemarks/accountcreationremarks-index.js")
            .Include("~/Content/js/controllers/Shared/savingsAccountComments.js"));

        bundles.Add(new ScriptBundle("~/Content/js/bundle-interestratechange")
            .Include(sharedScripts)
            .Include(angularScripts)
            .Include("~/Content/js/controllers/InterestRateChange/interestratechange-index.js"));

        bundles.Add(new ScriptBundle("~/Content/js/bundle-incomingpayments-importfile")
            .Include(sharedScripts)
            .Include(angularScripts)
            .Include("~/Content/js/controllers/IncomingPayments/importfile.js"));

        bundles.Add(new ScriptBundle("~/Content/js/bundle-savingsaccount")
            .Include(sharedScripts)
            .Include(angularScripts)
            .Include("~/Content/js/components-old/customerinfo.js")
            .Include("~/Content/js/controllers/SavingsAccount/savingsaccount-index.js")
            .Include("~/Content/js/controllers/SavingsAccount/savingsaccount-details.js")
            .Include("~/Content/js/controllers/SavingsAccount/savingsaccount-customer.js")
            .Include("~/Content/js/controllers/SavingsAccount/savingsaccount-search.js")
            .Include("~/Content/js/controllers/SavingsAccount/savingsaccount-withdrawals.js")
            .Include("~/Content/js/controllers/SavingsAccount/savingsaccount-accountclosure.js")
            .Include("~/Content/js/controllers/SavingsAccount/savingsaccount-withdrawalaccount.js")
            .Include("~/Content/js/controllers/SavingsAccount/savingsaccount-withdrawalaccountchange.js")
            .Include("~/Content/js/controllers/Shared/savingsAccountComments.js")
        );

        bundles.Add(new ScriptBundle("~/Content/js/bundle-unplacedpayment")
            .Include(sharedScripts)
            .Include(angularScripts)
            .Include("~/Content/js/controllers/UnplacedPayments/unplacedpayment-main.js")
            .Include("~/Content/js/controllers/UnplacedPayments/unplacedpayment-place.js")
            .Include("~/Content/js/controllers/UnplacedPayments/unplacedpayment-repay.js"));

        bundles.Add(new ScriptBundle("~/Content/js/bundle-outgoingpayments-index")
            .Include(sharedScripts)
            .Include(angularScripts)
            .Include("~/Content/js/controllers/OutgoingPayments/index.js"));

        bundles.Add(new ScriptBundle("~/Content/js/bundle-bookkeepingfiles-index")
            .Include(sharedScripts)
            .Include(angularScripts)
            .Include("~/Content/js/controllers/BookkeepingFiles/index.js"));

        bundles.Add(new ScriptBundle("~/Content/js/bundle-dailykycscreen-index")
            .Include(sharedScripts)
            .Include(angularScripts)
            .Include("~/Content/js/controllers/DailyKycScreen/index.js")
        );

        bundles.Add(new ScriptBundle("~/Content/js/bundle-reports")
            .Include(sharedScripts)
            .Include(angularScripts)
            .Include("~/Content/js/controllers/Reports/index.js"));

        bundles.Add(new ScriptBundle("~/Content/js/bundle-trapetsamlexport-index")
            .Include(sharedScripts)
            .Include(angularScripts)
            .Include("~/Content/js/controllers/TrapetsAmlExport/trapetsamlexport.js"));

        bundles.Add(new ScriptBundle("~/Content/js/bundle-riksgalden-index")
            .Include(sharedScripts)
            .Include(angularScripts)
            .Include("~/Content/js/controllers/Riksgalden/index.js"));

        bundles.Add(new ScriptBundle("~/Content/js/bundle-changeexternalaccountmanagement")
            .Include(sharedScripts)
            .Include(angularScripts)
            .Include("~/Content/js/controllers/SavingsAccount/changeexternalaacountmanagement.js"));

        bundles.Add(new ScriptBundle("~/Content/js/bundle-manualpayment-index")
            .Include(sharedScripts)
            .Include(angularScripts)
            .Include("~/Content/js/controllers/ManualPayment/index.js"));

        bundles.Add(new ScriptBundle("~/Content/js/bundle-api-host")
            .Include(sharedScripts)
            .Include(angularScripts)
            .Include("~/Content/js/controllers/ApiHost/apiHost.js"));

        bundles.Add(new ScriptBundle("~/Content/js/bundle-fatcaexport-index")
            .Include(sharedScripts)
            .Include(angularScripts)
            .Include("~/Content/js/controllers/FatcaExport/index.js"));

        bundles.Add(new ScriptBundle("~/Content/js/bundle-scheduledtasks-Cm1amlexport")
            .Include(sharedScripts)
            .Include(angularScripts)
            .Include("~/Content/js/controllers/ScheduledTasks/Cm1amlexport.js"));

        bundles.Add(new ScriptBundle("~/Content/js/bundle-scheduledtasks-Treasuryamlexport")
            .Include(sharedScripts)
            .Include(angularScripts)
            .Include("~/Content/js/controllers/ScheduledTasks/Treasuryamlexport.js"));

        bundles.Add(new ScriptBundle("~/Content/js/bundle-customsaccountsexport-index")
            .Include(sharedScripts)
            .Include(angularScripts)
            .Include("~/Content/js/controllers/ScheduledTasks/customsAccountsExport.js"));
        return;

        string GetAngularLocale()
        {
            var c = NEnv.ClientCfg.Country.BaseCountry;
            switch (c)
            {
                case "SE":
                    return "sv-se";
                case "FI":
                    return "fi-fi";
                default:
                    throw new NotImplementedException();
            }
        }
    }

    /// <summary>
    /// Endpoint generated using NWebSec to catch reports of CSP-violations and log to file.
    /// Will be triggered by (autogenerated): report-uri /WebResource.axd?cspReport=true
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void NWebsecHttpHeaderSecurityModule_CspViolationReported(object sender,
        CspViolationReportEventArgs e)
    {
        var violationReport = e.ViolationReport;
        var logFolder = NEnv.LogFolder;
        GlobalContentSecurityPolicyFilters.LogToFile(violationReport, logFolder);
    }
}