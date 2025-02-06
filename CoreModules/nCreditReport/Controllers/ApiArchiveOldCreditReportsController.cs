using nCreditReport.Code;
using NTech;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Web.Mvc;

namespace nCreditReport.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    public class ApiArchiveOldCreditReportsController : NController
    {
        [HttpPost()]
        [Route("Api/Jobs/Archive")]
        public ActionResult ArchiveCreditReports(IDictionary<string, string> schedulerData = null)
        {
            Func<string, string> getSchedulerData = s => (schedulerData != null && schedulerData.ContainsKey(s)) ? schedulerData[s] : null;

            return CreditReportContext.RunWithExclusiveLock("ntech.scheduledjobs.archiveoldcreditreports",
                    ArchiveCreditReportsI,
                    () => new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Job is already running"));
        }

        public ActionResult ArchiveCreditReportsI()
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            var inactiveNrOfDaysCutoff = NEnv.CreditReportArchiveJobInactivNrOfDaysCutoff;

            var batchSize = 300;
            var totalArchivedCount = 0;
            int totalAnalysedCreditReports = 0;

            var w = Stopwatch.StartNew();
            try
            {
                var archiveService = new CreditReportArchiveService(ClockFactory.SharedInstance);
                var nrOfArchivableCreditReportsRemaining = archiveService.GetArchivableCreditReportsCount(inactiveNrOfDaysCutoff);
                while (w.Elapsed < NEnv.MaxCreditReportArchiveJobRuntime && nrOfArchivableCreditReportsRemaining > 0)
                {
                    int totalAnalysedCreditReportsInBatch;
                    var creditReportIds = archiveService.GetArchivableCreditReports(batchSize, inactiveNrOfDaysCutoff, out totalAnalysedCreditReportsInBatch);
                    nrOfArchivableCreditReportsRemaining -= totalAnalysedCreditReportsInBatch;
                    totalAnalysedCreditReports += totalAnalysedCreditReportsInBatch;

                    archiveService.ArchiveCreditReports(creditReportIds);
                    totalArchivedCount += creditReportIds.Count;
                }

                w.Stop();
                NLog.Information($"ArchiveCreditReports finished TotalMilliseconds={w.ElapsedMilliseconds}, Count analysed={totalAnalysedCreditReports}, Count archived={totalArchivedCount}");
            }
            catch (Exception ex)
            {
                NLog.Error(ex, $"ArchiveCreditReports crashed");
                errors.Add($"ArchiveCreditReports crashed, see error log for details");
            }
            finally
            {
                w.Stop();
            }
            return Json2(new { errors, totalMilliseconds = w.ElapsedMilliseconds, warnings, totalArchivedCount });

        }
    }

}
