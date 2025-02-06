using nSavings.Code;
using nSavings.Excel;
using NTech.Banking.BookKeeping;
using NTech.Banking.SieFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace nSavings.DbModel.BusinessEvents
{
    public class BookKeepingFileManager : BusinessEventManagerBase
    {
        public BookKeepingFileManager(int userId, string informationMetadata) : base(userId, informationMetadata)
        {
        }

        //Temporary solution until we have implemented support for account plans
        public static string EnsureAccountNr(NtechBookKeepingRuleFile.BookKeepingAccount nr) =>
            nr.GetLedgerAccountNr(_ => { throw new NotImplementedException(); });
        public static Tuple<string, string> EnsureAccountNrs(Tuple<NtechBookKeepingRuleFile.BookKeepingAccount, NtechBookKeepingRuleFile.BookKeepingAccount> accounts) =>
            Tuple.Create(EnsureAccountNr(accounts.Item1), EnsureAccountNr(accounts.Item2));


        public bool TryCreateBookKeepingFile(SavingsContext context, List<DateTime> transactionDates, DocumentClient documentClient, out OutgoingBookkeepingFileHeader h, out List<string> warnings)
        {
            warnings = new List<string>();

            if (transactionDates == null || transactionDates.Count == 0)
            {
                h = null;
                return false;
            }

            context.RequireAmbientTransaction();

            h = new OutgoingBookkeepingFileHeader
            {
                TransactionDate = Clock.Today,
                ChangedById = UserId,
                ChangedDate = Clock.Now,
                InformationMetaData = InformationMetadata,
                Transactions = new List<LedgerAccountTransaction>()
            };
            context.OutgoingBookkeepingFileHeaders.Add(h);

            var ruleSet = NtechBookKeepingRuleFile.Parse(XDocuments.Load(NEnv.BookKeepingRuleFileName));

            var sie = new SieBookKeepingFile(() => Clock.Now.DateTime)
            {
                ExportedCompanyName = ruleSet.CompanyName,
                ProgramName = "Näktergal Ab - Savings",
                ProgramVersion = Tuple.Create(1, 0)
            };
            if (ruleSet.CustomDimensions != null)
            {
                sie.DimensionsDeclarationRaw = ruleSet.CustomDimensions.CustomDimensionDeclaration;
            }

            var bookedTransactions = new List<LedgerAccountTransaction>();

            foreach (var forTransactionDate in transactionDates)
            {
                foreach (var eventRule in ruleSet.BusinessEventRules)
                {
                    var trs = context
                        .LedgerAccountTransactions
                        .Include("BusinessEvent")
                        .Where(x => x.BusinessEvent.EventType == eventRule.BusinessEventName && x.TransactionDate == forTransactionDate)
                        .ToList();

                    foreach (var evt in trs.GroupBy(x => new { x.BusinessEventId, x.BookKeepingDate, x.SavingsAccountNr }))
                    {
                        var bi = evt.First().BusinessEvent;
                        var bookKeepingDate = evt.Key.BookKeepingDate;
                        var text = $"{bi.EventType}:[{bi.Id}]";
                        var savingsAccountNr = evt.Select(x => x.SavingsAccountNr).FirstOrDefault();
                        if (savingsAccountNr != null)
                        {
                            text += $" S:[{savingsAccountNr}]";
                        }

                        var verification = sie.CreateVerification(bookKeepingDate, text, registrationDate: bi.TransactionDate);
                        foreach (var tr in evt)
                        {
                            var trConnections = new HashSet<string>();
                            if (tr.SavingsAccountNr != null) trConnections.Add("SavingsAccount");
                            if (tr.IncomingPaymentId.HasValue) trConnections.Add("IncomingPayment");
                            if (tr.OutgoingPaymentId.HasValue) trConnections.Add("OutgoingPayment");

                            foreach (var bookingRule in eventRule.Bookings)
                            {
                                if (bookingRule.LedgerAccount == tr.AccountCode && bookingRule.Connections.SetEquals(trConnections))
                                {
                                    var tp = sie.WithTransactionPair(tr.Amount, EnsureAccountNrs(bookingRule.BookKeepingAccounts));
                                    if (ruleSet.CommonCostPlace != null)
                                        tp.HavingCostPlaceDimension(ruleSet.CommonCostPlace.Item1, ruleSet.CommonCostPlace.Item2);
                                    else if (ruleSet.CustomDimensions != null)
                                    {
                                        string dimensionCase = "fallback";
                                        if (!ruleSet.CustomDimensions.CustomDimensionTextByCaseName.ContainsKey(dimensionCase))
                                            throw new Exception($"Missing custom dimension {dimensionCase}");
                                        tp.HavingDimensionRaw(ruleSet.CustomDimensions.CustomDimensionTextByCaseName[dimensionCase]);
                                    }
                                    tp.MergeIntoVerification(verification);
                                    bookedTransactions.Add(tr);
                                }
                            }
                        }
                        if (verification.Transactions != null && verification.Transactions.Count > 0)
                            sie.AddVerification(verification);
                    }
                }
            }

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

                string customerBookkeepingFileEnding = NEnv.SieFileEnding;
                var filename = $"Savings-BookKeeping-{dateTag}." + customerBookkeepingFileEnding;
                h.FileArchiveKey = documentClient.ArchiveStore(ms.ToArray(), "text/plain", filename);

                string exportProfileName = NEnv.BookkeepingFileExportProfileName;
                if (exportProfileName != null)
                {
                    int timeInMs;
                    List<string> successProfileNames;
                    List<string> failedProfileNames;
                    if (!documentClient.TryExportArchiveFile(h.FileArchiveKey, exportProfileName, out successProfileNames, out failedProfileNames, out timeInMs))
                    {
                        warnings.Add($"Export with profile '{exportProfileName}' failed for '{filename}'");
                    }
                }
            }

            h.XlsFileArchiveKey = CreateExcelFromSie(sie, dateTag, documentClient);

            return true;
        }

        public string[] GetBookKeepingWarnings(List<DateTime> transactionDates)
        {
            var warnDays = new List<string>();
            foreach (var forTransactionDate in transactionDates)
            {
                using (var context = new SavingsContext())
                {
                    var baseQuery = context.LedgerAccountTransactions.Where(x => x.TransactionDate == forTransactionDate);
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

        private string CreateExcelFromSie(SieBookKeepingFile sie, string dateTag, DocumentClient documentClient)
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
                    VerRegDate = ver.RegistrationDate,
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

            var request = new DocumentClientExcelRequest
            {
                Sheets = sheets.ToArray()
            };

            return documentClient.CreateXlsxToArchive(request, $"Savings-BookKeeping-{dateTag}.xlsx");
        }

        private class ExcelRowModel
        {
            public DateTime? VerDate { get; internal set; }
            public string VerText { get; internal set; }
            public DateTime? VerRegDate { get; internal set; }
            public string TransactionAccount { get; internal set; }
            public decimal? TransactionDebetAmount { get; internal set; }
            public decimal? TransactionCreditAmount { get; internal set; }
            public int? DimensionNr { get; internal set; }
            public string ObjectNr { get; internal set; }
            public string RowType { get; set; }
        }
    }
}