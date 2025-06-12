using nSavings.Code;
using NTech;
using System;
using System.Collections.Generic;
using System.Linq;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;

namespace nSavings.DbModel.BusinessEvents
{
    public class YearlyInterestCapitalizationBusinessEventManager : BusinessEventManagerBase
    {
        public YearlyInterestCapitalizationBusinessEventManager(int userId, string informationMetadata, IClock clock) :
            base(userId, informationMetadata, clock)
        {
        }

        public void RunYearlyInterestCapitalization(bool includeCalculationDetails, out int successCount)
        {
            var firstOfCurrentYear = new DateTime(Clock.Today.Year, 1, 1);
            var toDate = new DateTime(Clock.Today.Year, 1, 1).AddDays(-1);
            successCount = 0;
            using (var context = new SavingsContext())
            {
                var eligableAccountNrs = context
                    .SavingsAccountHeaders
                    .Where(x =>
                        x.Status == SavingsAccountStatusCode.Active.ToString()
                        && !x.SavingsAccountInterestCapitalizations.Any(y => y.ToDate >= toDate)
                        && x.CreatedByEvent.TransactionDate < firstOfCurrentYear
                    )
                    .Select(x => x.SavingsAccountNr)
                    .ToArray();

                if (eligableAccountNrs.Length == 0)
                    return;

                var dc = new Lazy<DocumentClient>(() => new DocumentClient());
                var evt = AddBusinessEvent(BusinessEventType.YearlyInterestCapitalization, context);

                foreach (var accountNrGroup in eligableAccountNrs.SplitIntoGroupsOfN(500))
                {
                    var accountNrs = accountNrGroup as string[] ?? accountNrGroup.ToArray();
                    if (!TryComputeAccumulatedInterestUntilDate(context, accountNrs.ToList(), toDate,
                            includeCalculationDetails, out var result, out var failedMessage))
                    {
                        throw new Exception(failedMessage);
                    }

                    foreach (var accountNr in accountNrs)
                    {
                        var r = result[accountNr];
                        if (toDate != r.ToDate) //Sanity check since the interest calculation part is shared
                            throw new Exception(
                                $"Invalid state. Account {accountNr} got toDate={r.ToDate:yyyy-MM-dd} != {toDate:yyyy-MM-dd}");
                        var c = new SavingsAccountInterestCapitalization
                        {
                            FromDate = r.FromDate,
                            ToDate = r.ToDate,
                            SavingsAccountNr = accountNr,
                            CreatedByEvent = evt
                        };
                        context.SavingsAccountInterestCapitalizations.Add(c);
                        successCount += 1;
                        FillInInfrastructureFields(c);
                        if (includeCalculationDetails)
                        {
                            //TODO: We may need to batch this somehow. Figure out if we can (like put each group of accounts into one excel)
                            c.CalculationDetailsDocumentArchiveKey = dc.Value.CreateXlsxToArchive(
                                r.ToDocumentClientExcelRequest(),
                                $"YearlyInterestCapitalizationCalculationDetails_{accountNr}_{toDate:yyyy}.xlsx");
                        }

                        if (r.TotalInterestAmount > 0m)
                        {
                            AddTransaction(context, LedgerAccountTypeCode.Capital, r.TotalInterestAmount, evt, r.ToDate,
                                savingsAccountNr: accountNr,
                                interestFromDate: r.ToDate.AddDays(1),
                                businessEventRoleCode: "CapitalizedInterest");
                            AddTransaction(context, LedgerAccountTypeCode.CapitalizedInterest, r.TotalInterestAmount,
                                evt, r.ToDate,
                                savingsAccountNr: accountNr);
                        }

                        if (r.ShouldBeWithheldForTaxAmount > 0m)
                        {
                            AddTransaction(context, LedgerAccountTypeCode.Capital, -r.ShouldBeWithheldForTaxAmount, evt,
                                r.ToDate,
                                savingsAccountNr: accountNr,
                                interestFromDate: r.ToDate.AddDays(1),
                                businessEventRoleCode: "WithheldTax");
                            AddTransaction(context, LedgerAccountTypeCode.WithheldCapitalizedInterestTax,
                                r.ShouldBeWithheldForTaxAmount, evt, r.ToDate,
                                savingsAccountNr: accountNr);
                        }

                        AddComment(
                            $"Interest capitalized for {toDate:yyyy}. Capitalized interest={r.TotalInterestAmount.ToString("C", CommentFormattingCulture)}. Of which withheld for tax={r.ShouldBeWithheldForTaxAmount.ToString("C", CommentFormattingCulture)}",
                            BusinessEventType.YearlyInterestCapitalization, context,
                            savingsAccountNr: accountNr,
                            attachmentArchiveKeys: c.CalculationDetailsDocumentArchiveKey == null
                                ? null
                                : new List<string>() { c.CalculationDetailsDocumentArchiveKey });
                    }
                }

                context.SaveChanges();
            }
        }

        public static IDictionary<string, decimal?> ComputeAccumulatedInterestForAllAccountsOnHistoricalDate(
            SavingsContext context, DateTime historicalDate,
            Action<IDictionary<string, IList<ResultModel.DayResultModel>>> observeInterestAmountParts = null)
        {
            historicalDate = historicalDate.Date;
            var allActiveAccounts = context
                .SavingsAccountHeaders
                .Select(x => new
                {
                    x.SavingsAccountNr,
                    x.CreatedByBusinessEventId,
                    AccountCreationDate = x.CreatedByEvent.TransactionDate,
                    AccountTypeCode = x.AccountTypeCode.ToString(),
                    Status = x
                        .DatedStrings
                        .Where(y => y.TransactionDate <= historicalDate &&
                                    y.Name == DatedSavingsAccountStringCode.SavingsAccountStatus.ToString())
                        .OrderByDescending(y => y.BusinessEventId)
                        .Select(y => y.Value)
                        .FirstOrDefault(),
                    LatestInterestCapitalizationToDate = x
                        .SavingsAccountInterestCapitalizations
                        .Where(y => y.CreatedByEvent.TransactionDate <= historicalDate)
                        .OrderByDescending(y => y.ToDate)
                        .Select(y => (DateTime?)y.ToDate)
                        .FirstOrDefault(),
                    CapitalTransactions = x
                        .Transactions
                        .Where(y => y.TransactionDate <= historicalDate &&
                                    y.AccountCode == LedgerAccountTypeCode.Capital.ToString())
                        .OrderBy(y => y.InterestFromDate)
                        .ThenBy(y => y.BusinessEventId)
                        .Select(y => new { y.InterestFromDate, y.Amount })
                })
                .Where(x => x.Status ==
                            SavingsAccountStatusCode.Active
                                .ToString()); //Dont need to filter on created date since this will do that implicitly

            var interestRateChanges = ChangeInterestRateBusinessEventManager
                .GetActiveInterestRates(context)
                .Where(x => x.ValidFromDate <= historicalDate)
                .GroupBy(x => x.AccountTypeCode)
                .Select(x => new { AccountTypeCode = x.Key, InterestRateChanges = x.OrderBy(y => y.ValidFromDate) })
                .ToDictionary(
                    x => x.AccountTypeCode,
                    x => x
                        .InterestRateChanges
                        .Select(y => new InputModel.InterestRateChangeModel
                        {
                            InterestRatePercent = y.InterestRatePercent,
                            ValidFromDate = y.ValidFromDate,
                            AppliesToAccountsSinceBusinessEventId = y.AppliesToAccountsSinceBusinessEventId
                        }).ToList());

            var allNrs = allActiveAccounts.Select(x => x.SavingsAccountNr).ToArray();
            var result = new Dictionary<string, decimal?>(allNrs.Length);
            IDictionary<string, IList<ResultModel.DayResultModel>> interestAmountParts =
                observeInterestAmountParts == null ? null : new Dictionary<string, IList<ResultModel.DayResultModel>>();
            foreach (var nrGroup in allNrs.SplitIntoGroupsOfN(300))
            {
                var accounts = allActiveAccounts
                    .Where(x => nrGroup.Contains(x.SavingsAccountNr))
                    .ToList()
                    .Select(x => new InputModel
                    {
                        SavingsAccountNr = x.SavingsAccountNr,
                        CreatedByBusinessEventId = x.CreatedByBusinessEventId,
                        AccountCreationDate = x.AccountCreationDate,
                        LatestCapitalizationInterestFromDate = x.LatestInterestCapitalizationToDate?.AddDays(1),
                        OrderedCapitalTransactions = x
                            .CapitalTransactions
                            .Select(y => new InputModel.CapitalTransactionModel
                            {
                                Amount = y.Amount,
                                InterestFromDate = y.InterestFromDate
                            })
                            .ToList(),
                        OrderedInterestRateChanges = interestRateChanges[x.AccountTypeCode]
                    })
                    .ToList();

                if (!TryComputeInterestRateUntilDate(accounts, historicalDate.AddDays(-1),
                        observeInterestAmountParts != null, out var r, out var failedMessage,
                        useNullInsteadOfErrorOnMissingInterst: true))
                    throw new Exception(failedMessage);

                foreach (var rr in r)
                {
                    result[rr.Key] = rr.Value?.TotalInterestAmount;
                    if (observeInterestAmountParts != null)
                        interestAmountParts[rr.Key] = rr.Value?.InterestAmountParts;
                }
            }

            observeInterestAmountParts?.Invoke(interestAmountParts);
            return result;
        }

        public static bool TryComputeAccumulatedInterestAssumingAccountIsClosedToday(
            SavingsContext context,
            IClock clock,
            IList<string> savingsAccountNrs,
            bool includeInterestAmountParts,
            out IDictionary<string, ResultModel> result,
            out string failedMessage,
            int? maxBusinessEventId = null,
            bool skipNonActiveAccounts = false)
        {
            return TryComputeAccumulatedInterestUntilDate(context, savingsAccountNrs, clock.Today.Date.AddDays(-1),
                includeInterestAmountParts, out result, out failedMessage, maxBusinessEventId: maxBusinessEventId,
                skipNonActiveAccounts: skipNonActiveAccounts);
        }

        public static bool TryComputeAccumulatedInterestUntilDate(
            SavingsContext context,
            IList<string> savingsAccountNrs,
            DateTime toDate,
            bool includeInterestAmountParts,
            out IDictionary<string, ResultModel> result,
            out string failedMessage,
            int? maxBusinessEventId = null,
            bool skipNonActiveAccounts = false)
        {
            var capitalTransactions =
                context.LedgerAccountTransactions.Where(x => x.AccountCode == LedgerAccountTypeCode.Capital.ToString());
            var capitalizations = context.SavingsAccountInterestCapitalizations.AsQueryable();

            if (maxBusinessEventId.HasValue)
            {
                capitalTransactions = capitalTransactions.Where(x => x.BusinessEventId <= maxBusinessEventId.Value);
                capitalizations = capitalizations.Where(x => x.CreatedByBusinessEventId <= maxBusinessEventId.Value);
            }

            var accounts = context
                .SavingsAccountHeaders
                .Where(x => savingsAccountNrs.Contains(x.SavingsAccountNr))
                .Select(x => new
                {
                    x.SavingsAccountNr,
                    x.CreatedByBusinessEventId,
                    CreatedTransactionDate = x.CreatedByEvent.TransactionDate,
                    x.Status,
                    AccountTypeCode = x.AccountTypeCode.ToString(),
                    LatestInterestCapitalizationToDate = capitalizations
                        .Where(y => y.SavingsAccountNr == x.SavingsAccountNr)
                        .OrderByDescending(y => y.ToDate)
                        .Select(y => (DateTime?)y.ToDate)
                        .FirstOrDefault(),
                    CapitalTransactions = capitalTransactions
                        .Where(y => y.SavingsAccountNr == x.SavingsAccountNr)
                        .OrderBy(y => y.InterestFromDate)
                        .ThenBy(y => y.BusinessEventId)
                        .Select(y => new { y.InterestFromDate, y.Amount })
                        .AsEnumerable()
                })
                .ToList();

            if (skipNonActiveAccounts)
            {
                accounts = accounts.Where(x => x.Status == SavingsAccountStatusCode.Active.ToString()).ToList();
            }
            else
            {
                if (accounts.Any(x => x.Status != SavingsAccountStatusCode.Active.ToString()))
                {
                    failedMessage = "Interest can only be computed for active accounts";
                    result = null;
                    return false;
                }

                if (savingsAccountNrs.Except(accounts.Select(x => x.SavingsAccountNr)).Any())
                {
                    failedMessage = "savingsAccountNrs contains nrs that dont exist";
                    result = null;
                    return false;
                }
            }

            var typeCodes = accounts.Select(x => x.AccountTypeCode).Distinct().ToList();

            var interestRateChanges = ChangeInterestRateBusinessEventManager
                .GetActiveInterestRates(context)
                .Where(x => x.ValidFromDate <= toDate && typeCodes.Contains(x.AccountTypeCode))
                .GroupBy(x => x.AccountTypeCode)
                .Select(x => new { AccountTypeCode = x.Key, InterestRateChanges = x.OrderBy(y => y.ValidFromDate) })
                .ToDictionary(x => x.AccountTypeCode, x => x.InterestRateChanges);

            if (accounts.Any(x => !interestRateChanges.ContainsKey(x.AccountTypeCode)))
            {
                failedMessage = "Some accountypes have no active interest rate";
                result = null;
                return false;
            }

            var models = accounts.Select(x => new InputModel
            {
                SavingsAccountNr = x.SavingsAccountNr,
                CreatedByBusinessEventId = x.CreatedByBusinessEventId,
                AccountCreationDate = x.CreatedTransactionDate,
                LatestCapitalizationInterestFromDate = x.LatestInterestCapitalizationToDate.HasValue
                    ? new DateTime?(x.LatestInterestCapitalizationToDate.Value.AddDays(1))
                    : null,
                OrderedCapitalTransactions = x.CapitalTransactions.Select(y => new InputModel.CapitalTransactionModel
                {
                    Amount = y.Amount,
                    InterestFromDate = y.InterestFromDate
                }).ToList(),
                OrderedInterestRateChanges = interestRateChanges[x.AccountTypeCode].Select(y =>
                    new InputModel.InterestRateChangeModel
                    {
                        InterestRatePercent = y.InterestRatePercent,
                        ValidFromDate = y.ValidFromDate,
                        AppliesToAccountsSinceBusinessEventId = y.AppliesToAccountsSinceBusinessEventId
                    }).ToList()
            }).ToList();

            return TryComputeInterestRateUntilDate(models, toDate, includeInterestAmountParts, out result,
                out failedMessage);
        }

        public static bool TryComputeInterestRateUntilDate(
            IList<InputModel> accounts,
            DateTime toDate,
            bool includeInterestAmountParts,
            out IDictionary<string, ResultModel> result,
            out string failedMessage,
            bool useNullInsteadOfErrorOnMissingInterst = false)
        {
            var localResult = new Dictionary<string, ResultModel>();
            foreach (var a in accounts)
            {
                if (!TryComputeInterestRateUntilDateForSingleAccount(toDate, a, includeInterestAmountParts,
                        useNullInsteadOfErrorOnMissingInterst, out failedMessage, out var r))
                {
                    result = null;
                    return false;
                }

                localResult[a.SavingsAccountNr] = r;
            }

            result = localResult;
            failedMessage = null;
            return true;
        }

        private static bool TryComputeInterestRateUntilDateForSingleAccount(DateTime toDate, InputModel a,
            bool includeInterestAmountParts, bool useNullInsteadOfErrorOnMissingInterst, out string failedMessage,
            out ResultModel result)
        {
            var r = new ResultModel
            {
                Input = a,
                ToDate = toDate,
                InterestAmountParts = includeInterestAmountParts ? new List<ResultModel.DayResultModel>() : null,
                TotalInterestAmount = 0m
            };
            var firstTransactionDate =
                a.OrderedCapitalTransactions.Select(x => (DateTime?)x.InterestFromDate).FirstOrDefault();
            var firstDate = DateTimeUtilities.Max(firstTransactionDate, a.LatestCapitalizationInterestFromDate);
            if (!firstDate.HasValue)
            {
                r.FromDate = a.AccountCreationDate;
                if (r.ToDate < r.FromDate)
                    r.ToDate = r.FromDate;
                failedMessage = null;
                result = r;
                return true;
            }

            if (!a.CreatedByBusinessEventId.HasValue)
                throw new Exception("Missing created by business event id");

            var d = firstDate.Value;
            var totalInterestAmount = 0m;
            var capitalBalanceCache = RunningTotalCache.Create(r.Input.OrderedCapitalTransactions,
                x => x.InterestFromDate, x => x.Amount, true);
            var interestCache = LatestItemCache.Create<InputModel.InterestRateChangeModel, decimal>(
                r.Input.OrderedInterestRateChanges.Where(x =>
                    !x.AppliesToAccountsSinceBusinessEventId.HasValue ||
                    x.AppliesToAccountsSinceBusinessEventId.Value <= a.CreatedByBusinessEventId.Value),
                x => x.ValidFromDate, x => x.InterestRatePercent, true);

            r.FromDate = d;
            while (d <= toDate)
            {
                var capitalBalance = capitalBalanceCache.GetRunningTotal(d);

                var interestRatePercent = interestCache.GetCurrentValue(d);
                if (!interestRatePercent.HasValue && capitalBalance > 0m)
                {
                    if (useNullInsteadOfErrorOnMissingInterst)
                    {
                        failedMessage = null;
                        result = null;
                        return true;
                    }

                    failedMessage =
                        $"Account {a.SavingsAccountNr} has capital balance for {d:yyyy-MM-dd} but no interest is defined for that date";
                    result = null;
                    return false;
                }

                var isLeapYear = DateTime.IsLeapYear(d.Year);

                var dailyInterestAmount = interestRatePercent.HasValue && capitalBalance > 0m
                    ? interestRatePercent.Value / 100m / (isLeapYear ? 366m : 365m) * capitalBalance
                    : 0m;

                totalInterestAmount += dailyInterestAmount;
                if (includeInterestAmountParts)
                {
                    r.InterestAmountParts.Add(new ResultModel.DayResultModel
                    {
                        Date = d,
                        AccountBalance = capitalBalance,
                        AccountInterestRatePercent = interestRatePercent,
                        Amount = dailyInterestAmount,
                        IsLeapYear = isLeapYear
                    });
                }

                d = d.AddDays(1);
            }

            r.TotalInterestAmount = Math.Round(totalInterestAmount, 2);
            r.ShouldBeWithheldForTaxAmount = Math.Round(r.TotalInterestAmount * 0.3m, 2);

            failedMessage = null;
            result = r;
            return true;
        }

        public class InputModel
        {
            public string SavingsAccountNr { get; set; }
            public int? CreatedByBusinessEventId { get; set; }
            public DateTime AccountCreationDate { get; set; }
            public DateTime? LatestCapitalizationInterestFromDate { get; set; }
            public List<CapitalTransactionModel> OrderedCapitalTransactions { get; set; }

            public class CapitalTransactionModel
            {
                public decimal Amount { get; set; }
                public DateTime InterestFromDate { get; set; }
            }

            public List<InterestRateChangeModel> OrderedInterestRateChanges { get; set; }

            public class InterestRateChangeModel
            {
                public decimal InterestRatePercent { get; set; }
                public DateTime ValidFromDate { get; set; }
                public int? AppliesToAccountsSinceBusinessEventId { get; set; }
            }
        }

        public class ResultModel
        {
            public InputModel Input { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public decimal TotalInterestAmount { get; set; }
            public decimal ShouldBeWithheldForTaxAmount { get; set; }
            public IList<DayResultModel> InterestAmountParts { get; set; }

            public class DayResultModel
            {
                public decimal Amount { get; set; }
                public decimal AccountBalance { get; set; }
                public decimal? AccountInterestRatePercent { get; set; }
                public bool IsLeapYear { get; set; }
                public DateTime Date { get; set; }

                public bool CanBeCoalescedWith(DayResultModel d)
                {
                    if (Amount != d.Amount)
                        return false;
                    if (AccountBalance != d.AccountBalance)
                        return false;
                    if (IsLeapYear != d.IsLeapYear)
                        return false;
                    if (AccountInterestRatePercent != d.AccountInterestRatePercent)
                        return false;
                    return true;
                }
            }

            public bool IsCapitalizationNeeded()
            {
                //The last check is iffy. Do we really need to store a zero item here just to track all the accounts days of life?
                return (TotalInterestAmount > 0m || ShouldBeWithheldForTaxAmount > 0m || ToDate > FromDate);
            }

            public IList<IList<DayResultModel>> SummarizeParts()
            {
                var items = new List<IList<DayResultModel>>();
                if (InterestAmountParts.Count == 0)
                    return items;
                var currentList = new List<DayResultModel>();
                DayResultModel lastItem = null;
                foreach (var i in InterestAmountParts)
                {
                    if (lastItem == null || !lastItem.CanBeCoalescedWith(i))
                    {
                        if (currentList.Count > 0)
                            items.Add(currentList);
                        currentList = new List<DayResultModel>();
                    }

                    currentList.Add(i);
                    lastItem = i;
                }

                if (currentList.Count > 0)
                    items.Add(currentList);
                return items;
            }

            public DocumentClientExcelRequest ToDocumentClientExcelRequest()
            {
                var r = this;
                var er = new DocumentClientExcelRequest
                {
                    Sheets = new[]
                    {
                        new DocumentClientExcelRequest.Sheet
                        {
                            Title = $"{r.Input.SavingsAccountNr} Summarized",
                            AutoSizeColumns = true
                        },
                        new DocumentClientExcelRequest.Sheet
                        {
                            Title = "Raw",
                            AutoSizeColumns = true
                        }
                    }
                };

                var summaries = r
                    .SummarizeParts()
                    .Select(x => new
                    {
                        FromDate = x.Min(y => y.Date),
                        ToDate = x.Max(y => y.Date),
                        CountDays = x.Count(),
                        Balance = x.First().AccountBalance,
                        AccountInterestRatePercent = x.First().AccountInterestRatePercent,
                        InterestAmount = x.First().Amount,
                        IsLeapYear = x.First().IsLeapYear
                    })
                    .ToList();

                er.Sheets[0].SetColumnsAndData(summaries,
                    summaries.Col(x => x.FromDate, ExcelType.Date, "From date"),
                    summaries.Col(x => x.ToDate, ExcelType.Date, "To date"),
                    summaries.Col(x => x.CountDays, ExcelType.Number, "# days", nrOfDecimals: 0, includeSum: true),
                    summaries.Col(x => x.Balance, ExcelType.Number, "Balance", nrOfDecimals: 2, includeSum: false),
                    summaries.Col(x => x.AccountInterestRatePercent / 100m, ExcelType.Percent, "Interest rate"),
                    summaries.Col(x => x.InterestAmount, ExcelType.Number, "Daily Interest amount", nrOfDecimals: 6,
                        includeSum: false),
                    summaries.Col(x => x.InterestAmount * x.CountDays, ExcelType.Number, "Period Interest amount",
                        nrOfDecimals: 6, includeSum: true),
                    summaries.Col(x => x.IsLeapYear ? "X" : "", ExcelType.Text, "Leap year"));

                var interestAmountParts = r.InterestAmountParts;
                er.Sheets[1].SetColumnsAndData(r.InterestAmountParts,
                    interestAmountParts.Col(x => x.Date, ExcelType.Date, "Date"),
                    interestAmountParts.Col(x => x.AccountBalance, ExcelType.Number, "Balance", nrOfDecimals: 2,
                        includeSum: false),
                    interestAmountParts.Col(x => x.AccountInterestRatePercent / 100m, ExcelType.Percent,
                        "Interest rate"),
                    interestAmountParts.Col(x => x.Amount, ExcelType.Number, "Interest amount", nrOfDecimals: 6,
                        includeSum: true),
                    interestAmountParts.Col(x => x.IsLeapYear ? "X" : "", ExcelType.Text, "Leap year"));

                return er;
            }
        }
    }
}