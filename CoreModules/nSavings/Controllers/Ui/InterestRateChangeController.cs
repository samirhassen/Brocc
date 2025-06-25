using System;
using System.Text;
using System.Web.Mvc;
using Newtonsoft.Json;
using nSavings.Code;
using nSavings.Controllers.Api;
using nSavings.DbModel;
using nSavings.DbModel.BusinessEvents;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Ui;

[NTechAuthorizeSavingsHigh]
public class InterestRateChangeController : NController
{
    [HttpGet]
    [Route("Ui/InterestRateChange")]
    public ActionResult Index(int? testUserId)
    {
        var today = Clock.Today.Date;
        var currentUserId = NEnv.IsProduction ? CurrentUserId : testUserId ?? CurrentUserId;

        var currentChangeState = ApiInterestRateChangeController.GetChangeStateViewModel(
            ApiInterestRateChangeController.ChangeHandler.GetCurrentChangeState(),
            currentUserId,
            GetUserDisplayNameByUserId,
            Clock);

        using var context = new SavingsContext();
        var currentInterestRate =
            ChangeInterestRateBusinessEventManager.GetCurrentInterestRateForNewAccounts(context,
                SavingsAccountTypeCode.StandardAccount, today);
        var upcomingChanges =
            ApiInterestRateChangeController.GetUpcomingChangesViewModel(context,
                GetUserDisplayNameByUserId, Clock);

        ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.GetEncoding("iso-8859-1").GetBytes(
            JsonConvert.SerializeObject(new
            {
                testUserId = NEnv.IsProduction ? null : testUserId,
                today = today.ToString("yyyy-MM-dd"),
                currentChangeState,
                upcomingChanges,
                currentUserId = CurrentUserId,
                currentInterestRate,
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
                    fetchHistoricalChangeItems =
                        Url.Action("FetchHistoricalChangeItems", "ApiInterestRateChange")
                }
            })));

        return View();
    }
}