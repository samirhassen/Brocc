using nSavings.DbModel.BusinessEvents;
using nSavings.DbModel.Repository;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nSavings.Code.Treasury
{
    public class TreasuryDomainModel
    {
        public List<AccountModel> Accounts { get; set; }
        private int currentUserId;
        private string informationMetadata;

        private TreasuryDomainModel()
        {
        }

        public static TreasuryDomainModel GetChangesSinceLastExport(int currentUserId, string informationMetadata, NTech.IClock clock)
        {
            var d = new TreasuryDomainModel();
            d.currentUserId = currentUserId;
            d.informationMetadata = informationMetadata;
            var repo = new SystemItemRepository(currentUserId, informationMetadata);
            using (var context = new SavingsContext())
            {
                d.Accounts = GetAccounts(
                    context, clock, currentUserId, informationMetadata);
            }

            return d;
        }

        public static List<AccountModel> GetAccounts(SavingsContext context, NTech.IClock clock, int currentUserId, string informationMetadata)
        {
            var q = context
               .SavingsAccountHeaders
               .AsNoTracking()
               .AsQueryable();

            var rates = ChangeInterestRateBusinessEventManager.GetPerAccountActiveInterestRates(context);

            var withdrawalCodes = new List<string> { BusinessEventType.AccountClosure.ToString(), BusinessEventType.Withdrawal.ToString() };
            var r = q.Where(n => n.Status == SavingsAccountStatusCode.Active.ToString()).Select(x => new
            {
                x.SavingsAccountNr,
                x.MainCustomerId,
                StartDate = x.CreatedByEvent.TransactionDate,
                CapitalBalanceTransactions = (x
                    .Transactions
                    .Where(y => !withdrawalCodes.Contains(y.BusinessEvent.EventType) && y.AccountCode == LedgerAccountTypeCode.Capital.ToString())
                    .Sum(y => (decimal?)y.Amount) ?? 0m) + (x
                    .Transactions
                    .Where(y => y.BusinessEvent.EventType == BusinessEventType.OutgoingPaymentFileExport.ToString() && y.AccountCode == LedgerAccountTypeCode.ShouldBePaidToCustomer.ToString())
                    .Sum(y => (decimal?)y.Amount) ?? 0m),
                InterestRatePercent = rates
                            .Where(y => y.SavingsAccountNr == x.SavingsAccountNr && y.ValidFromDate <= clock.Today)
                            .OrderByDescending(y => y.ValidFromDate)
                            .Select(y => (decimal?)y.InterestRatePercent)
                            .FirstOrDefault()
            }).ToList();

            //Get customerinfo
            var client = new CustomerClient();
            var result = client.BulkFetchPropertiesByCustomerIds(new HashSet<int>(r.Select(x => x.MainCustomerId).Distinct()), "firstName", "lastName", "addressCountry");

            Func<string, IDictionary<int, CustomerClient.GetPropertyCustomer>, int, string> getValue = (n, h, cid) =>
               h[cid].Properties.SingleOrDefault(x => x.Name.Equals(n, StringComparison.OrdinalIgnoreCase))?.Value;

            return r.Where(n => n.CapitalBalanceTransactions > 0).Select(x => new AccountModel
            {
                CustomerId = x.MainCustomerId,
                SavingsAccountNr = x.SavingsAccountNr,
                StartDate = x.StartDate,
                CurrentInterestRate = x.InterestRatePercent ?? 0m,
                CurrentBalance = x.CapitalBalanceTransactions,
                CustomerFullName = $"{getValue("firstName", result, x.MainCustomerId)} {getValue("lastName", result, x.MainCustomerId)}",
                CustomerCountry = getValue("addressCountry", result, x.MainCustomerId)
            }).ToList();
        }

        public class AccountModel
        {
            public int CustomerId { get; set; }
            public string SavingsAccountNr { get; set; }
            public DateTimeOffset StartDate { get; set; }
            public decimal CurrentInterestRate { get; set; }
            public decimal CurrentBalance { get; set; }
            public string CustomerFullName { get; set; }
            public string CustomerCountry { get; set; }
        }

        private class ExtraCreditData
        {
            public byte[] Ts { get; set; }
            public DateTime CreationDate { get; set; }
            public decimal? CapitalDebt { get; set; }
            public decimal? NotNotifiedCapitalAmount { get; set; }
            public IEnumerable<DateTime> PendingFuturePaymentFreeMonths { get; set; }
        }
    }
}