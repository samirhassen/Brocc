using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.Code.Services
{
    public class FetchCustomerKycStatusChangesResult
    {
        public int TotalScreenedCount { get; set; }
        public List<int> ConflictedCustomerIds { get; set; }
    }

    public class KycService
    {
        private readonly ICustomerClient customerClient;

        public KycService(ICustomerClient customerClient)
        {
            this.customerClient = customerClient;
        }

        public FetchCustomerKycStatusChangesResult FetchCustomerKycStatusChanges(ISet<int> activeCustomerIds, DateTime screenDate)
        {
            var r = customerClient.FetchCustomerKycStatusChanges(activeCustomerIds, screenDate);
            return new FetchCustomerKycStatusChangesResult
            {
                ConflictedCustomerIds = r.CustomerIdsWithChanges,
                TotalScreenedCount = r.TotalScreenedCount
            };
        }

        public void ListScreenBatch(ISet<int> customerIds, DateTime screenDate)
        {
            customerClient.KycScreenNew(customerIds, screenDate, false);
        }

        public ISet<int> FetchAllActiveCustomerIds(ICreditContextExtended context)
        {
            return new HashSet<int>(context
                .CreditHeadersQueryable
                .Where(x => x.Status == CreditStatus.Normal.ToString() || x.Status == CreditStatus.SentToDebtCollection.ToString())
                .SelectMany(x => x.CreditCustomers)
                .Select(x => x.CustomerId)
                .ToList());
        }

        public ISet<int> CompanyLoanFetchAllActiveCustomerIds(ICreditContextExtended context)
        {
            return new HashSet<int>(context
                .CreditCustomerListMembersQueryable
                .Where(x => (x.ListName == "companyLoanBeneficialOwner" || x.ListName == "companyLoanAuthorizedSignatory")
                    && (x.Credit.Status == CreditStatus.Normal.ToString() || x.Credit.Status == CreditStatus.SentToDebtCollection.ToString()))
                .Select(x => x.CustomerId)
                .ToList());
        }
    }
}