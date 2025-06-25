using System;
using System.Data.Entity;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using nSavings.DbModel;
using nSavings.DbModel.BusinessEvents;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;
using NTech.Services.Infrastructure;
using Serilog;

namespace nSavings.Controllers.Api;

#nullable enable

public class CountResponse(in int count)
{
    public int count { get; } = count;
}

public class MaturityJobResponse(in int successful, in int failed)
{
    public int successful { get; } = successful;
    public int failed { get; } = failed;
}

[NTechApi]
[RoutePrefix("Api/FixedInterestAccountMaturity")]
[NTechAuthorizeSavingsHigh(ValidateAccessToken = true)]
public class ApiFixedInterestAccountMaturityController : NController
{
    [HttpGet, Route("CountApplicable")]
    public async Task<ActionResult> CountApplicable()
    {
        var ct = Request.GetOwinContext().Request.CallCancelled;

        using var context = new SavingsContext();
        var count = await context.SavingsAccountHeaders
            .CountAsync(a =>
                a.AccountTypeCode == nameof(SavingsAccountTypeCode.FixedInterestAccount) &&
                a.Status == nameof(SavingsAccountStatusCode.Active) &&
                a.MaturesAt > Clock.Today, ct);

        return Json2(new CountResponse(count));
    }

    [HttpPost, Route("Run")]
    public async Task<ActionResult> AccountMaturityJob()
    {
        var ct = Request.GetOwinContext().Request.CallCancelled;

        return await SavingsContext.RunWithExclusiveLockAsync("ntech.scheduledjobs.fixedinterestaccountmaturity",
            RunAccountMaturityJob,
            _ => Task.FromResult<ActionResult>(
                new HttpStatusCodeResult(HttpStatusCode.Conflict, "Maturity job is already running.")),
            ct: ct);
    }

    private async Task<ActionResult> RunAccountMaturityJob(CancellationToken ct)
    {
        try
        {
            using var ctx = new SavingsContext();
            var mgr = new AccountMaturityBusinessEventManager(CurrentUserId, InformationMetadata, Clock, ctx);

            var result = await mgr.RunAccountMaturityJobAsync(ct);

            if (result.Item2 > 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError,
                    $"Failed to convert {result.Item2} applicable account(s). {result.Item1} succeeded.");
            }

            return Json2(new MaturityJobResponse(result.Item1, result.Item2));
        }
        catch (Exception ex)
        {
            NLog.Error(ex, "Error running account maturity job");
            return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Error running account maturity job");
        }
    }
}