using nCredit.Code;
using nCredit.WebserviceMethods.Reports;
using nCredit.WebserviceMethods.Reports.CompanyLoan;
using nCredit.WebserviceMethods.Reports.MortgageLoans;
using nCredit.WebserviceMethods.Reports.UnsecuredLoansLegacy;
using NTech;
using NTech.Core.Credit.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechAuthorizeCreditHigh]
    public class ReportsController : NController
    {
        [Route("Ui/Reports")]
        public ActionResult Index()
        {
            //Several reports depend on this being populated. This will almost always be a noop/super fast op
            //since periodic maintainance will have fixed this already.
            ApiPeriodicMaintenanceController.PopulateCalendarDates(CoreClock.SharedInstance, Service.CalendarDateService);

            NTechNavigationTarget targetToHere = NTechNavigationTarget.CreateCrossModuleNavigationTarget("CreditReports", null);

            DataWarehouseClient.ScheduledExcelExportedReportsResult preGeneratedReports = null;
            if (NEnv.ServiceRegistry.ContainsService("nDataWarehouse"))
            {
                ViewBag.ShowScheduledReports = true;
                preGeneratedReports = new Code.DataWarehouseClient().FetchScheduledExcelExportedReportsForCurrentUser(targetToHere);
            }
            else
                ViewBag.ShowScheduledReports = false;

            ViewBag.IsUnsecuredLoansEnabled = NEnv.IsUnsecuredLoansEnabled;
            ViewBag.IsStandardUnsecuredLoansEnabled = NEnv.IsStandardUnsecuredLoansEnabled;
            ViewBag.IsMortgageLoansEnabled = NEnv.IsMortgageLoansEnabled;
            ViewBag.IsCompanyLoansEnabled = NEnv.IsCompanyLoansEnabled;
            ViewBag.IsPreCreditEnabled = NEnv.ClientCfgCore.IsFeatureEnabled("ntech.feature.precredit");
            ViewBag.IsBalanziaFi = NEnv.ClientCfg.ClientName == "balanzia";
            ViewBag.ShowDwWarning = (NEnv.IsUnsecuredLoansEnabled || NEnv.IsCompanyLoansEnabled) && !NEnv.IsStandardUnsecuredLoansEnabled;
            ViewBag.IsLegacyAmlReportingAidFiReportEnabled = AmlReportingAidLegacyReportMethod.IsReportEnabled;

            var applicatonReportParameters = GetApplicationReportParameters();

            SetInitialData(new
            {
                reportUrls = new
                {
                    providerFeedback = Url.Action("Get", "ApiReportsProviderFeedback"),
                    applicationAnalysis = NEnv.ClientCfgCore.IsFeatureEnabled("ntech.feature.precredit") ? Url.Action("Get", "ApiReportsApplicationAnalysis") : null,
                    paymentsConsumerCredits = Url.Action("Get", "ApiReportsPaymentsConsumerCredits"),
                    reservationBasis = Url.Action("Get", "ApiReportsReservationBasis"),
                    quarterlyRATI = NEnv.IsStandardUnsecuredLoansEnabled ? null : Url.Action("Get", "ApiReportsQuarterlyRATI"),
                    loanPerformance = GetLoanPerformanceUrl(),
                    liquidityExposure = NEnv.IsStandardUnsecuredLoansEnabled ? null : Url.Action("Get", "ApiReportsLiquidityExposure"),
                    cancelledApplications = NEnv.ClientCfgCore.IsFeatureEnabled("ntech.feature.precredit") && !NEnv.IsStandardUnsecuredLoansEnabled ? Url.Action("Get", "ApiReportsCancelledApplications") : null,
                    contactlist = Service.WsUrl.CreatePostUrl("Reports/ContactList"),
                    lcr = NEnv.IsStandardUnsecuredLoansEnabled ? null : Url.Action("Get", "ApiReportsLcr"),
                    unplacedBalance = Service.WsUrl.CreateGetUrl("Reports/GetUnplacedBalance"),
                    bookkeepingLoanLedger = (NEnv.IsUnsecuredLoansEnabled && !NEnv.IsStandardUnsecuredLoansEnabled) ? Service.WsUrl.CreateGetUrl("Reports/GetBookkeepingLoanLedger") : null,
                    applicationRejectionReasons = (NEnv.ClientCfgCore.IsFeatureEnabled("ntech.feature.precredit") && NEnv.IsUnsecuredLoansEnabled && !NEnv.IsStandardUnsecuredLoansEnabled) ? Service.WsUrl.CreateGetUrl("Reports/ApplicationRejectionReasons") : null,
                    applicationWaterfall = (NEnv.ClientCfgCore.IsFeatureEnabled("ntech.feature.precredit") && NEnv.IsUnsecuredLoansEnabled && !NEnv.IsStandardUnsecuredLoansEnabled) ? Service.WsUrl.CreateGetUrl("Reports/GetApplicationWaterfall") : null,
                    companyLoanApplicationList = NEnv.IsCompanyLoansEnabled && NEnv.ClientCfgCore.IsFeatureEnabled("ntech.feature.precredit") ? CreateOtherModuleStreamingReportUrl("nPreCredit", "api/Reports/CompanyLoan/Applications") : null,
                    companyLoanledger = (NEnv.IsCompanyLoansEnabled) ? Service.WsUrl.CreateGetUrl("Reports/GetCompanyLoanLedger") : null,
                    companyLoanCustomLedger = CompanyLoanCustomLedgerReportMethod.IsReportEnabled ? Service.WsUrl.CreateGetUrl("Reports/GetCompanyLoanCustomLedger") : null,
                    companyLoanOverdueNotifications = (NEnv.IsCompanyLoansEnabled) ? Service.WsUrl.CreateGetUrl("Reports/GetCompanyLoanOverdueNotifications") : null,
                    mortgageLoanQuarterlyBKI = NEnv.IsMortgageLoansEnabled && NEnv.IsMortgageLoanBKIClient ? Service.WsUrl.CreateGetUrl("Reports/MortgageLoanQuarterlyBKI") : null,
                    mortgageLoanCollateral = NEnv.IsMortgageLoansEnabled ? Service.WsUrl.CreateGetUrl("Reports/MortgageLoanCollateral") : null,
                    mortgageLoanPerformance = NEnv.IsMortgageLoansEnabled ? Service.WsUrl.CreateGetUrl("Reports/GetMortgageLoanPerformance") : null,
                    mortgageLoanIfrsCollateral = NEnv.IsMortgageLoansEnabled && NEnv.ClientCfg.Country.BaseCountry == "FI" ? Service.WsUrl.CreateGetUrl("Reports/MortgageLoanIfrsCollateral") : null,
                    mortgageLoanApplications = NEnv.ClientCfgCore.IsFeatureEnabled("ntech.feature.precredit") && NEnv.IsMortgageLoansEnabled && NEnv.ClientCfg.Country.BaseCountry == "FI" ? CreateOtherModuleStreamingReportUrl("nPreCredit", "api/MortgageLoan/Reports/Applications") : null,
                    swedishQuarterlyF818 = SwedishQuarterlyF818ReportMethod.IsReportEnabled ? Service.WsUrl.CreateGetUrl("Reports/GetSwedishQuarterlyF818") : null,
                    legacyAmlReportingAidFi = AmlReportingAidLegacyReportMethod.IsReportEnabled ? Service.WsUrl.CreateGetUrl("Reports/GetAmlReportingAidLegacy") : null,
                    amlReportingAidCompanySe = AmlReportingAidReportCompanySeMethod.IsReportEnabled ? Service.WsUrl.CreateGetUrl("Reports/GetAmlReportingAidCompanySe") : null,
                    mortgageFixedInterestRateHistory = MortgageFixedInterestRateHistoryReportMethod.IsReportEnabled ? Service.WsUrl.CreateGetUrl("Reports/MortgageLoanFixedInterestRateHistory") : null,
                    mortgageAverageInterestRates = MortgageAverageInterestRateReportMethod.IsReportEnabled ? Service.WsUrl.CreateGetUrl("Reports/MortgageAverageInterestRates") : null,
                    kycQuestionsStatus = NEnv.ClientCfgCore.IsFeatureEnabled("feature.customerpages.kyc") ? CreateOtherModuleStreamingReportUrl("nCustomer", "Api/Kyc-Reminders/Report") : null,
                    bookkeepingReconciliation =
                        BookkeepingReconciliationReportService.IsReportEnabled(NEnv.EnvSettings) 
                            ?
                            CreateOtherModuleStreamingReportUrl("NTechHost", "Api/Credit/Reports/BookkeepingReconciliation")
                            : null,
                    alternatePaymentPlans = AlternatePaymentPlanReportsService.IsReportEnabled(NEnv.ClientCfgCore)
                        ? CreateOtherModuleStreamingReportUrl("NTechHost", "Api/Credit/Reports/AlternatePaymentPlans")
                        : null
                },
                lastDwUpdateAgeInDays = new SchedulerClient().FetchLastSuccessAgeInDaysByTag("UpdateDataWarehouse"),
                preGeneratedReports = preGeneratedReports?.Reports,
                today = Clock.Today,
                creditQuarters = GetCreditQuarters(),
                abTestExperiments = GetAbTestExperiments(),
                waterfallParameters = (NEnv.ClientCfgCore.IsFeatureEnabled("ntech.feature.precredit") && (NEnv.IsUnsecuredLoansEnabled || NEnv.IsMortgageLoansEnabled || NEnv.IsStandardUnsecuredLoansEnabled)) ? new
                {
                    providerNames = applicatonReportParameters.Value.ProviderNames,
                    scoreGroups = applicatonReportParameters.Value.ScoreGroups,
                    applicationMonths = applicatonReportParameters.Value.ApplicationMonths,
                    applicationYears = GetApplicationYears(applicatonReportParameters.Value.ApplicationMonths),
                    applicationQuarters = GetApplicationQuarters(applicatonReportParameters.Value.ApplicationMonths),
                    providerDisplayNameByProviderName = applicatonReportParameters.Value.ProviderNames?.ToDictionary(x => x, ProviderDisplayNames.GetProviderDisplayName)
                } : null
            });
            return View();
        }

        private string GetLoanPerformanceUrl() => NEnv.IsStandardUnsecuredLoansEnabled
                        ? Service.WsUrl.CreateGetUrl("Reports/GetUnsecuredLoanStandardLoanPerformance")
                        : (NEnv.IsUnsecuredLoansEnabled ? Service.WsUrl.CreateGetUrl("Reports/GetLoanPerformance") : null);

        private (List<DateTime> ApplicationMonths, List<string> ProviderNames, List<string> ScoreGroups)? GetApplicationReportParameters()
        {
            if (!NEnv.ClientCfgCore.IsFeatureEnabled("ntech.feature.precredit"))
            {
                return null;
            }

            var useDwAsSource = NEnv.IsUnsecuredLoansEnabled && !NEnv.IsStandardUnsecuredLoansEnabled;
            if (useDwAsSource)
            {
                var dwClient = new Lazy<DataWarehouseClient>();
                var providerNames = dwClient.Value
                    .FetchReportData<DwProviderNameModel>("providerNames1", new System.Dynamic.ExpandoObject())
                    .Select(x => x.ProviderName).ToList();
                var applicationMonths = dwClient.Value
                    .FetchReportData<DwApplicationMonthModel>("applicationMonths1", new System.Dynamic.ExpandoObject())
                    .Select(x => x.MonthDate).ToList();
                var scoreGroups = dwClient.Value.FetchReportData<DwScoreGroupModel>("scoreGroups1", new System.Dynamic.ExpandoObject())
                    .Select(x => x.ScoreGroup).ToList();
                return (applicationMonths, providerNames, scoreGroups);
            }
            else
            {
                var client = new PreCreditClient();
                var (providerNames, applicationMonths) = client.GetCommonReportParameters();
                return (applicationMonths, providerNames, null);
            }
        }

        private static string CreateOtherModuleStreamingReportUrl(string moduleName, string moduleLocalReportUrl)
        {
            if (!NEnv.ServiceRegistry.ContainsService(moduleName))
                throw new Exception("No such module exists");

            return $"/Ui/Report/{moduleName}{(moduleLocalReportUrl.StartsWith("/") ? "" : "/")}{moduleLocalReportUrl}";
        }

        [HttpGet]
        [Route("Ui/Report/{targetModule}/{*moduleLocalReportUrl}")]
        public ActionResult StreamFromOtherModule()
        {
            var targetModule = RouteData?.Values["targetModule"]?.ToString() ?? "";
            var moduleLocalReportUrl = (RouteData?.Values["moduleLocalReportUrl"]?.ToString() ?? "")
                + (Request.QueryString.AllKeys.Length == 0 ? "" : $"?{Request.QueryString}");

            var sr = NEnv.ServiceRegistry;

            if (!sr.ContainsService(targetModule))
                return HttpNotFound();

            var ms = new MemoryStream();
            NHttp
                .Begin(sr.Internal.ServiceRootUri(targetModule), NHttp.GetCurrentAccessToken())
                .Get(moduleLocalReportUrl)
                .DownloadFile(ms, out var contentType, out var fileName);

            ms.Position = 0;

            return File(ms, contentType, fileName);
        }

        private List<AbTestExperimentModel> GetAbTestExperiments()
        {
            if (!NEnv.ClientCfgCore.IsFeatureEnabled("ntech.feature.precredit"))
                return null;

            var client = new PreCreditClient();
            return client.GetAbTestExperiments();
        }

        private List<Quarter> GetCreditQuarters()
        {
            using (var context = new CreditContext())
            {
                var r = context
                    .Transactions
                    .GroupBy(x => 1).Select(x => new
                    {
                        MinDate = x.Min(y => (DateTime?)y.TransactionDate),
                        MaxDate = x.Max(y => (DateTime?)y.TransactionDate)
                    })
                    .SingleOrDefault();

                if (r == null || !r.MinDate.HasValue)
                    return Enumerables.Singleton(Quarter.ContainingDate(Clock.Today)).ToList();
                else
                {
                    var fd = r.MinDate.Value;
                    var td = Dates.Max(r.MaxDate.Value, Clock.Today);

                    return Quarter.GetAllBetween(fd, td).OrderByDescending(x => x.ToDate).ToList();
                }
            }
        }

        private List<ApplicationYear> GetApplicationYears(List<DateTime> applicationMonths)
        {
            return applicationMonths.GroupBy(x => x.Year).Select(x => new ApplicationYear
            {
                Year = x.Key,
                FirstDate = x.Min(),
                LastDate = x.Max()
            })
            .OrderByDescending(x => x.Year)
            .ToList();
        }

        private List<ApplicationQuarter> GetApplicationQuarters(List<DateTime> applicationMonths)
        {
            return applicationMonths.GroupBy(x => new { x.Year, Quarter = WaterfallReportMethod.MonthToQuarterMapping[x.Month] }).Select(x => new ApplicationQuarter
            {
                Year = x.Key.Year,
                Quarter = x.Key.Quarter,
                FirstDate = x.Min(),
                LastDate = x.Max()
            })
            .OrderByDescending(x => x.Year)
            .ToList();
        }

        private class ApplicationYear
        {
            public int Year { get; set; }
            public DateTime FirstDate { get; set; }
            public DateTime LastDate { get; set; }
        }

        private class ApplicationQuarter
        {
            public int Year { get; set; }
            public int Quarter { get; set; }
            public DateTime FirstDate { get; set; }
            public DateTime LastDate { get; set; }
        }


        private class DwProviderNameModel
        {
            public string ProviderName { get; set; }
        }
        private class DwApplicationMonthModel
        {
            public DateTime MonthDate { get; set; }
        }
        private class DwScoreGroupModel
        {
            public string ScoreGroup { get; set; }
        }
        public class AbTestExperimentModel
        {
            public int ExperimentId { get; set; }
            public string ExperimentName { get; set; }
        }
    }
}