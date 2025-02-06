using nDataWarehouse.Code;
using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;

namespace nDataWarehouse.Controllers
{
    public class ExcelScheduledReportsController : NController
    {
        private bool? IsCurrentUserPermittedToViewReport(string reportName)
        {
            var reports = ScheduledExcelReportExporter.ParseReportsFromDwModel(NEnv.DatawarehouseModel);
            var report = reports.FirstOrDefault(x => x.ReportName == reportName);
            if (report == null)
            {
                return null;
            }
            return report.Roles.Any(x => this.User.IsInRole(x));
        }

        [HttpGet()]
        [Route("Ui/ScheduledExcelExports/List/{reportName}")]
        public ActionResult ViewReports(string reportName, string backTarget, DateTime? currentDate)
        {
            if (string.IsNullOrWhiteSpace(reportName))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing reportName");

            var isPermitted = IsCurrentUserPermittedToViewReport(reportName);
            if (!isPermitted.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such report");

            if (!isPermitted.Value)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "The current user does not have permission to view this report.");

            ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.GetEncoding("iso-8859-1").GetBytes(JsonConvert.SerializeObject(new
            {
                currentDate = currentDate ?? DateTime.Now,
                reportName = reportName,
                fetchBatchUrl = Url.Action("FetchReportBatch", "ExcelScheduledReports"),
                createReportUrl = Url.Action("CreateSpecificScheduledExcelExport", "ExcelScheduledReports"),
                whitelistedBackUrl = (string.IsNullOrWhiteSpace(backTarget)
                    ? NEnv.ServiceRegistry.External.ServiceRootUri("nBackoffice")
                    : NEnv.ServiceRegistry.External.ServiceUrl("nBackoffice", "Ui/CrossModuleNavigate", Tuple.Create("targetCode", backTarget))).ToString()
            })));
            return View();
        }

        [NTechApi]
        [HttpPost]
        [Route("Api/ScheduledExcelExports/FetchExportedReportBatch")]
        public ActionResult FetchReportBatch(string reportName, int? startBeforeId, int? batchSize)
        {
            if (string.IsNullOrWhiteSpace(reportName))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing reportName");

            var isPermitted = IsCurrentUserPermittedToViewReport(reportName);
            if (!isPermitted.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such report");

            if (!isPermitted.Value)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "The current user does not have permission to view this report.");

            using (var context = new AnalyticsContext())
            {
                var b = context
                    .ExportedReports
                    .Where(x => x.ReportName == reportName);

                if (startBeforeId.HasValue)
                    b = b.Where(x => x.Id < startBeforeId.Value);

                var result = b
                    .OrderByDescending(x => x.Id)
                    .Take(batchSize ?? 100)
                    .Select(x => new
                    {
                        x.Id,
                        x.ReportDate,
                        x.ReportName,
                        x.ReportArchiveKey,
                        x.GenerationTimeInMs
                    })
                    .ToList();

                return Json2(new
                {
                    ReportsBatch = result.Select(x => new
                    {
                        x.Id,
                        x.ReportDate,
                        x.ReportName,
                        x.ReportArchiveKey,
                        ReportArchiveUrl = x.ReportArchiveKey == null ? null : Url.Action("ViewReport", "ExcelScheduledReports", new { reportId = x.Id }),
                        x.GenerationTimeInMs
                    }).ToList(),
                    OldestIdInBatch = result.FirstOrDefault()?.Id
                });
            }
        }

        [NTechApi]
        [HttpGet]
        [Route("Api/ScheduledExcelExports/View/{reportId}")]
        public ActionResult ViewReport(int reportId)
        {
            using (var context = new AnalyticsContext())
            {
                var report = context
                    .ExportedReports
                    .Where(x => x.Id == reportId)
                    .Select(x => new { x.ReportName, x.ReportArchiveKey })
                    .SingleOrDefault();

                if (report == null)
                    return HttpNotFound();

                var isPermitted = IsCurrentUserPermittedToViewReport(report.ReportName);
                if (!isPermitted.HasValue)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such report");

                if (!isPermitted.Value)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "The current user does not have permission to view this report.");

                if (report.ReportArchiveKey == null)
                    return HttpNotFound();
                else
                    return Redirect(DocumentClient.GetArchiveFetchLink(report.ReportArchiveKey).ToString());
            }
        }

        [NTechApi]
        [HttpPost]
        [Route("Api/ScheduledExcelExports/FetchAvailableReportsForCurrentUser")]
        public ActionResult FetchAvailableReportsForCurrentUser(string backTarget)
        {
            var reports = ScheduledExcelReportExporter
                .ParseReportsFromDwModel(NEnv.DatawarehouseModel)
                .Where(x => x.Roles.Any(y => this.User.IsInRole(y)))
                .ToList();

            var names = reports.Select(x => x.ReportName).ToList();

            using (var context = new AnalyticsContext())
            {
                var result = context
                    .ExportedReports
                    .Where(x => names.Contains(x.ReportName))
                    .GroupBy(x => x.ReportName)
                    .Select(x => new
                    {
                        ReportName = x.Key,
                        LatestReport = x.OrderByDescending(y => y.Id).Select(y => new { y.Id, y.ReportDate }).FirstOrDefault()
                    })
                    .ToList()
                    .Select(x => new
                    {
                        x.ReportName,
                        LatestReportViewUrl = x.LatestReport != null
                            ? Url.Action("ViewReport", new { reportId = x.LatestReport.Id })
                            : null,
                        LatestReportDate = x.LatestReport?.ReportDate
                    })
                    .ToDictionary(x => x.ReportName);

                Func<string, string> toOutsideUrl = localUrl =>
                    localUrl == null
                    ? null
                    : new Uri(new Uri(NEnv.ServiceRegistry.External["nDataWarehouse"]), localUrl).ToString();

                return Json2(new
                {
                    Reports = reports.Select(x =>
                    {
                        var h = result.ContainsKey(x.ReportName) ? result[x.ReportName] : null;
                        return new
                        {
                            x.ReportName,
                            x.FriendlyName,
                            LatestReportViewUrl = toOutsideUrl(h?.LatestReportViewUrl),
                            LatestReportDate = h?.LatestReportDate,
                            AllReportsViewUrl = toOutsideUrl(Url.Action("ViewReports", new { reportName = x.ReportName, backTarget = backTarget }))
                        };
                    }).ToList()
                });
            }
        }

        [NTechApi]
        [HttpPost]
        [Route("Api/ScheduledExcelExports/CreateSpecific")]
        public ActionResult CreateSpecificScheduledExcelExport(DateTime? reportDate, string reportName)
        {
            if (string.IsNullOrWhiteSpace(reportName))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing reportName");
            }
            return CreateDailyScheduledExcelExports(reportDate, new Dictionary<string, string>
            {
                { "onlyThisReportName", reportName }
            }, ignoreDayOfWeek: true);
        }

        [NTechApi]
        [HttpPost]
        [Route("Api/ScheduledExcelExports/CreateDaily")]
        public ActionResult CreateDailyScheduledExcelExports(DateTime? reportDate, IDictionary<string, string> schedulerData, bool? ignoreDayOfWeek = null)
        {
            try
            {
                var dayOfTheWeek = DateTime.Now.DayOfWeek;

                var e = new ScheduledExcelReportExporter();
                var reports = ScheduledExcelReportExporter
                    .ParseReportsFromDwModel(NEnv.DatawarehouseModel)
                    .Where(x => x.IsDaily && (ignoreDayOfWeek.GetValueOrDefault() || !x.OnlyRunOnThisDay.HasValue || x.OnlyRunOnThisDay.Value == dayOfTheWeek))
                    .ToList();

                if (schedulerData != null && schedulerData.ContainsKey("onlyThisReportName"))
                {
                    reports = reports.Where(x => x.ReportName == schedulerData["onlyThisReportName"]).ToList();
                }

                string failedMessage;
                if (!e.TryExportReports(reports, reportDate ?? DateTime.Now, out failedMessage))
                {
                    return Json2(new { errors = new[] { failedMessage } });
                }
                else
                    return Json2(new { });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error in CreateDailyScheduledExcelExports");
                return Json2(new { errors = new[] { "Internal server error. See error log" } });
            }
        }
    }
}