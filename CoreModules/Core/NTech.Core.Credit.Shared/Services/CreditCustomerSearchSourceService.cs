using nCredit;
using nCredit.Code.Services;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services
{
    public class CreditCustomerSearchSourceService : ICustomerSearchSourceService
    {
        private readonly CreditSearchService creditSearchService;
        private readonly CreditContextFactory creditContextFactory;
        private readonly ICoreClock clock;

        public CreditCustomerSearchSourceService(CreditSearchService creditSearchService, CreditContextFactory creditContextFactory, ICoreClock clock)
        {
            this.creditSearchService = creditSearchService;
            this.creditContextFactory = creditContextFactory;
            this.clock = clock;
        }
        public ISet<int> FindCustomers(string searchQuery) => creditSearchService
            .Search(new SearchCreditRequest
            {
                OmnisearchValue = searchQuery,
                SkipCustomerSearch = true //The nCustomer version of ICustomerSearchSourceService handles this so no need to double search
            })
            .SelectMany(x => x.ConnectedCustomerIds)
            .ToHashSetShared();

        public List<CustomerSearchEntity> GetCustomerEntities(int customerId)
        {
            void AddCustomerWithRole(Dictionary<int, HashSet<string>> localCustomersWithRoles, int localCustomerId, string localRoleName)
            {
                if (!localCustomersWithRoles.ContainsKey(localCustomerId))
                    localCustomersWithRoles.Add(localCustomerId, new HashSet<string> { localRoleName });
                else
                    localCustomersWithRoles[localCustomerId].Add(localRoleName);
            }

            using (var context = creditContextFactory.CreateContext())
            {
                var result = context
                    .CreditHeadersQueryable
                    .Where(x => x.CustomerListMembers.Any(y => y.CustomerId == customerId) || x.CreditCustomers.Any(y => y.CustomerId == customerId))
                    .Select(x => new
                    {
                        Entity = new CustomerSearchEntity
                        {
                            Source = "nCredit",
                            StatusCode = x.Status,
                            IsActive = x.Status == CreditStatus.Normal.ToString(),
                            CurrentBalance = x.Transactions.Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                            EntityId = x.CreditNr,
                            EntityType = x.CreditType
                        },
                        x.Status,
                        x.StartDate,
                        LatestStatusItem = x.DatedCreditStrings.Where(y => y.Name == DatedCreditStringCode.CreditStatus.ToString()).OrderByDescending(y => y.BusinessEventId).FirstOrDefault(),
                        x.CreditCustomers,
                        x.CustomerListMembers,
                        OldestOpenNotificationDueDate = x
                            .Notifications
                            .Where(y => y.ClosedTransactionDate == null)
                            .OrderBy(y => y.DueDate)
                            .Select(y => (DateTime?)y.DueDate)
                            .FirstOrDefault(),
                        x.CollateralHeaderId,
                        x.CreditType
                    })
                    .ToList();

                var today = clock.Today;
                foreach (var item in result)
                {
                    var entity = item.Entity;

                    entity.CreationDate = item.StartDate.DateTime;

                    entity.EndDate = entity.IsActive ? new DateTime?() : item.LatestStatusItem.TransactionDate;

                    var customersWithRoles = new Dictionary<int, HashSet<string>>();
                    foreach (var creditCustomer in item.CreditCustomers)
                    {
                        AddCustomerWithRole(customersWithRoles, creditCustomer.CustomerId, "creditCustomer");
                    }
                    foreach (var listMember in item.CustomerListMembers)
                    {
                        AddCustomerWithRole(customersWithRoles, listMember.CustomerId, listMember.ListName);
                    }
                    entity.Customers = customersWithRoles
                        .Keys
                        .Select(x => new CustomerSearchEntityCustomer { CustomerId = x, Roles = customersWithRoles[x].ToList() })
                        .ToList();

                    entity.StatusDisplayText = "";
                    if (item.Status == CreditStatus.Normal.ToString())
                    {
                        int nrOfDaysOverdue = 0;
                        if (item.OldestOpenNotificationDueDate.HasValue && item.OldestOpenNotificationDueDate.Value < today)
                        {
                            nrOfDaysOverdue = (int)Math.Round(today.Subtract(item.OldestOpenNotificationDueDate.Value).TotalDays);
                        }

                        if (nrOfDaysOverdue < 30)
                            entity.StatusDisplayText = "Normal - Performing";
                        else if (nrOfDaysOverdue < 90)
                            entity.StatusDisplayText = $"Normal - Underperforming ({nrOfDaysOverdue})";
                        else
                            entity.StatusDisplayText = $"Normal - Impaired ({nrOfDaysOverdue})";
                    }
                    else
                    {
                        entity.StatusDisplayText = item.Status;
                    }

                    if (item.CollateralHeaderId.HasValue)
                    {
                        entity.GroupId = item.CollateralHeaderId.Value.ToString();
                        entity.GroupType = $"{item.CreditType}_Collateral";
                    }
                }
                return result.Select(x => x.Entity).ToList();
            }
        }
    }
}
