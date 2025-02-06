using nSavings.DbModel.BusinessEvents;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechApi]
    [RoutePrefix("Api/YearlyInterestCapitalization")]
    [NTechAuthorizeSavingsHigh(ValidateAccessToken = true)]
    public class ApiYearlyInterestCapitalizationController : NController
    {
        [Route("Run")]
        [HttpPost()]
        public ActionResult RunYearlyInterestCapitalization(IDictionary<string, string> schedulerData = null)
        {
            Func<string, string> getSchedulerData = s => (schedulerData != null && schedulerData.ContainsKey(s)) ? schedulerData[s] : null;

            var includeCalculationDetails = !(getSchedulerData("skipCalculationDetails") == "true");

            var c = new Code.DocumentClient();
            return SavingsContext.RunWithExclusiveLock("ntech.scheduledjobs.savingsyearlyinterestcapitalization",
                    () => RunYearlyInterestCapitalizationI(includeCalculationDetails),
                    () => new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Job is already running"));
        }

        private ActionResult RunYearlyInterestCapitalizationI(bool includeCalculationDetails)
        {
            int successCount = 0;
            List<string> errors = new List<string>();
            var w = Stopwatch.StartNew();
            try
            {
                var mgr = new YearlyInterestCapitalizationBusinessEventManager(CurrentUserId, InformationMetadata, Clock);
                int iSuccessCount;
                mgr.RunYearlyInterestCapitalization(includeCalculationDetails, out iSuccessCount);
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

            NLog.Information("YearlyInterestCapitalization finished TotalMilliseconds={totalMilliseconds}", w.ElapsedMilliseconds);

            //Used by nScheduler
            var warnings = new List<string>();
            errors?.ForEach(x => warnings.Add(x));
            if (successCount == 0)
                warnings.Add("No accounts capitalized");

            return Json2(new { successCount, failCount = 0, errors, totalMilliseconds = w.ElapsedMilliseconds, warnings = warnings });
        }
    }
}