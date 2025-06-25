using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using nSavings.Code;
using NTech;
using NTech.Core.Savings.Shared.Database;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFixed;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;

namespace nSavings.DbModel.BusinessEvents;

public class InterestCapitalizationBusinessEventManager(
    in int userId,
    in string informationMetadata,
    in IClock clock,
    IDocumentClient docCli = null,
    in ISavingsContext ctx = null)
    : BusinessEventManagerBase(userId, informationMetadata, clock)
{
    private readonly ISavingsContext context = ctx ?? new SavingsContext();
    private readonly Lazy<IDocumentClient> documentClient = new(() => docCli ?? new DocumentClient());

    public async Task RunInterestCapitalizationForAccountAsync(string savingsAccountNr,
        DateTime to, BusinessEvent evt = null, bool includeCalculationDetails = false)
    {
        evt ??= AddBusinessEvent(BusinessEventType.ManualAccountCapitalization, context);

        var count = await context.SavingsAccountHeadersQueryable
            .Where(a =>
                a.SavingsAccountNr == savingsAccountNr &&
                a.Status == nameof(SavingsAccountStatusCode.Active) &&
                !a.SavingsAccountInterestCapitalizations.Any(y => y.ToDate >= to))
            .CountAsync();

        if (count == 0)
            throw new ArgumentException($"Account {savingsAccountNr} not eligible for capitalization",
                nameof(savingsAccountNr));

        CapitalizeInterestBatch(evt, includeCalculationDetails, [savingsAccountNr], to);

        await context.SaveChangesAsync();
    }

    public int RunInterestCapitalizationAllAccounts(bool includeCalculationDetails,
        SavingsAccountTypeCode? accountTypeFilter)
    {
        var firstThisMonth = new DateTime(Clock.Today.Year, Clock.Today.Month, 1);
        var from = firstThisMonth.AddMonths(-1);
        var to = firstThisMonth.AddDays(-1);

        var eligibleAccountNrs = context
            .SavingsAccountHeadersQueryable
            .Where(x =>
                x.Status == nameof(SavingsAccountStatusCode.Active)
                && (accountTypeFilter == null || x.AccountTypeCode == accountTypeFilter.ToString())
                && !x.SavingsAccountInterestCapitalizations.Any(y => y.ToDate >= to)
                && x.CreatedByEvent.TransactionDate < from
            )
            .Select(x => x.SavingsAccountNr)
            .ToArray();

        if (eligibleAccountNrs.Length == 0)
            return 0;

        var evt = AddBusinessEvent(BusinessEventType.MonthlyInterestCapitalization, context);

        var successCount = eligibleAccountNrs.SplitIntoGroupsOfN(500)
            .Sum(accountNrGroup =>
                CapitalizeInterestBatch(evt, includeCalculationDetails, accountNrGroup, to));

        context.SaveChanges();
        return successCount;
    }

    private int CapitalizeInterestBatch(BusinessEvent evt, bool includeCalculationDetails,
        IEnumerable<string> accountBatch, DateTime to)
    {
        var batch = accountBatch as string[] ?? accountBatch.ToArray();
        if (!TryComputeAccumulatedInterestUntilDate(context, batch.ToList(), to,
                includeCalculationDetails, out var result, out var failedMessage))
        {
            throw new Exception(failedMessage);
        }

        var successCount = 0;
        foreach (var accountNr in batch)
        {
            var r = result[accountNr];
            if (to != r.ToDate) //Sanity check since the interest calculation part is shared
                throw new Exception(
                    $"Invalid state. Account {accountNr} got toDate={r.ToDate:yyyy-MM-dd} != {to:yyyy-MM-dd}");

            var c = new SavingsAccountInterestCapitalization
            {
                FromDate = r.FromDate,
                ToDate = r.ToDate,
                SavingsAccountNr = accountNr,
                CreatedByEvent = evt
            };

            //context.SavingsAccountInterestCapitalizationsQueryable.Add(c);
            context.AddSavingsAccountInterestCapitalizations(c);
            successCount += 1;
            FillInInfrastructureFields(c);
            if (includeCalculationDetails)
            {
                //TODO: We may need to batch this somehow. Figure out if we can (like put each group of accounts into one excel)
                c.CalculationDetailsDocumentArchiveKey = documentClient.Value.CreateXlsxToArchive(
                    r.ToDocumentClientExcelRequest(),
                    $"MonthlyInterestCapitalizationCalculationDetails_{accountNr}_{to:yyyy-MM}.xlsx");
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
                $"Interest capitalized for {to:yyyy-MM}. Capitalized interest={r.TotalInterestAmount.ToString("C", CommentFormattingCulture)}. Of which withheld for tax={r.ShouldBeWithheldForTaxAmount.ToString("C", CommentFormattingCulture)}",
                (BusinessEventType)Enum.Parse(typeof(BusinessEventType), evt.EventType), context,
                savingsAccountNr: accountNr,
                attachmentArchiveKeys: c.CalculationDetailsDocumentArchiveKey == null
                    ? null
                    : [c.CalculationDetailsDocumentArchiveKey]);
        }

        return successCount;
    }

    private static bool TryComputeAccumulatedInterestUntilDate(
        ISavingsContext context,
        IList<string> savingsAccountNrs,
        DateTime toDate,
        bool includeInterestAmountParts,
        out IDictionary<string, ResultModel> result,
        out string failedMessage)
    {
        var capitalTransactions =
            context.LedgerAccountTransactionsQueryable.Where(x =>
                x.AccountCode == nameof(LedgerAccountTypeCode.Capital));
        var capitalizations = context.SavingsAccountInterestCapitalizationsQueryable.AsQueryable();

        var accounts = context
            .SavingsAccountHeadersQueryable
            .Where(x => savingsAccountNrs.Contains(x.SavingsAccountNr))
            .Select(x => new
            {
                x.SavingsAccountNr,
                x.CreatedByBusinessEventId,
                CreatedTransactionDate = x.CreatedByEvent.TransactionDate,
                x.Status,
                AccountTypeCode = x.AccountTypeCode.ToString(),
                x.FixedInterestProduct,
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


        var inactiveAccounts = accounts
            .Where(a => a.Status != nameof(SavingsAccountStatusCode.Active))
            .Select(a => a.SavingsAccountNr)
            .ToList();
        if (inactiveAccounts.Any())
        {
            failedMessage =
                $"Interest can only be calculated for active accounts. Tried calculating interest for inactive accounts: [{string.Join(", ", inactiveAccounts)}]";
            result = null;
            return false;
        }

        var missingAccounts = savingsAccountNrs.Except(accounts.Select(x => x.SavingsAccountNr)).ToList();
        if (missingAccounts.Any())
        {
            failedMessage =
                $"Tried calculating interest for accounts that don't exist: [{string.Join(", ", missingAccounts)}]";
            result = null;
            return false;
        }

        var typeCodes = accounts.Select(a => a.AccountTypeCode).ToHashSet();

        var interestRateChanges = ChangeInterestRateBusinessEventManager
            .GetActiveInterestRates(context)
            .Where(x => x.ValidFromDate <= toDate && typeCodes.Contains(x.AccountTypeCode))
            .GroupBy(x => x.AccountTypeCode)
            .Select(x => new { AccountTypeCode = x.Key, InterestRateChanges = x.OrderBy(y => y.ValidFromDate) })
            .ToDictionary(x => x.AccountTypeCode, x => x.InterestRateChanges);

        var fixedRates = FixedAccountProductBusinessEventManager.GetActiveFixedRatesQueryable(context)
            .Include(r => r.CreatedAtBusinessEvent);

        var missingRates = accounts.Where(a =>
                !interestRateChanges.ContainsKey(a.AccountTypeCode) &&
                fixedRates.All(r => r.Id != a.FixedInterestProduct))
            .Select(a => a.SavingsAccountNr)
            .ToList();
        if (missingRates.Any())
        {
            failedMessage = $"Some accounts do not have an active interest rate: [{string.Join(", ", missingRates)}]";
            result = null;
            return false;
        }

        var models = accounts.Select(a =>
        {
            return new InputModel
            {
                SavingsAccountNr = a.SavingsAccountNr,
                CreatedByBusinessEventId = a.CreatedByBusinessEventId,
                AccountCreationDate = a.CreatedTransactionDate,
                LatestCapitalizationInterestFromDate = a.LatestInterestCapitalizationToDate.HasValue
                    ? new DateTime?(a.LatestInterestCapitalizationToDate.Value.AddDays(1))
                    : null,
                OrderedCapitalTransactions = a.CapitalTransactions.Select(y => new InputModel.CapitalTransactionModel
                {
                    Amount = y.Amount,
                    InterestFromDate = y.InterestFromDate
                }).ToList(),
                OrderedInterestRateChanges = a.AccountTypeCode == nameof(SavingsAccountTypeCode.FixedInterestAccount)
                    ? [MapToRateChange(fixedRates.Single(r => r.Id == a.FixedInterestProduct))]
                    : interestRateChanges[a.AccountTypeCode].Select(y =>
                        new InputModel.InterestRateChangeModel
                        {
                            InterestRatePercent = y.InterestRatePercent,
                            ValidFromDate = y.ValidFromDate,
                            AppliesToAccountsSinceBusinessEventId = y.AppliesToAccountsSinceBusinessEventId
                        }).ToList()
            };
        }).ToList();

        return TryComputeInterestRateUntilDate(models, toDate, includeInterestAmountParts, out result,
            out failedMessage);
    }

    private static InputModel.InterestRateChangeModel MapToRateChange(FixedAccountProduct product)
    {
        return new InputModel.InterestRateChangeModel
        {
            InterestRatePercent = product.InterestRatePercent,
            ValidFromDate = product.ValidFrom,
            AppliesToAccountsSinceBusinessEventId = product.CreatedAtBusinessEvent.Id
        };
    }

    private static bool TryComputeInterestRateUntilDate(
        IList<InputModel> accounts,
        DateTime toDate,
        bool includeInterestAmountParts,
        out IDictionary<string, ResultModel> result,
        out string failedMessage)
    {
        var localResult = new Dictionary<string, ResultModel>();
        foreach (var account in accounts)
        {
            if (!TryComputeInterestRateUntilDateForSingleAccount(toDate, account, includeInterestAmountParts,
                    out failedMessage, out var r))
            {
                result = null;
                return false;
            }

            localResult[account.SavingsAccountNr] = r;
        }

        result = localResult;
        failedMessage = null;
        return true;
    }

    private static bool TryComputeInterestRateUntilDateForSingleAccount(DateTime toDate, InputModel a,
        bool includeInterestAmountParts, out string failedMessage,
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
                return AccountInterestRatePercent == d.AccountInterestRatePercent;
            }
        }

        public bool IsCapitalizationNeeded()
        {
            //The last check is iffy. Do we really need to store a zero item here just to track all the accounts days of life?
            return (TotalInterestAmount > 0m || ShouldBeWithheldForTaxAmount > 0m || ToDate > FromDate);
        }

        private IList<IList<DayResultModel>> SummarizeParts()
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
                    currentList = [];
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
                Sheets =
                [
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
                ]
            };

            var summaries = r
                .SummarizeParts()
                .Select(x => new
                {
                    FromDate = x.Min(y => y.Date),
                    ToDate = x.Max(y => y.Date),
                    CountDays = x.Count,
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