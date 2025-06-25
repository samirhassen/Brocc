using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Web.Mvc;
using nSavings.Code;
using nSavings.DbModel;
using nSavings.DbModel.BusinessEvents;
using NTech.Services.Infrastructure;
using Serilog;

namespace nSavings.Controllers.Api;

[NTechApi]
[RoutePrefix("Api/YearlyInterestCapitalization")]
[NTechAuthorizeSavingsHigh(ValidateAccessToken = true)]
public class ApiYearlyInterestCapitalizationController : NController
{
    [HttpPost, Route("Run")]
    public ActionResult RunYearlyInterestCapitalization(IDictionary<string, string> schedulerData = null)
    {
        var includeCalculationDetails = GetSchedulerData("skipCalculationDetails") != "true";

        var c = new DocumentClient();
        return SavingsContext.RunWithExclusiveLock(
            "ntech.scheduledjobs.savingsyearlyinterestcapitalization",
            () => RunYearlyInterestCapitalizationI(includeCalculationDetails),
            () => new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Job is already running"));

        string GetSchedulerData(string s) =>
            schedulerData != null && schedulerData.TryGetValue(s, out var value) ? value : null;
    }

    private ActionResult RunYearlyInterestCapitalizationI(bool includeCalculationDetails)
    {
        int successCount;
        var errors = new List<string>();
        var w = Stopwatch.StartNew();
        try
        {
            var mgr = new YearlyInterestCapitalizationBusinessEventManager(CurrentUserId, InformationMetadata,
                Clock);
            mgr.RunYearlyInterestCapitalization(includeCalculationDetails, out var iSuccessCount);
            successCount = iSuccessCount;
        }
        catch (Exception ex)
        {
            NLog.Error(ex, "YearlyInterestCapitalization crashed");
            return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
        }
        finally
        {
            w.Stop();
        }

        NLog.Information("YearlyInterestCapitalization finished TotalMilliseconds={totalMilliseconds}",
            w.ElapsedMilliseconds);

        //Used by nScheduler
        var warnings = new List<string>();
        errors.ForEach(x => warnings.Add(x));
        if (successCount == 0)
            warnings.Add("No accounts capitalized");

        return Json2(new
        {
            successCount, failCount = 0, errors, totalMilliseconds = w.ElapsedMilliseconds, warnings = warnings
        });
    }
}