using Newtonsoft.Json;
using nSavings.DbModel.BusinessEvents;
using NTech.Services.Infrastructure;
using System;
using System.Text;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechAuthorizeSavingsHigh]
    public class InterestRateChangeController : NController
    {
        [HttpGet]
        [Route("Ui/InterestRateChange")]
        public ActionResult Index(int? testUserId)
        {
            using (var context = new SavingsContext())
            {
                var today = Clock.Today.Date;
                var currentUserId = NEnv.IsProduction ? this.CurrentUserId : testUserId ?? this.CurrentUserId;

                var currentChangeState = ApiInterestRateChangeController.GetChangeStateViewModel(
                    ApiInterestRateChangeController.ChangeHandler.GetCurrentChangeState(),
                    currentUserId,
                    this.GetUserDisplayNameByUserId,
                    this.Clock);

                var currentInterestRate = ChangeInterestRateBusinessEventManager.GetCurrentInterestRateForNewAccounts(context, SavingsAccountTypeCode.StandardAccount, today);
                var upcomingChanges = ApiInterestRateChangeController.GetUpcomingChangesViewModel(context, this.GetUserDisplayNameByUserId, this.Clock);

                ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.GetEncoding("iso-8859-1").GetBytes(JsonConvert.SerializeObject(new
                {
                    testUserId = NEnv.IsProduction ? new int?() : testUserId,
                    today = today.ToString("yyyy-MM-dd"),
                    currentChangeState,
                    upcomingChanges,
                    currentUserId = this.CurrentUserId,
                    currentInterestRate = currentInterestRate,
                    earliestAllowedAllAccountsLoweredDate = (NEnv.ClientCfg.Country.BaseCountry == "FI"
                        ? Clock.Today.AddMonths(2)
                        : Clock.Today.AddDays(1)).ToString("yyyy-MM-dd"),
                    earliestAllowedNewAccountsOrRaisedDate = Clock.Today.AddDays(1).ToString("yyyy-MM-dd"),
                    urls = new
                    {
                        initiateChange = Url.Action("InitiateChange", "ApiInterestRateChange"),
                        getCurrentChangeState = Url.Action("GetCurrentChangeState", "ApiInterestRateChange"),
                        cancelChange = Url.Action("CancelChange", "ApiInterestRateChange"),
                        verifyChange = Url.Action("VerifyChange", "ApiInterestRateChange"),
                        rejectChange = Url.Action("RejectChange", "ApiInterestRateChange"),
                        carryOutChange = Url.Action("CarryOutChange", "ApiInterestRateChange"),
                        cancelUpcomingChange = Url.Action("CancelUpcomingChange", "ApiInterestRateChange"),
                        fetchHistoricalChangeItems = Url.Action("FetchHistoricalChangeItems", "ApiInterestRateChange")
                    }
                })));

                return View();
            }
        }
    }
}