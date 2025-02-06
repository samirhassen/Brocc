using nSavings.Code;
using nSavings.DbModel.BusinessEvents;
using nSavings.Excel;
using NTech;
using NTech.Banking.BookKeeping;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Xml.Linq;

namespace nSavings.Controllers
{
    [NTechApi]
    public class ApiBookkeepingFilesController : NController
    {
        public class Filter
        {
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
        }

        [HttpPost]
        [Route("Api/BookkeepingFiles/GetFilesPage")]
        public ActionResult GetFilesPage(int pageSize, Filter filter = null, int pageNr = 0)
        {
            using (var context = new SavingsContext())
            {
                var baseResult = context
                    .OutgoingBookkeepingFileHeaders
                    .AsQueryable();

                if (filter != null && filter.FromDate.HasValue)
                {
                    var fd = filter.FromDate.Value.Date;
                    baseResult = baseResult.Where(x => x.TransactionDate >= fd);
                }

                if (filter != null && filter.ToDate.HasValue)
                {
                    var td = filter.ToDate.Value.Date;
                    baseResult = baseResult.Where(x => x.TransactionDate <= td);
                }

                var totalCount = baseResult.Count();
                var currentPage = baseResult
                    .OrderByDescending(x => x.Timestamp)
                    .Skip(pageSize * pageNr)
                    .Take(pageSize)
                    .ToList()
                    .Select(x => new
                    {
                        x.TransactionDate,
                        x.FromTransactionDate,
                        x.ToTransactionDate,
                        x.FileArchiveKey,
                        x.XlsFileArchiveKey,
                        UserId = x.ChangedById
                    })
                    .ToList()
                    .Select(x => new
                    {
                        x.TransactionDate,
                        x.FromTransactionDate,
                        x.ToTransactionDate,
                        x.UserId,
                        UserDisplayName = GetUserDisplayNameByUserId(x.UserId.ToString()),
                        x.FileArchiveKey,
                        x.XlsFileArchiveKey,
                        ArchiveDocumentUrl = Url.Action("ArchiveDocument", "ApiArchiveDocument", new { key = x.FileArchiveKey, setFileDownloadName = true }),
                        ExcelDocumentUrl = x.XlsFileArchiveKey == null ? null : Url.Action("ArchiveDocument", "ApiArchiveDocument", new { key = x.XlsFileArchiveKey, setFileDownloadName = true })
                    })
                    .ToList();

                var nrOfPages = (totalCount / pageSize) + (totalCount % pageSize == 0 ? 0 : 1);

                return Json2(new
                {
                    CurrentPageNr = pageNr,
                    TotalNrOfPages = nrOfPages,
                    Page = currentPage.ToList(),
                    Filter = filter
                });
            }
        }

        public static List<DateTime> GetDates(SavingsContext context, IClock clock)
        {
            DateTime fromDate;

            var lastFileToDate = context
                .OutgoingBookkeepingFileHeaders
                .Max(x => (DateTime?)x.ToTransactionDate);

            if (lastFileToDate.HasValue)
            {
                fromDate = lastFileToDate.Value.AddDays(1);
            }
            else
            {
                var minTrDate = context.LedgerAccountTransactions.Min(x => (DateTime?)x.TransactionDate);
                if (!minTrDate.HasValue)
                    return new List<DateTime>();
                else
                    fromDate = minTrDate.Value;
            }

            DateTime toDate = clock.Today.AddDays(-1);
            var dates = new List<DateTime>();
            var d = fromDate;
            int guard = 0;

            while (d <= toDate && guard++ < 10000)
            {
                dates.Add(d);
                d = d.AddDays(1);
            }
            if (guard > 9000)
                throw new Exception("Hit guard code in GetDates");

            return dates;
        }

        [HttpPost]
        [Route("Api/BookkeepingFiles/CreateFile")]
        public ActionResult CreateFile()
        {
            using (var context = new SavingsContext())
            {
                var documentClient = new Code.DocumentClient();
                var mgr = new BookKeepingFileManager(CurrentUserId, InformationMetadata);
                using (var tr = context.Database.BeginTransaction())
                {
                    var dates = GetDates(context, Clock);

                    if (dates.Count == 0)
                        return Json2(new { noNewTransactions = true });

                    OutgoingBookkeepingFileHeader h;
                    List<string> warnings;
                    var wasFileCreated = mgr.TryCreateBookKeepingFile(context, dates, documentClient, out h, out warnings);

                    if (wasFileCreated)
                    {
                        context.SaveChanges();
                        tr.Commit();
                    }

                    warnings = warnings ?? new List<string>();
                    warnings.AddRange(mgr.GetBookKeepingWarnings(dates) ?? new string[] { });

                    if (wasFileCreated)
                    {
                        return Json2(new { noNewTransactions = false, bookkeepingFileHeaderId = h.Id, warnings = warnings.Count > 0 ? warnings : null });
                    }
                    else
                    {
                        return Json2(new { noNewTransactions = true, warnings = warnings.Count > 0 ? warnings : null });
                    }
                }
            }
        }

        [HttpGet]
        [Route("Api/Bookkeeping/RulesAsXls")]
        public ActionResult RulesAsXls()
        {
            var request = new DocumentClientExcelRequest
            {
                Sheets = new DocumentClientExcelRequest.Sheet[]
                {
                            new DocumentClientExcelRequest.Sheet
                            {
                                AutoSizeColumns = true,
                                Title = $"Savings bookkeeping rules"
                            }
                }
            };

            var ruleSet = NtechBookKeepingRuleFile.Parse(XDocuments.Load(NEnv.BookKeepingRuleFileName));

            var allConnections = new HashSet<string>(ruleSet.BusinessEventRules.SelectMany(x => x.Bookings.SelectMany(y => y.Connections)).ToList());

            var rows = ruleSet.BusinessEventRules.SelectMany(evt => evt.Bookings.Select((b, i) => new
            {
                EventName = i == 0 ? evt.BusinessEventName : null,
                LedgerAccount = b.LedgerAccount,
                DebetAccount = BookKeepingFileManager.EnsureAccountNr(b.BookKeepingAccounts.Item1),
                CreditAccount = BookKeepingFileManager.EnsureAccountNr(b.BookKeepingAccounts.Item2),
                Connections = b.Connections
            })).ToList();

            var s = request.Sheets[0];
            var cols = DocumentClientExcelRequest.CreateDynamicColumnList(rows);
            cols.Add(rows.Col(x => x.EventName, ExcelType.Text, "Event name"));
            cols.Add(rows.Col(x => x.LedgerAccount, ExcelType.Text, "Ledger account"));
            foreach (var c in allConnections)
            {
                cols.Add(rows.Col(x => x.Connections.Contains(c) ? "X" : "", ExcelType.Text, c));
            }
            cols.Add(rows.Col(x => x.DebetAccount, ExcelType.Text, "Debet account"));
            cols.Add(rows.Col(x => x.CreditAccount, ExcelType.Text, "Credit account"));

            s.SetColumnsAndData(rows, cols.ToArray());

            var client = new DocumentClient();
            var report = client.CreateXlsx(request);

            return new FileStreamResult(report, XlsxContentType) { FileDownloadName = $"SavingsBookkeepingRules-{DateTime.Today.ToString("yyyy-MM-dd")}.xlsx" };
        }
    }
}