using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;

namespace nSavings.Code.Services
{
    public class SavingsCustomerSearchSourceService : ICustomerSearchSourceService
    {
        private readonly SavingsAccountSearchService accountSearchService;

        public SavingsCustomerSearchSourceService(SavingsAccountSearchService accountSearchService)
        {
            this.accountSearchService = accountSearchService;
        }

        public ISet<int> FindCustomers(string searchQuery) => accountSearchService
            .Search(new SavingsAccountSearchRequest { OmnisearchValue = searchQuery, SkipCustomerSearch = true })
            .Select(x => x.MainCustomerId)
            .ToHashSetShared();

        public List<CustomerSearchEntity> GetCustomerEntities(int customerId)
        {
            using (var context = new DbModel.SavingsContext())
            {
                return context
                    .SavingsAccountHeaders
                    .Where(x => x.MainCustomerId == customerId)
                    .Select(x => new
                    {
                        x.SavingsAccountNr,
                        x.MainCustomerId,
                        x.AccountTypeCode,
                        x.Status,
                        CreationDate = x.CreatedByEvent.TransactionDate,
                        CurrentBalance = x.Transactions.Where(y => y.AccountCode == LedgerAccountTypeCode.Capital.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                        LatestStatusItem = x.DatedStrings.Where(y => y.Name == DatedSavingsAccountStringCode.SavingsAccountStatus.ToString()).OrderByDescending(y => y.BusinessEventId).FirstOrDefault()
                    })
                    .ToList()
                    .Select(x => new CustomerSearchEntity
                    {
                        Source = "nSavings",
                        EntityId = x.SavingsAccountNr,
                        CreationDate = x.CreationDate,
                        CurrentBalance = x.CurrentBalance,
                        IsActive = x.Status == SavingsAccountStatusCode.Active.ToString(),
                        Customers = new List<CustomerSearchEntityCustomer>
                        {
                            new CustomerSearchEntityCustomer { CustomerId = x.MainCustomerId, Roles = new List<string> { "Customer" } }
                        },
                        EndDate = x.Status == SavingsAccountStatusCode.Active.ToString()
                            ? new DateTime?()
                            : x.LatestStatusItem.TransactionDate,
                        StatusCode = x.Status,
                        StatusDisplayText = x.Status,
                        EntityType = x.AccountTypeCode.ToString(),
                    })
                    .ToList();
            }
        }
    }
}
