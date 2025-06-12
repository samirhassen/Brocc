using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using nSavings.Code;
using nSavings.Code.Services;
using nSavings.DbModel;
using NTech;
using NTech.Services.Infrastructure;
using Serilog;

namespace nSavings.Controllers.Api
{
    [NTechApi]
    [RoutePrefix("Api/PeriodicMaintenance")]
    [NTechAuthorizeSavingsHigh(ValidateAccessToken = true)]
    public class ApiPeriodicMaintenanceController : NController
    {
        [Route("Run")]
        [HttpPost()]
        public ActionResult RunPeriodicMaintenance(IDictionary<string, string> schedulerData = null)
        {
            return SavingsContext.RunWithExclusiveLock("ntech.scheduledjobs.savingsperiodicmaintenance",
                SavingsPeriodicmaintenanceI,
                () => new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Job is already running"));
        }

        private ActionResult SavingsPeriodicmaintenanceI()
        {
            var errors = new List<string>();
            var w = Stopwatch.StartNew();
            try
            {
                //NOTE: Anything that is added to run here must support running with any frequency without causing problems

                //Remove expired temporary encryption items
                ApiEncryptedTemporaryStorageController.DeleteExpiredItems();
                PopulateCalendarDates(Clock);
                ControllerServiceFactory.CustomerRelationsMerge.MergeSavingsAccountsToCustomerRelations();
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Savings PeriodicMaintenance crashed");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
            finally
            {
                w.Stop();
            }

            NLog.Information("Savings PeriodicMaintenance finished TotalMilliseconds={totalMilliseconds}",
                w.ElapsedMilliseconds);

            //Used by nScheduler
            var warnings = new List<string>();
            errors.ForEach(x => warnings.Add(x));

            return Json2(new { errors, totalMilliseconds = w.ElapsedMilliseconds, warnings = warnings });
        }

        private void PopulateCalendarDates(IClock clock)
        {
            if (!NEnv.IsProduction)
            {
                //If you change this, make sure that the calendar table is always continous even when jumping back and forth through time
                if (clock.Today > DateTime.Today.AddYears(20))
                    throw new Exception(
                        "The calendar table cannot support timemachine dates more than 20 years into the future at this point");
            }

            //Make sure all dates between 1950-01-01 and at least 20 years from now exist
            using (var context = new SavingsContext())
            {
                void AddTenYearsToCalendar(DateTime startFromDate)
                {
                    context.Database.ExecuteSqlCommand(@"WITH Dates 
AS 
(
	SELECT	TheDate = @startDate
	UNION ALL 
	SELECT	TheDate = DATEADD(DAY, 1, TheDate)
	FROM	Dates
	WHERE	TheDate < DATEADD(YEAR, 10, @startDate)
)
insert into CalendarDate
(TheDate)
select d.TheDate from Dates d 
OPTION (MAXRECURSION 6000)", new SqlParameter("@startDate", startFromDate));
                }

                var loopGuard = 0;
                while (loopGuard++ < 500)
                {
                    var maxDate = context.CalendarDates.Select(x => (DateTime?)x.TheDate).Max();
                    if (!maxDate.HasValue || maxDate < DateTime.Today.AddYears(20))
                    {
                        AddTenYearsToCalendar(maxDate?.AddDays(1) ?? new DateTime(1950, 1, 1));
                    }
                    else
                        break;
                }

                if (loopGuard > 400)
                    throw new Exception("Hit guard code. Infinite loop detected");
            }
        }
    }
}