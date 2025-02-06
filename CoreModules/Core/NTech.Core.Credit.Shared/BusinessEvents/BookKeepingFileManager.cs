using nCredit.Code.Services;
using nCredit.Excel;
using NTech.Banking.BookKeeping;
using NTech.Banking.SieFiles;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DbModel;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class BookKeepingFileManager : BusinessEventManagerOrServiceBase
    {
        private readonly ICreditEnvSettings envSettings;
        private readonly CreditContextFactory contextFactory;
        private readonly Func<NtechBookKeepingRuleFile> getNtechBookKeepingRuleFile;

        public BookKeepingFileManager(INTechCurrentUserMetadata currentUser, IClientConfigurationCore clientConfiguration, ICoreClock clock, 
            ICreditEnvSettings envSettings, CreditContextFactory contextFactory, Func<NtechBookKeepingRuleFile> getNtechBookKeepingRuleFile) : base(currentUser, clock, clientConfiguration)
        {
            this.envSettings = envSettings;
            this.contextFactory = contextFactory;
            this.getNtechBookKeepingRuleFile = getNtechBookKeepingRuleFile;
        }

        public class BookableTransaction
        {

            public int BusinessEventId { get; set; }
            public DateTime BookKeepingDate { get; set; }
            public DateTime TransactionDate { get; set; }
            public int? CreditNotificationId { get; set; }
            public string CreditNr { get; set; }
            public int? IncomingPaymentId { get; set; }
            public int? OutgoingPaymentId { get; set; }
            public int? WriteoffId { get; set; }
            public string AccountCode { get; set; }
            public string SubAccountCode { get; set; }
            public decimal Amount { get; set; }
            public DateTime? CreditNotificationDueDate { get; set; }
            public DateTime BusinessEventTransactionDate { get; set; }
            public string BusinessEventEventType { get; set; }
            public long AccountTransactionId { get; set; }
            public string CreditProviderName { get; set; }
        }

        public static SieBookKeepingFile CreateSieFileFromTransactions(
            List<Tuple<DateTime, List<BookableTransaction>>> eligableTransactionsPerDate, NtechBookKeepingRuleFile ruleSet,
            ICoreClock clock, ISet<string> selfProviderNames, IKeyValueStoreService keyValueStoreService, 
            NtechAccountPlanFile accountPlan,
            Action<long> observeBookingByTransactionId = null,
            Action<Dictionary<SieBookKeepingFile.Verification, Dictionary<string, string>>> observeConnections = null)
        {
            var connectionsByVerification = new Dictionary<SieBookKeepingFile.Verification, Dictionary<string, string>>();
            var getAccountNr = CreateBookKeepingAccountNrByAccountSource(keyValueStoreService, accountPlan);
            Tuple<string, string> GetAccountNrs(Tuple<NtechBookKeepingRuleFile.BookKeepingAccount, NtechBookKeepingRuleFile.BookKeepingAccount> accounts) =>
                Tuple.Create(getAccountNr(accounts.Item1), getAccountNr(accounts.Item2));

            var sie = new SieBookKeepingFile(() => clock.Now.DateTime)
            {
                ExportedCompanyName = ruleSet.CompanyName,
                ProgramName = "Näktergal Ab - Credit",
                ProgramVersion = Tuple.Create(1, 0)
            };
            if (ruleSet.CustomDimensions != null)
            {
                sie.DimensionsDeclarationRaw = ruleSet.CustomDimensions.CustomDimensionDeclaration;
            }

            foreach (var (_, allTransactionsForDate) in eligableTransactionsPerDate)
            {
                foreach (var eventRule in ruleSet.BusinessEventRules)
                {
                    var trs = allTransactionsForDate.Where(x => x.BusinessEventEventType == eventRule.BusinessEventName).ToList();
                    foreach (var evt in trs.GroupBy(x => new { x.BusinessEventId, x.BookKeepingDate, x.CreditNotificationId, x.CreditNr, x.IncomingPaymentId, x.OutgoingPaymentId }))
                    {
                        var f = evt.First();

                        var bookKeepingDate = evt.Key.BookKeepingDate;
                        var text = f.BusinessEventEventType;
                        var creditNr = evt.Select(x => x.CreditNr).FirstOrDefault();
                        var connectionIdByType = new Dictionary<string, string>();
                        if (creditNr != null)
                        {
                            text += $" C:[{creditNr}]";
                            connectionIdByType["CreditNr"] = creditNr;
                        }

                        var notificationDueDate = f.CreditNotificationDueDate;
                        if (notificationDueDate.HasValue)
                        {
                            text += $" N:[{notificationDueDate.Value.ToString("yyyy-MM-dd")}]";
                            connectionIdByType["CreditNotificationId"] = evt.Key.CreditNotificationId.Value.ToString();
                        }
                        
                        var incomingPaymentId = evt.Select(x => x.IncomingPaymentId).FirstOrDefault();
                        if (incomingPaymentId.HasValue)
                        {
                            text += $" I:[{incomingPaymentId.Value}]";
                            connectionIdByType["IncomingPaymentId"] = incomingPaymentId.Value.ToString();
                        }
                        
                        var outgoingPaymentId = evt.Select(x => x.OutgoingPaymentId).FirstOrDefault();
                        if (outgoingPaymentId.HasValue)
                        {
                            text += $" O:[{outgoingPaymentId.Value}]";
                            connectionIdByType["OutgoingPaymentId"] = outgoingPaymentId.Value.ToString();
                        }

                        var verification = sie.CreateVerification(bookKeepingDate, text: text, registrationDate: f.BusinessEventTransactionDate);
                        foreach (var tr in evt)
                        {
                            var trConnections = new HashSet<string>();
                            if (tr.CreditNr != null) trConnections.Add("Credit");
                            if (tr.CreditNotificationId.HasValue) trConnections.Add("Notification");
                            if (tr.IncomingPaymentId.HasValue) trConnections.Add("IncomingPayment");
                            if (tr.OutgoingPaymentId.HasValue) trConnections.Add("OutgoingPayment");
                            if (tr.WriteoffId.HasValue) trConnections.Add("Writeoff");

                            foreach (var bookingRule in eventRule.Bookings)
                            {
                                if (bookingRule.LedgerAccount == tr.AccountCode && bookingRule.Connections.SetEquals(trConnections) && (bookingRule.OnlySubAccountCode == null || bookingRule.OnlySubAccountCode == tr.SubAccountCode))
                                {
                                    var tp = sie.WithTransactionPair(tr.Amount, GetAccountNrs(bookingRule.BookKeepingAccounts));
                                    if (ruleSet.CommonCostPlace != null)
                                        tp.HavingCostPlaceDimension(ruleSet.CommonCostPlace.Item1, ruleSet.CommonCostPlace.Item2);
                                    else if (ruleSet.CustomDimensions != null)
                                    {
                                        string dimensionCase = "fallback";
                                        if (tr.CreditProviderName != null && !selfProviderNames.Contains(tr.CreditProviderName))
                                            dimensionCase = "providerCredit";
                                        else if (tr.CreditProviderName != null && selfProviderNames.Contains(tr.CreditProviderName))
                                            dimensionCase = "ownCredit";

                                        tp.HavingDimensionRaw(ruleSet.CustomDimensions.CustomDimensionTextByCaseName.Req(dimensionCase));
                                    }
                                    tp.MergeIntoVerification(verification);
                                    observeBookingByTransactionId?.Invoke(tr.AccountTransactionId);
                                }
                            }
                        }
                        if (verification.Transactions != null && verification.Transactions.Count > 0)
                        {
                            sie.AddVerification(verification);
                            connectionsByVerification[verification] = connectionIdByType;
                        }                            
                    }
                }
            }

            observeConnections?.Invoke(connectionsByVerification);

            return sie;
        }

        public static List<Tuple<DateTime, List<BookableTransaction>>> CreateEligableTransactions(IQueryable<AccountTransaction> dateFilteredTransactions, NtechBookKeepingRuleFile ruleSet)
        {
            var eligableAccountCodes = ruleSet.BusinessEventRules.SelectMany(x => x.Bookings.Select(y => y.LedgerAccount))
                .Distinct().ToList();

            return dateFilteredTransactions
                .Where(x => eligableAccountCodes.Contains(x.AccountCode))
                .Select(x => new BookableTransaction
                {
                    BusinessEventId = x.BusinessEventId,
                    BookKeepingDate = x.BookKeepingDate,
                    CreditNotificationId = x.CreditNotificationId,
                    TransactionDate = x.TransactionDate,
                    CreditNr = x.CreditNr,
                    IncomingPaymentId = x.IncomingPaymentId,
                    OutgoingPaymentId = x.OutgoingPaymentId,
                    WriteoffId = x.WriteoffId,
                    AccountCode = x.AccountCode,
                    SubAccountCode = x.SubAccountCode,
                    Amount = x.Amount,
                    CreditNotificationDueDate = (DateTime?)x.CreditNotification.DueDate,
                    BusinessEventTransactionDate = x.BusinessEvent.TransactionDate,
                    BusinessEventEventType = x.BusinessEvent.EventType,
                    AccountTransactionId = x.Id,
                    CreditProviderName = x.Credit.ProviderName
                })
                .ToList()
                .GroupBy(x => x.TransactionDate)
                .OrderBy(x => x.Key)
                .Select(x => Tuple.Create(x.Key, x.ToList()))
                .ToList();
        }

        public enum BookKeepingPreviewFileFormatCode
        {
            Excel,
            Sie
        }
        public MemoryStream CreatePreview(ICreditContextExtended context, DateTime fromDate, DateTime toDate,
            ISet<string> selfProviderNames, IKeyValueStoreService keyValueStoreService, IDocumentClient documentClient,
           BookKeepingPreviewFileFormatCode formatCode,
           NtechAccountPlanFile accountPlan)
        {
            var ruleSet = getNtechBookKeepingRuleFile();
            var eligableTransactionsPerDate = CreateEligableTransactions(context.TransactionsQueryable.Where(x => x.TransactionDate >= fromDate && x.TransactionDate <= toDate), ruleSet);
            var bookedTransactionIds = new List<long>();
            var sie = CreateSieFileFromTransactions(eligableTransactionsPerDate, ruleSet, Clock, selfProviderNames, keyValueStoreService, accountPlan, x => bookedTransactionIds.Add(x));
            if (formatCode == BookKeepingPreviewFileFormatCode.Excel)
            {
                var excelRequest = CreateExcelFromSieRequest(sie);
                return documentClient.CreateXlsx(excelRequest);
            }
            else if (formatCode == BookKeepingPreviewFileFormatCode.Sie)
            {
                var ms = new MemoryStream();
                sie.Save(ms);
                ms.Flush();
                ms.Position = 0;
                return ms;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public bool TryCreateBookKeepingFile(ICreditContextExtended context, List<DateTime> transactionDates, IDocumentClient documentClient, string exportProfileName, ISet<string> selfProviderNames, IKeyValueStoreService keyValueStoreService, NtechAccountPlanFile accountPlan, out OutgoingBookkeepingFileHeader h, out List<string> warnings,
            Action<SieBookKeepingFile> observeFile = null)
        {
            warnings = new List<string>();

            if (transactionDates == null || transactionDates.Count == 0)
            {
                h = null;
                return false;
            }

            context.EnsureCurrentTransaction();

            h = new OutgoingBookkeepingFileHeader
            {
                TransactionDate = Clock.Today,
                ChangedById = UserId,
                ChangedDate = Clock.Now,
                InformationMetaData = InformationMetadata,
                Transactions = new List<AccountTransaction>()
            };
            context.AddOutgoingBookkeepingFileHeaders(h);

            var ruleSet = getNtechBookKeepingRuleFile();

            var eligableTransactionsPerDate = CreateEligableTransactions(
                context.TransactionsQueryable.Where(x => transactionDates.Contains(x.TransactionDate)),
                ruleSet);

            var bookedTransactionIds = new List<long>();

            Dictionary<SieBookKeepingFile.Verification, Dictionary<string, string>> connectionsByVerification = null;
            var sie = CreateSieFileFromTransactions(eligableTransactionsPerDate, ruleSet, Clock, selfProviderNames, keyValueStoreService, accountPlan,
                observeBookingByTransactionId: x => bookedTransactionIds.Add(x), 
                observeConnections: x => connectionsByVerification = x);
            observeFile?.Invoke(sie);
            var bookedTransactions = context.TransactionsQueryable.Where(x => bookedTransactionIds.Contains(x.Id)).ToList();

            foreach (var t in bookedTransactions)
            {
                t.OutgoingBookkeepingFile = h;
            }

            if (bookedTransactions.Count == 0)
            {
                h = null;
                return false;
            }

            h.FromTransactionDate = transactionDates.Min();
            h.ToTransactionDate = transactionDates.Max();

            var dateTag = $"{h.FromTransactionDate.ToString("yyyy-MM-dd")}-{h.ToTransactionDate.ToString("yyyy-MM-dd")}";
            using (var ms = new MemoryStream())
            {
                sie.Save(ms);

                string customerBookKeepingFileEnding = envSettings.SieFileEnding;
                var filename = $"BookKeeping-{dateTag}." + customerBookKeepingFileEnding;
                h.FileArchiveKey = documentClient.ArchiveStore(ms.ToArray(), "text/plain", filename);
                if (exportProfileName != null)
                {
                    var exportResult = documentClient.TryExportArchiveFile(h.FileArchiveKey, exportProfileName);
                    int timeInMs = exportResult.TimeInMs;
                    List<string> successProfileNames = exportResult.SuccessProfileNames;
                    List<string> failedProfileNames = exportResult.FailedProfileNames;

                    if (!exportResult.IsSuccess)
                    {
                        warnings.Add($"Export with profile '{exportProfileName}' failed for '{filename}'");
                    }
                }
            }

            h.XlsFileArchiveKey = CreateExcelFromSie(sie, dateTag, documentClient);

            //Store transactions and verifications
            foreach(var fileVerification in sie.Verifications)
            {
                var dbVerification = new SieFileVerification
                {
                    Date = fileVerification.Date.Value,
                    OutgoingFile = h,
                    RegistrationDate = fileVerification.RegistrationDate.Value,
                    Text = (fileVerification.Text ?? "None").ClipRight(256),
                    Connections = new List<SieFileConnection>(),
                    Transactions = new List<SieFileTransaction>()
                };
                context.AddSieFileVerifications(dbVerification);

                foreach (var transaction in fileVerification.Transactions)
                {
                    dbVerification.Transactions.Add(new SieFileTransaction
                    {
                        AccountNr = transaction.Account,
                        Amount = transaction.Amount,
                        Verification = dbVerification
                    });
                }
                foreach(var connection in connectionsByVerification[fileVerification])
                {
                    dbVerification.Connections.Add(new SieFileConnection
                    {
                        ConnectionType = connection.Key,
                        ConnectionId = connection.Value
                    });
                }
            }

            return true;
        }

        public string[] GetBookKeepingWarnings(List<DateTime> transactionDates)
        {
            var warnDays = new List<string>();
            foreach (var forTransactionDate in transactionDates)
            {
                using (var context = contextFactory.CreateContext())
                {
                    var baseQuery = context.TransactionsQueryable.Where(x => x.TransactionDate == forTransactionDate);
                    var isOk = baseQuery.Any(x => x.OutgoingBookkeepingFileHeaderId.HasValue) || (baseQuery.Count() == 0);
                    if (!isOk)
                        warnDays.Add(forTransactionDate.ToString("yyyy-MM-dd"));
                }
            }
            if (warnDays.Any())
                return new[] { $"There are whole days with transactions but no bookkeeping: {string.Join(", ", warnDays)}" };
            else
                return null;
        }

        public static Func<NtechBookKeepingRuleFile.BookKeepingAccount, string> CreateBookKeepingAccountNrByAccountSource(IKeyValueStoreService keyValueStoreService, NtechAccountPlanFile accountPlan)
        {
            var getNrByName = CreateBookKeepingAccountNrByNameSource(keyValueStoreService, accountPlan);

            return (NtechBookKeepingRuleFile.BookKeepingAccount account) => account.GetLedgerAccountNr(getNrByName);
        }

        public static Func<string, string> CreateBookKeepingAccountNrByNameSource(IKeyValueStoreService keyValueStoreService, NtechAccountPlanFile accountPlan)
        {
            var databaseAccountNrs = keyValueStoreService.GetAllValues(KeyValueStoreKeySpaceCode.BookKeepingAccountNrsV1.ToString());

            return accountName =>
                {
                    var initialAccountNr = accountPlan.Accounts.FirstOrDefault(y => y.Name == accountName)?.InitialAccountNr;
                    var databaseAccountNr = databaseAccountNrs.Opt(accountName);

                    var accountNr = databaseAccountNr ?? initialAccountNr;

                    if (string.IsNullOrWhiteSpace(accountNr))
                        throw new Exception($"The account '{accountName}' does not exist either in the BookKeepingAccountPlan.xml file or in the BookKeepingAccountNrsV1 keyspace.");

                    return accountNr;
                };
        }

        public static List<DateTime> GetDatesToHandle(ICreditContextExtended context)
        {
            DateTime fromDate;

            var lastFileToDate = context
                .OutgoingBookkeepingFileHeadersQueryable
                .Max(x => (DateTime?)x.ToTransactionDate);

            if (lastFileToDate.HasValue)
            {
                fromDate = lastFileToDate.Value.AddDays(1);
            }
            else
            {
                var minTrDate = context.TransactionsQueryable.Min(x => (DateTime?)x.TransactionDate);
                if (!minTrDate.HasValue)
                    return new List<DateTime>();
                else
                    fromDate = minTrDate.Value;
            }

            DateTime toDate = context.CoreClock.Today.AddDays(-1);
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

        private DocumentClientExcelRequest CreateExcelFromSieRequest(SieBookKeepingFile sie)
        {
            //Create excel version
            var sheets = new List<DocumentClientExcelRequest.Sheet>();

            //Vers and transactions
            var trRows = new List<ExcelRowModel>();
            foreach (var ver in sie.Verifications)
            {
                trRows.Add(new ExcelRowModel
                {
                    RowType = "Verification",
                    VerDate = ver.Date,
                    VerText = ver.Text,
                    VerRegDate = ver.RegistrationDate
                });
                foreach (var tr in ver.Transactions)
                {
                    var firstOd = tr.ObjectNrsByDimensionNr.Take(1).Select(x => new KeyValuePair<int, string>?(x)).FirstOrDefault();
                    trRows.Add(new ExcelRowModel
                    {
                        RowType = "Transaction",
                        TransactionAccount = tr.Account,
                        TransactionDebetAmount = tr.Amount >= 0m ? new decimal?(tr.Amount) : null,
                        TransactionCreditAmount = tr.Amount < 0m ? new decimal?(-tr.Amount) : null,
                        DimensionNr = firstOd.HasValue ? new int?(firstOd.Value.Key) : null,
                        ObjectNr = firstOd.HasValue ? firstOd.Value.Value : null,
                    });
                    foreach (var od in tr.ObjectNrsByDimensionNr.Skip(1))
                    {
                        trRows.Add(new ExcelRowModel
                        {
                            RowType = "TransactionDimension",
                            DimensionNr = od.Key,
                            ObjectNr = od.Value
                        });
                    }
                }
            }
            var trSheet = new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = $"Verifications"
            };

            trSheet.SetColumnsAndData(trRows,
                trRows.Col(x => x.VerDate, ExcelType.Date, "VerDate"),
                trRows.Col(x => x.VerText, ExcelType.Text, "VerText"),
                trRows.Col(x => x.VerRegDate, ExcelType.Date, "VerRegDate"),
                trRows.Col(x => x.TransactionAccount, ExcelType.Text, "Account"),
                trRows.Col(x => x.TransactionDebetAmount, ExcelType.Number, "DebetAmount"),
                trRows.Col(x => x.TransactionCreditAmount, ExcelType.Number, "CreditAmount"),
                trRows.Col(x => x.DimensionNr.HasValue ? x.DimensionNr.Value.ToString() : null, ExcelType.Text, "DimensionNr"),
                trRows.Col(x => x.ObjectNr, ExcelType.Text, "ObjectNr"),
                trRows.Col(x => x.RowType, ExcelType.Text, "RowType"));
            sheets.Add(trSheet);

            //Dimensions
            var dimSheet = new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = $"Dimensions"
            };
            var dims = sie.Dimensions.ToList();
            dimSheet.SetColumnsAndData(dims,
                dims.Col(x => x.Nr.ToString(), ExcelType.Text, "DimensionNr"),
                dims.Col(x => x.Name, ExcelType.Text, "DimensionName"));
            sheets.Add(dimSheet);

            //Objects
            var objSheet = new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = $"Objects"
            };
            var objs = sie.Objects.ToList();
            objSheet.SetColumnsAndData(objs,
                objs.Col(x => x.DimensionNr.ToString(), ExcelType.Text, "DimensionNr"),
                objs.Col(x => x.ObjectNr, ExcelType.Text, "ObjectNr"),
                objs.Col(x => x.Name, ExcelType.Text, "ObjectName"));
            sheets.Add(objSheet);

            return new DocumentClientExcelRequest
            {
                Sheets = sheets.ToArray()
            };
        }

        private string CreateExcelFromSie(SieBookKeepingFile sie, string dateTag, IDocumentClient documentClient)
        {
            var request = CreateExcelFromSieRequest(sie);
            return documentClient.CreateXlsxToArchive(request, $"BookKeeping-{dateTag}.xlsx");
        }

        private class ExcelRowModel
        {
            public DateTime? VerDate { get; internal set; }
            public DateTime? VerRegDate { get; internal set; }
            public string VerText { get; internal set; }
            public string TransactionAccount { get; internal set; }
            public decimal? TransactionDebetAmount { get; internal set; }
            public decimal? TransactionCreditAmount { get; internal set; }
            public int? DimensionNr { get; internal set; }
            public string ObjectNr { get; internal set; }
            public string RowType { get; set; }
        }
    }
}