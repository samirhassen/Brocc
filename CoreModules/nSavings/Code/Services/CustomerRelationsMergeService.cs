using System;
using System.Collections.Generic;
using System.Linq;
using nSavings.DbModel;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;
using NTech.Core.Savings.Shared.Services;

namespace nSavings.Code.Services;

public class CustomerRelationsMergeService(ICustomerClient customerClient) : ICustomerRelationsMergeService
{
    public void MergeSavingsAccountsToCustomerRelations(ISet<string> onlySavingsAccountNrs = null)
    {
        string[] savingsAccountNrs;
        using (var context = new SavingsContext())
        {
            var pre = context.SavingsAccountHeaders.AsQueryable();
            if (onlySavingsAccountNrs is { Count: > 0 })
                pre = pre.Where(x => onlySavingsAccountNrs.Contains(x.SavingsAccountNr));
            savingsAccountNrs = pre.Select(x => x.SavingsAccountNr).ToArray();
        }

        var nonClosedStatusCodes = new List<string>
        {
            nameof(SavingsAccountStatusCode.Active),
            nameof(SavingsAccountStatusCode.FrozenBeforeActive)
        };

        foreach (var savingsAccountNrGroup in savingsAccountNrs.SplitIntoGroupsOfN(500))
        {
            using var context = new SavingsContext();
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
                        .Where(y => y.Name == nameof(DatedSavingsAccountStringCode.SavingsAccountStatus))
                        .OrderByDescending(y => y.BusinessEventId)
                        .Select(y => (DateTime?)y.TransactionDate)
                        .FirstOrDefault()
                })
                .ToList()
                .Select(x => new CustomerClientCustomerRelation
                {
                    CustomerId = x.MainCustomerId,
                    RelationId = x.SavingsAccountNr,
                    RelationType =
                        $"SavingsAccount_{x.AccountTypeCode.ToString()}",
                    StartDate = x.StartDate,
                    EndDate = nonClosedStatusCodes.Contains(x.Status) ? null : x.StatusDate
                })
                .ToList();

            customerClient.MergeCustomerRelations(relations);
        }
    }
}