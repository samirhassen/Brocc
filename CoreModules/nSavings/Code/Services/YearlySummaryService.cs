using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NTech;
using NTech.Banking.BankAccounts.Fi;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace nSavings.Code.Services
{
    public class YearlySummaryService : IYearlySummaryService
    {
        private readonly Func<SavingsContext> createContext;
        private readonly Func<CustomerClient> createCustomerClient;
        private readonly IClock clock;
        private readonly Func<DocumentClient> createDocumentClient;
        private readonly CultureInfo formattingCulture;

        public YearlySummaryService(Func<SavingsContext> createContext, Func<CustomerClient> createCustomerClient, Func<DocumentClient> createDocumentClient, IClock clock, CultureInfo formattingCulture)
        {
            this.createContext = createContext;
            this.createCustomerClient = createCustomerClient;
            this.clock = clock;
            this.createDocumentClient = createDocumentClient;
            this.formattingCulture = formattingCulture;
        }

        public class TransactionModel
        {
            public int BusinessEventId { get; set; }
            public string BusinessEventTypeCode { get; set; }
            public string AccountTypeCode { get; set; }
            public decimal Amount { get; set; }
            public DateTime BookKeepingDate { get; set; }
        }

        public class EventModelBase
        {
            public int BusinessEventId { get; set; }
            public string BusinessEventTypeCode { get; set; }
            public DateTime TransactionDate { get; set; }
        }

        public class EventModel : EventModelBase
        {
            public List<TransactionModel> Transactions { get; set; }
        }

        private class TmpEventModel : EventModelBase
        {
            public int MainCustomerId { get; set; }
            public string SavingsAccountNr { get; set; }
            public IEnumerable<TransactionModel> Transactions { get; set; }

            public EventModel ToEventModel()
            {
                return new EventModel
                {
                    BusinessEventId = this.BusinessEventId,
                    BusinessEventTypeCode = this.BusinessEventTypeCode,
                    TransactionDate = this.TransactionDate,
                    Transactions = this.Transactions.ToList()
                };
            }
        }

        public class SummaryDataModel
        {
            public decimal BalanceAfterAmount { get; set; }
            public decimal TotalInterestAmount { get; set; }
            public decimal WithheldTaxAmount { get; set; }
        }

        public SummaryDataModel ComputeSummary(List<EventModel> events, int year)
        {
            var allTransactions = events.SelectMany(x => x.Transactions).ToList();

            var nextYear = year + 1;
            var capitalizationEventId = events
                .Where(x => (x.BusinessEventTypeCode == BusinessEventType.AccountClosure.ToString() && x.TransactionDate.Year == year) || (x.BusinessEventTypeCode == BusinessEventType.YearlyInterestCapitalization.ToString() && x.TransactionDate.Year == nextYear))
                .OrderByDescending(x => x.BusinessEventId)
                .Select(x => (int?)x.BusinessEventId)
                .FirstOrDefault();

            if (!capitalizationEventId.HasValue)
                return null;

            return new SummaryDataModel
            {
                BalanceAfterAmount = allTransactions
                    .Where(x => x.BusinessEventId <= capitalizationEventId.Value && x.AccountTypeCode == LedgerAccountTypeCode.Capital.ToString()).Sum(x => (decimal?)x.Amount) ?? 0m,
                TotalInterestAmount = allTransactions.Where(x => x.BusinessEventId <= capitalizationEventId.Value && x.BookKeepingDate.Year == year && x.AccountTypeCode == LedgerAccountTypeCode.CapitalizedInterest.ToString()).Sum(x => (decimal?)x.Amount) ?? 0m,
                WithheldTaxAmount = allTransactions.Where(x => x.BusinessEventId <= capitalizationEventId.Value && x.BookKeepingDate.Year == year && x.AccountTypeCode == LedgerAccountTypeCode.WithheldCapitalizedInterestTax.ToString()).Sum(x => (decimal?)x.Amount) ?? 0m,
            };
        }

        public List<int> GetAllYearsWithSummaries(List<EventModel> events)
        {
            return events
                .Select(x => new
                {
                    CloseYear = x.BusinessEventTypeCode == BusinessEventType.AccountClosure.ToString() ? new int?(x.TransactionDate.Year) : null,
                    CapYear = x.BusinessEventTypeCode == BusinessEventType.YearlyInterestCapitalization.ToString() ? new int?(x.TransactionDate.Year - 1) : null
                })
                .Select(x => x.CloseYear ?? x.CapYear)
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .Distinct()
                .OrderByDescending(x => x)
                .ToList()
                .Where(x => x < clock.Today.Year) //Closed accounts are not visible as yearly summaries until the following year to mirror other accounts
                .ToList();
        }

        private IQueryable<TmpEventModel> GetEventsModel(SavingsContext context)
        {
            return context
                .SavingsAccountHeaders
                .Select(x => new
                {
                    x.SavingsAccountNr,
                    x.MainCustomerId,
                    CapEventIds = x.SavingsAccountInterestCapitalizations.Select(y => y.CreatedByBusinessEventId),
                    TrEventIds = x.Transactions.Select(y => y.BusinessEventId),
                })
                .Select(x => new
                {
                    x.SavingsAccountNr,
                    x.MainCustomerId,
                    EventIds = x.CapEventIds.Union(x.TrEventIds)
                })
                .SelectMany(x => context
                    .BusinessEvents
                    .Where(y => x.EventIds.Contains(y.Id))
                    .Select(y => new TmpEventModel
                    {
                        SavingsAccountNr = x.SavingsAccountNr,
                        MainCustomerId = x.MainCustomerId,
                        BusinessEventId = y.Id,
                        BusinessEventTypeCode = y.EventType,
                        TransactionDate = y.TransactionDate,
                        Transactions = y.CreatedLedgerTransactions.Where(z => z.SavingsAccountNr == x.SavingsAccountNr).Select(z => new TransactionModel
                        {
                            AccountTypeCode = z.AccountCode,
                            Amount = z.Amount,
                            BookKeepingDate = z.BookKeepingDate,
                            BusinessEventId = z.BusinessEventId,
                            BusinessEventTypeCode = y.EventType,
                        })
                    })
                );
        }

        public List<int> GetAllYearsWithSummaries(string savingsAccountNr)
        {
            using (var context = createContext())
            {
                var events = GetEventsModel(context).Where(x => x.SavingsAccountNr == savingsAccountNr).ToList().Select(x => x.ToEventModel()).ToList();
                return GetAllYearsWithSummaries(events);
            }
        }

        public Dictionary<string, List<int>> GetAllYearsWithSummariesForCustomerAccounts(int customerId)
        {
            var result = new Dictionary<string, List<int>>();
            using (var context = createContext())
            {
                var eventsByAccount = GetEventsModel(context)
                    .Where(x => x.MainCustomerId == customerId)
                    .ToList()
                    .GroupBy(x => x.SavingsAccountNr)
                    .ToList();
                foreach (var g in eventsByAccount)
                {
                    var savingsAccountNr = g.Key;
                    var events = g.Select(x => x.ToEventModel()).ToList();
                    result[savingsAccountNr] = GetAllYearsWithSummaries(events);
                }
            }
            return result;
        }

        private PdfRenderContext CreateRenderContext(string savingsAccountNr, int year, int? forcedCustomerId)
        {
            SummaryDataModel summaryData;
            decimal? endOfYearInterestRate;
            string iban;
            int customerId;
            using (var context = createContext())
            {
                var events = GetEventsModel(context)
                    .Where(x => x.SavingsAccountNr == savingsAccountNr)
                    .ToList();
                if (forcedCustomerId.HasValue)
                    events = events.Where(x => x.MainCustomerId == forcedCustomerId.Value).ToList();
                if (events.Count == 0)
                    return null;

                customerId = events.First().MainCustomerId;

                endOfYearInterestRate = DbModel.BusinessEvents.ChangeInterestRateBusinessEventManager
                    .GetPerAccountActiveInterestRates(context)
                    .Where(x => x.TransactionDate.Year <= year && x.SavingsAccountNr == savingsAccountNr)
                    .OrderByDescending(x => x.Id)
                    .Select(x => (decimal?)x.InterestRatePercent)
                    .FirstOrDefault();

                //NOTE: These this makes no sense but the client wants this to be end of year even on closed accounts so not tied to the business event.
                iban = context
                    .DatedSavingsAccountStrings
                    .Where(y => y.SavingsAccountNr == savingsAccountNr && y.Name == DatedSavingsAccountStringCode.WithdrawalIban.ToString() && y.TransactionDate.Year <= year)
                    .OrderByDescending(y => y.BusinessEventId).Select(y => y.Value)
                    .FirstOrDefault();

                summaryData = ComputeSummary(events.Select(x => x.ToEventModel()).ToList(), year);
                if (summaryData == null)
                    return null;
            }

            var cc = createCustomerClient();
            var contactInfo = cc.FetchCustomerContactInfo(customerId, true, true);

            var f = formattingCulture;
            IBANFi b;

            return new PdfRenderContext
            {
                printDate = clock.Now.ToString("d", f),
                year = year.ToString(f),
                interestRatePercent = (endOfYearInterestRate / 100m)?.ToString("P", f),
                addressStreet = contactInfo?.addressStreet,
                addressZipAndCity = $"{contactInfo?.addressZipcode} {contactInfo?.addressCity}",
                civicRegNr = contactInfo?.civicRegNr,
                fullName = $"{contactInfo?.firstName} {contactInfo?.lastName}",
                savingsAccountNr = savingsAccountNr,
                summaryDate = new DateTime(year + 1, 1, 1).AddDays(-1).ToString("d", f),
                withdrawalIban = IBANFi.TryParse(iban, out b) ? b?.GroupsOfFourValue : iban,
                balanceAmount = (summaryData?.BalanceAfterAmount)?.ToString("C", f),
                interestAmount = (summaryData?.TotalInterestAmount)?.ToString("C", f),
                withheldTaxAmount = (summaryData?.WithheldTaxAmount)?.ToString("C", f)
            };
        }

        public Stream CreateSummaryPdf(string savingsAccountNr, int year)
        {
            return CreateSummaryPdfWithOwnerCheck(savingsAccountNr, year, null);
        }

        public Stream CreateSummaryPdfWithOwnerCheck(string savingsAccountNr, int year, int? forcedCustomerId)
        {
            var renderContext = CreateRenderContext(savingsAccountNr, year, forcedCustomerId);
            if (renderContext == null)
                return null;
            var m = JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(renderContext), new ExpandoObjectConverter()) as IDictionary<string, object>;
            var dc = createDocumentClient();
            return dc.PdfRenderDirect("savings-yearlysummary", m);
        }

        private class PdfRenderContext
        {
            public string year { get; set; }
            public string printDate { get; set; }
            public string fullName { get; set; }
            public string civicRegNr { get; set; }
            public string addressStreet { get; set; }
            public string addressZipAndCity { get; set; }
            public string savingsAccountNr { get; set; }
            public string withdrawalIban { get; set; } //grouped
            public string interestRatePercent { get; set; } //
            public string summaryDate { get; set; } //last day of year currently even on close
            public string balanceAmount { get; set; }
            public string interestAmount { get; set; }
            public string withheldTaxAmount { get; set; }
        }
    }

    public interface IYearlySummaryService
    {
        List<int> GetAllYearsWithSummaries(string savingsAccountNr);
        Dictionary<string, List<int>> GetAllYearsWithSummariesForCustomerAccounts(int customerId);
        Stream CreateSummaryPdf(string savingsAccountNr, int year);
        Stream CreateSummaryPdfWithOwnerCheck(string savingsAccountNr, int year, int? forcedCustomerId);
    }
}