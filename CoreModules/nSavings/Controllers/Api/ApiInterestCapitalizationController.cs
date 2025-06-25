using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Web.Mvc;
using nSavings.DbModel;
using nSavings.DbModel.BusinessEvents;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;
using NTech.Services.Infrastructure;
using Serilog;

namespace nSavings.Controllers.Api;

[NTechApi]
[RoutePrefix("Api/InterestCapitalization")]
[NTechAuthorizeSavingsHigh(ValidateAccessToken = true)]
public class ApiInterestCapitalizationController : NController
{
    [HttpPost, Route("Run")]
    public ActionResult RunInterestCapitalization(string accountType,
        IDictionary<string, string> schedulerData = null)
    {
        SavingsAccountTypeCode? accountTypeCode = null;
        if (!string.IsNullOrWhiteSpace(accountType))
        {
            if (!Enum.TryParse<SavingsAccountTypeCode>(accountType, out var at))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Invalid account type: {accountType}");
            }

            accountTypeCode = at;
        }

        var includeCalculationDetails = GetSchedulerData("skipCalculationDetails") != "true";

        return SavingsContext.RunWithExclusiveLock(
            "ntech.scheduledjobs.savingsinterestcapitalization",
            () => RunInterestCapitalization(includeCalculationDetails, accountTypeCode),
            () => new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Job is already running"));

        string GetSchedulerData(string s) =>
            schedulerData != null && schedulerData.TryGetValue(s, out var value) ? value : null;
    }

    private class SchedulerResponseResult
    {
        public List<string> Warnings { get; set; }
        public List<string> Errors { get; set; }
        public int SuccessCount { get; set; }
        public int FailCount { get; set; }
        public long TotalMilliseconds { get; set; }
    }

    private ActionResult RunInterestCapitalization(bool includeCalculationDetails,
        SavingsAccountTypeCode? accountTypeCode)
    {
        try
        {
            var mgr = new InterestCapitalizationBusinessEventManager(CurrentUserId, InformationMetadata, Clock);
            var w = Stopwatch.StartNew();
            var changed = mgr.RunInterestCapitalizationAllAccounts(includeCalculationDetails, accountTypeCode);
            w.Stop();

            NLog.Information("InterestCapitalization finished TotalMilliseconds={totalMilliseconds}",
                w.ElapsedMilliseconds);

            return Json2(new SchedulerResponseResult
            {
                Errors = [],
                Warnings = changed == 0 ? ["No accounts capitalized"] : [],
                SuccessCount = changed,
                FailCount = 0,
                TotalMilliseconds = w.ElapsedMilliseconds,
            });
        }
        catch (Exception ex)
        {
            NLog.Error(ex, "Error running interest capitalization");
            return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
        }
    }
}