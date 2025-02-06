using nCredit.Code;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using nCredit.Excel;
using NTech;
using NTech.Banking.BookKeeping;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Xml.Linq;

namespace nCredit.Controllers
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
            using (var context = new CreditContext())
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

        [HttpPost]
        [Route("Api/BookkeepingFiles/CreateFile")]
        public ActionResult CreateFile(IDictionary<string, string> schedulerData = null)
        {
            Func<string, string> getSchedulerData = s => (schedulerData != null && schedulerData.ContainsKey(s)) ? schedulerData[s] : null;
            var skipDeliveryExport = (getSchedulerData("skipDeliveryExport") ?? "false").Trim().ToLowerInvariant() == "true";
            var exportProfileName = skipDeliveryExport ? null : NEnv.BookKeepingFileExportProfileName;

            using (var context = new CreditContextExtended(GetCurrentUserMetadata(), Clock))
            {
                var mgr = Service.BookKeeping;
                using (var tr = context.Database.BeginTransaction())
                {
                    var dates = BookKeepingFileManager.GetDatesToHandle(context);

                    if (dates.Count == 0)
                        return Json2(new { noNewTransactions = true });

                    OutgoingBookkeepingFileHeader h;

                    var selfProviderNames = NEnv.GetAffiliateModels().Where(x => x.IsSelf).Select(x => x.ProviderName).ToHashSet();
                    
                    List<string> warnings;
                    var wasFileCreated = mgr.TryCreateBookKeepingFile(context, dates, Service.DocumentClientHttpContext, exportProfileName, selfProviderNames, Service.KeyValueStore, NEnv.BookKeepingAccountPlan, out h, out warnings);

                    warnings = warnings ?? new List<string>();
                    if (wasFileCreated)
                    {
                        context.SaveChanges();
                        tr.Commit();
                    }

                    warnings.AddRange(mgr.GetBookKeepingWarnings(dates)?.ToList() ?? new List<string>());

                    if (wasFileCreated)
                    {
                        return Json2(new { noNewTransactions = false, bookkeepingFileHeaderId = h.Id, warnings = warnings });
                    }
                    else
                    {
                        return Json2(new { noNewTransactions = true, warnings = warnings });
                    }
                }
            }
        }

        private (List<BookKeepingRuleDescriptionTableRow> Rows, HashSet<string> AllConnections) GetBookKeepingDescriptionTableRows()
        {
            var ruleSet = NtechBookKeepingRuleFile.Parse(XDocuments.Load(NEnv.BookKeepingRuleFileName));

            var allConnections = new HashSet<string>(ruleSet.BusinessEventRules.SelectMany(x => x.Bookings.SelectMany(y => y.Connections)).ToList());

            Func<NtechBookKeepingRuleFile.BookingRule, string> describeFilter = x =>
            {
                if (!string.IsNullOrWhiteSpace(x.OnlySubAccountCode))
                {
                    return $"only sub account: {x.OnlySubAccountCode}";
                }
                return null;
            };

            var getAccountNr = BookKeepingFileManager.CreateBookKeepingAccountNrByAccountSource(Service.KeyValueStore, NEnv.BookKeepingAccountPlan);
            Func<NtechBookKeepingRuleFile.BookKeepingAccount, string> getAccountName = x =>
            {
                string accountName = null;
                x.GetLedgerAccountNr(y =>
                {
                    accountName = y;
                    return null;
                });

                return accountName;
            };

            var rows = ruleSet.BusinessEventRules.SelectMany(evt => evt.Bookings.Select((b, i) => new BookKeepingRuleDescriptionTableRow
            {
                EventName = i == 0 ? evt.BusinessEventName : null,
                LedgerAccountName = b.LedgerAccount,
                DebetAccountNr = getAccountNr(b.BookKeepingAccounts.Item1),
                DebetAccountName = getAccountName(b.BookKeepingAccounts.Item1),
                CreditAccountNr = getAccountNr(b.BookKeepingAccounts.Item2),
                CreditAccountName = getAccountName(b.BookKeepingAccounts.Item2),
                Connections = b.Connections,
                Filter = describeFilter(b),
            })).ToList();

            return (Rows: rows, AllConnections: allConnections);
        }

        [HttpPost]
        [Route("Api/Bookkeeping/RulesAsJson")]
        public ActionResult RulesAsJson()
        {
            var accountPlan = NEnv.BookKeepingAccountPlan;
            var allAccountNames = new List<string>();
            var storedAccountNamesNotInInitial = this.Service.KeyValueStore
                .GetAllValues(KeyValueStoreKeySpaceCode.BookKeepingAccountNrsV1.ToString())
                .Keys
                .ToHashSet();

            if (accountPlan != null)
            {
                foreach (var account in accountPlan.Accounts)
                {
                    allAccountNames.Add(account.Name);
                    storedAccountNamesNotInInitial.Remove(account.Name);
                }
            }
            foreach (var accountName in storedAccountNamesNotInInitial)
            {
                //There is no obivious case when there will be accounts in the db that 
                //are not in the account plan but if there are we dont want to hide it from the user.
                allAccountNames.Add(accountName);
            }

            var getAccountNrByName = BookKeepingFileManager.CreateBookKeepingAccountNrByNameSource(this.Service.KeyValueStore, NEnv.BookKeepingAccountPlan);
            var accountNrByAccountName = allAccountNames.ToDictionary(x => x, getAccountNrByName);

            var result = GetBookKeepingDescriptionTableRows();
            return Json2(new
            {
                ruleRows = result.Rows,
                allConnections = result.AllConnections,
                allAccountNames = allAccountNames,
                accountNrByAccountName
            });
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
                        Title = $"Credit bookkeeping rules"
                    }
                }
            };

            var result = GetBookKeepingDescriptionTableRows();
            var rows = result.Rows;
            var allConnections = result.AllConnections;

            var s = request.Sheets[0];
            var cols = DocumentClientExcelRequest.CreateDynamicColumnList(rows);
            cols.Add(rows.Col(x => x.EventName, ExcelType.Text, "Event name"));
            cols.Add(rows.Col(x => x.LedgerAccountName, ExcelType.Text, "Ledger account"));
            foreach (var c in allConnections)
            {
                cols.Add(rows.Col(x => x.Connections.Contains(c) ? "X" : "", ExcelType.Text, c));
            }
            cols.Add(rows.Col(x => x.DebetAccountNr, ExcelType.Text, "Debet account - nr"));
            cols.Add(rows.Col(x => x.DebetAccountName, ExcelType.Text, "name"));
            cols.Add(rows.Col(x => x.CreditAccountNr, ExcelType.Text, "Credit account - nr"));
            cols.Add(rows.Col(x => x.CreditAccountName, ExcelType.Text, "name"));
            if (rows.Any(x => !string.IsNullOrWhiteSpace(x.Filter)))
            {
                cols.Add(rows.Col(x => x.Filter, ExcelType.Text, "Filter"));
            }

            s.SetColumnsAndData(rows, cols.ToArray());

            var client = Service.DocumentClientHttpContext;
            var report = client.CreateXlsx(request);

            return new FileStreamResult(report, XlsxContentType) { FileDownloadName = $"CreditBookkeepingRules-{DateTime.Today.ToString("yyyy-MM-dd")}.xlsx" };
        }

        public class BookKeepingRuleDescriptionTableRow
        {
            public string EventName { get; set; }
            public string LedgerAccountName { get; set; }
            public HashSet<string> Connections { get; set; }
            public string DebetAccountNr { get; set; }
            public string DebetAccountName { get; set; }
            public string CreditAccountNr { get; set; }
            public string CreditAccountName { get; set; }
            public string Filter { get; set; }
        }
    }
}