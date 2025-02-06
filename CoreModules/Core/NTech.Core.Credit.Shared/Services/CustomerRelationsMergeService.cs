using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.Code.Services
{
    public class CustomerRelationsMergeService : ICustomerRelationsMergeService
    {
        private readonly ICustomerClient customerClient;
        private readonly CreditContextFactory creditContextFactory;

        public CustomerRelationsMergeService(ICustomerClient customerClient, CreditContextFactory creditContextFactory)
        {
            this.customerClient = customerClient;
            this.creditContextFactory = creditContextFactory;
        }

        public void MergeLoansToCustomerRelations(ISet<string> onlyTheseCreditNrs = null)
        {
            string[] creditNrs;
            using (var context = creditContextFactory.CreateContext())
            {
                var pre = context.CreditHeadersQueryable;
                if (onlyTheseCreditNrs != null && onlyTheseCreditNrs.Count > 0)
                    pre = pre.Where(x => onlyTheseCreditNrs.Contains(x.CreditNr));
                creditNrs = pre.Select(x => x.CreditNr).ToArray();
            }

            foreach (var creditNrGroup in creditNrs.SplitIntoGroupsOfN(500))
            {
                using (var context = creditContextFactory.CreateContext())
                {
                    var relations = context
                        .CreditHeadersQueryable
                        .Where(x => creditNrGroup.Contains(x.CreditNr))
                        .Select(x => new
                        {
                            x.CreditNr,
                            x.CreditType,
                            x.StartDate,
                            x.Status,
                            CustomerIds = x.CreditCustomers.Select(y => y.CustomerId),
                            StatusDate = x
                                .DatedCreditStrings
                                .Where(y => y.Name == "CreditStatus")
                                .OrderByDescending(y => y.BusinessEventId)
                                .Select(y => (DateTime?)y.TransactionDate)
                                .FirstOrDefault()
                        })
                        .ToList()
                        .SelectMany(x => x.CustomerIds.Select(y => new { CustomerId = y, Credit = x }))
                        .Select(x => new CustomerClientCustomerRelation
                        {
                            CustomerId = x.CustomerId,
                            RelationId = x.Credit.CreditNr,
                            RelationType = $"Credit_{(x.Credit.CreditType ?? "UnsecuredLoan")}",
                            StartDate = x.Credit.StartDate.Date,
                            EndDate = x.Credit.Status == CreditStatus.Normal.ToString() ? null : x.Credit.StatusDate
                        })
                        .ToList();

                    customerClient.MergeCustomerRelations(relations);
                }
            }
        }
    }

    public interface ICustomerRelationsMergeService
    {
        void MergeLoansToCustomerRelations(ISet<string> onlyTheseCreditNrs = null);
    }
}