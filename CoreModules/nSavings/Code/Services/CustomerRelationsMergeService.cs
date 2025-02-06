using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nSavings.Code.Services
{
    public class CustomerRelationsMergeService : ICustomerRelationsMergeService
    {
        private readonly NTech.Core.Module.Shared.Clients.ICustomerClient customerClient;

        public CustomerRelationsMergeService(NTech.Core.Module.Shared.Clients.ICustomerClient customerClient)
        {
            this.customerClient = customerClient;
        }

        public void MergeSavingsAccountsToCustomerRelations(ISet<string> onlySavingsAccountNrs = null)
        {
            string[] savingsAccountNrs;
            using (var context = new SavingsContext())
            {
                var pre = context.SavingsAccountHeaders.AsQueryable();
                if (onlySavingsAccountNrs != null && onlySavingsAccountNrs.Count > 0)
                    pre = pre.Where(x => onlySavingsAccountNrs.Contains(x.SavingsAccountNr));
                savingsAccountNrs = pre.Select(x => x.SavingsAccountNr).ToArray();
            }

            var nonClosedStatusCodes = new List<string>
                {
                    SavingsAccountStatusCode.Active.ToString(),
                    SavingsAccountStatusCode.FrozenBeforeActive.ToString()
                };

            foreach (var savingsAccountNrGroup in savingsAccountNrs.SplitIntoGroupsOfN(500))
            {
                using (var context = new SavingsContext())
                {
                    var relations = context
                        .SavingsAccountHeaders
                        .Where(x => savingsAccountNrGroup.Contains(x.SavingsAccountNr))
                        .Select(x => new
                        {
                            x.SavingsAccountNr,
                            x.AccountTypeCode,
                            StartDate = x.CreatedByEvent.TransactionDate,
                            x.Status,
                            x.MainCustomerId,
                            StatusDate = x
                                .DatedStrings
                                .Where(y => y.Name == DatedSavingsAccountStringCode.SavingsAccountStatus.ToString())
                                .OrderByDescending(y => y.BusinessEventId)
                                .Select(y => (DateTime?)y.TransactionDate)
                                .FirstOrDefault()
                        })
                        .ToList()
                        .Select(x => new CustomerClientCustomerRelation
                        {
                            CustomerId = x.MainCustomerId,
                            RelationId = x.SavingsAccountNr,
                            RelationType = $"SavingsAccount_{(x.AccountTypeCode ?? SavingsAccountTypeCode.StandardAccount.ToString())}",
                            StartDate = x.StartDate,
                            EndDate = nonClosedStatusCodes.Contains(x.Status) ? null : x.StatusDate
                        })
                        .ToList();

                    customerClient.MergeCustomerRelations(relations);
                }
            }
        }
    }
}