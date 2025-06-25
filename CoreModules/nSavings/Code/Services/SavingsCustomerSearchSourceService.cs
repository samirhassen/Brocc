using System.Collections.Generic;
using System.Linq;
using nSavings.DbModel;
using NTech.Core.Module.Shared.Services;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;

namespace nSavings.Code.Services;

public class SavingsCustomerSearchSourceService(SavingsAccountSearchService accountSearchService)
    : ICustomerSearchSourceService
{
    public ISet<int> FindCustomers(string searchQuery) => accountSearchService
        .Search(new SavingsAccountSearchRequest { OmnisearchValue = searchQuery, SkipCustomerSearch = true })
        .Select(x => x.MainCustomerId)
        .ToHashSetShared();

    public List<CustomerSearchEntity> GetCustomerEntities(int customerId)
    {
        using var context = new SavingsContext();
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
                CurrentBalance =
                    x.Transactions.Where(y => y.AccountCode == nameof(LedgerAccountTypeCode.Capital))
                        .Sum(y => (decimal?)y.Amount) ?? 0m,
                LatestStatusItem = x.DatedStrings
                    .Where(y => y.Name == nameof(DatedSavingsAccountStringCode.SavingsAccountStatus))
                    .OrderByDescending(y => y.BusinessEventId).FirstOrDefault()
            })
            .ToList()
            .Select(x => new CustomerSearchEntity
            {
                Source = "nSavings",
                EntityId = x.SavingsAccountNr,
                CreationDate = x.CreationDate,
                CurrentBalance = x.CurrentBalance,
                IsActive = x.Status == nameof(SavingsAccountStatusCode.Active),
                Customers =
                [
                    new CustomerSearchEntityCustomer
                    {
                        CustomerId = x.MainCustomerId,
                        Roles = ["Customer"]
                    }
                ],
                EndDate = x.Status == nameof(SavingsAccountStatusCode.Active)
                    ? null
                    : x.LatestStatusItem.TransactionDate,
                StatusCode = x.Status,
                StatusDisplayText = x.Status,
                EntityType = x.AccountTypeCode.ToString(),
            })
            .ToList();
    }
}