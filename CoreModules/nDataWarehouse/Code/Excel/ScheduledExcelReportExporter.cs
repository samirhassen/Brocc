using nDataWarehouse.Code.Clients;
using nDataWarehouse.Code.Excel;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace nDataWarehouse.Code
{
    public class ScheduledExcelReportExporter
    {
        public bool TryExportReports(List<ScheduledReport> reports, DateTime reportDate, out string failedMessage)
        {
            var e = new ExcelQueryExporter();

            Stopwatch w;
            foreach (var report in reports)
            {
                w = Stopwatch.StartNew();
                HashSet<string> tempFiles = new HashSet<string>();
                try
                {
                    var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DataWarehouse"].ConnectionString;
                    string tmpfile;
                    using (var conn = new SqlConnection(connectionString))
                    {
                        conn.Open();

                        ExcelQueryExporter.CivicRegNrMapping mapping = SetupCivicRegNrMapping(report);

                        if (report.SplitReportRowCount.HasValue)
                        {
                            tmpfile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip"); ;
                            using (var stream = new FileStream(tmpfile, FileMode.CreateNew, FileAccess.ReadWrite))
                            {
                                e.CreateSplitXlsxZipFromQueryToStream(conn, report.QueryScript, c => { }, stream, mapping, x => $"{report.FriendlyName}-{x}.xlsx", report.SplitReportRowCount.Value);
                            }
                        }
                        else
                        {
                            tmpfile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".xlsx"); ;
                            using (var stream = new FileStream(tmpfile, FileMode.CreateNew, FileAccess.ReadWrite))
                            {
                                e.CreateXlsxFromQueryToStream(conn, report.QueryScript, c => { }, stream, mapping);
                            }
                        }
                        tempFiles.Add(tmpfile);
                    }

                    var dc = new DocumentClient();
                    var archiveKey = dc.ArchiveStoreFile(
                        new FileInfo(tmpfile),
                        report.SplitReportRowCount.HasValue ? "application/zip" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"{report.ReportName}_{reportDate.ToString("yyyy-MM-dd")}.{(report.SplitReportRowCount.HasValue ? "zip" : "xlsx")}",
                        TimeSpan.FromMinutes(10));

                    using (var context = new AnalyticsContext())
                    {
                        w.Stop();
                        var er = new ExportedReport
                        {
                            ReportArchiveKey = archiveKey,
                            ReportDate = reportDate,
                            ReportName = report.ReportName,
                            GenerationTimeInMs = w.ElapsedMilliseconds
                        };
                        context.ExportedReports.Add(er);
                        context.SaveChanges();
                    }
                }
                finally
                {
                    foreach (var tmpfile in tempFiles)
                        try { File.Delete(tmpfile); } catch { /*Ignored*/}
                }
            }

            failedMessage = null;
            return true;
        }

        private ExcelQueryExporter.CivicRegNrMapping SetupCivicRegNrMapping(ScheduledReport report)
        {
            var s = NEnv.CivicRegNrInsertionSettings;

            if (s.IsEnabled && report.CustomerIdColumns != null && report.CustomerIdColumns.Count > 0)
            {
                ExcelCustomerIdToCivicNrReplacer replacer;
                IDictionary<int, string> civicRegNrByCustomerId = null;
                replacer = new ExcelCustomerIdToCivicNrReplacer();
                civicRegNrByCustomerId = ExcelCustomerIdToCivicNrReplacer.CreateCivicRegnrCustomerClientSource(() => new CustomerClient(s.GetSystemUserBearerToken))(replacer.GetAllCustomerIds(report.CustomerIdQuery));
                return new ExcelQueryExporter.CivicRegNrMapping
                {
                    CivicRegNrByCustomerId = civicRegNrByCustomerId,
                    CustomerIdColumns = report.CustomerIdColumns
                };
            }
            return null;
        }

        public static List<ScheduledReport> ParseReportsFromDwModel(XDocument d)
        {
            var reports = new List<ScheduledReport>();
            var s = d.Descendants().Where(x => x.Name.LocalName == "ScheduledExcelExports").FirstOrDefault();
            if (s == null)
                return reports;

            var weekDayNames = Enum.GetNames(typeof(DayOfWeek));

            foreach (var n in s.Descendants().Where(x => x.Name.LocalName == "ScheduledExcelExport"))
            {
                var f = n.Attribute("frequency")?.Value;
                DayOfWeek? onlyRunOnThisDay;
                if (f == "Daily")
                    onlyRunOnThisDay = null;
                else if (weekDayNames.Contains(f))
                    onlyRunOnThisDay = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), f);
                else
                    throw new Exception($"Only frequency=\"Daily\" or one of {string.Join(", ", weekDayNames)} is suported for ScheduledExcelExport");

                var query = n.Descendants().Where(x => x.Name.LocalName == "Script").Single().Value;
                var roles = n.Descendants().Where(x => x.Name.LocalName == "ViewableByRole").Select(x => x.Value).ToList();
                var customerIdColumns = new HashSet<string>(n.Descendants().Where(x => x.Name.LocalName == "CustomerIdColumn").Select(x => x.Value).ToList());
                var customerIdQuery = n.Descendants().Where(x => x.Name.LocalName == "CustomerIdQuery").SingleOrDefault()?.Value;

                Func<string, int?> parseSplitReportRowCount = x => string.IsNullOrWhiteSpace(x) ? (int?)null : int.Parse(x);
                var splitReportRowCount = parseSplitReportRowCount(n.Descendants().Where(x => x.Name.LocalName == "SplitReportRowCount").SingleOrDefault()?.Value);

                if (roles.Count == 0)
                    throw new Exception("ScheduledExcelExport must have at least one ViewableByRole");
                reports.Add(new ScheduledReport
                {
                    ReportName = n.Attribute("name").Value,
                    FriendlyName = n.Attribute("friendlyname")?.Value ?? n.Attribute("name").Value,
                    IsDaily = true,
                    QueryScript = query,
                    Roles = roles,
                    CustomerIdColumns = customerIdColumns,
                    CustomerIdQuery = customerIdQuery,
                    SplitReportRowCount = splitReportRowCount,
                    OnlyRunOnThisDay = onlyRunOnThisDay
                });
            }

            return reports;
        }

        public class ScheduledReport
        {
            public string QueryScript { get; set; }
            public string ReportName { get; set; }
            public string FriendlyName { get; set; }
            public bool IsDaily { get; set; }
            public List<string> Roles { get; set; }
            public HashSet<string> CustomerIdColumns { get; set; }
            public string CustomerIdQuery { get; set; }
            public int? SplitReportRowCount { get; set; }
            public DayOfWeek? OnlyRunOnThisDay { get; set; }
        }
    }
}
