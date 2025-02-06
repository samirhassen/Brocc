using nSavings.Code;
using nSavings.WebserviceMethods.Reports;
using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechAuthorizeSavingsHigh]
    public class ReportsController : NController
    {
        [Route("Ui/Reports")]
        public ActionResult Index()
        {
            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                reportUrls = new
                {
                    savingsLedger = Service.WsUrl.CreateGetUrl("Reports/GetSavingsLedger"),
                    currentInterestRates = Url.Action("GetCurrentInterestRates", "ApiInterestRateReports"),
                    interestRatesPerAccount = Url.Action("GetInterestRatesPerAccount", "ApiInterestRateReports"),
                    dailyOutgoingPayments = Url.Action("Get", "ApiReportsDailyOutgoingPayments"),
                    providerFeedback = Url.Action("GetProviderFeedback", "ApiReportsProviderFeedback"),
                    unplacedBalance = Service.WsUrl.CreateGetUrl("Reports/GetUnplacedBalance"),
                    amlReportingAidFi = AmlReportingAidReportMethod.IsReportEnabled ? Service.WsUrl.CreateGetUrl("Reports/GetAmlReportingAidLegacy") : null
                },
                lastDwUpdateAgeInDays = new SchedulerClient().FetchLastSuccessAgeInDaysByTag("UpdateDataWarehouse"),
                today = Clock.Today
            });
            return View();
        }
    }
}