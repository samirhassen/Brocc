using NTech.Banking.ScoringEngine;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.PreCredit.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code
{
    public class LegacyUlApplicationScoringDataService
    {
        public class ApplicationScoringDataForCustomers
        {
            public Dictionary<int, IList<HistoricalApplication>> OtherApplications { get; set; }
            public Dictionary<int, List<HistoricalCreditExtended>> CustomerCreditHistoryByApplicantNr { get; set; }
            public IDictionary<int, IList<PauseItemScoringModel>> ReleventPausedItemsByApplicantNr { get; set; }
        }

        public ApplicationScoringDataForCustomers GetApplicationScoringDataForCustomers(string applicationNr, IPreCreditContextExtended context, ICustomerServiceRepository c, NTech.Core.Module.Shared.Clients.ICreditClient creditClient)
        {
            var h = context
                .CreditApplicationHeadersQueryable
                .Where(x => x.ApplicationNr == applicationNr)
                .Select(x => new
                {
                    CustomerIdItems = x.Items.Where(y => y.Name == "customerId" && !y.IsEncrypted).Select(y => new { y.GroupName, y.Value })
                })
                .Single();
            var applicantNrAndCustomerIds = new List<Tuple<int, int>>();
            foreach (var ci in h.CustomerIdItems)
            {
                var applicantNr = int.Parse(ci.GroupName.Substring("applicant".Length));
                var customerId = int.Parse(ci.Value);
                applicantNrAndCustomerIds.Add(Tuple.Create(applicantNr, customerId));
            }
            applicantNrAndCustomerIds = applicantNrAndCustomerIds.OrderBy(x => x.Item1).ToList();
            return GetApplicationScoringDataForCustomers(applicationNr, applicantNrAndCustomerIds, c, creditClient);
        }

        public ApplicationScoringDataForCustomers GetApplicationScoringDataForCustomers(string applicationNr, IList<Tuple<int, int>> applicantNrAndCustomerIds, ICustomerServiceRepository c, NTech.Core.Module.Shared.Clients.ICreditClient creditClient)
        {
            var otherApplications = new Dictionary<int, IList<HistoricalApplication>>();
            var customerCreditHistoryByApplicantNr = new Dictionary<int, List<HistoricalCreditExtended>>();

            var customerIds = (applicantNrAndCustomerIds.Select(x => x.Item2).ToArray());

            var applicationsByCustomerId = c.FindByCustomerIds(applicantNrAndCustomerIds.Select(x => x.Item2).ToArray());
            var customerCreditHistories = creditClient.GetCustomerCreditHistory(customerIds.ToList());

            foreach (var p in applicantNrAndCustomerIds)
            {
                var applicantNr = p.Item1;
                var customerId = p.Item2;

                var customerApplications = applicationsByCustomerId[customerId];
                var customerCredits = customerCreditHistories.Where(x => x.CustomerIds.Contains(customerId)).ToList();

                customerCreditHistoryByApplicantNr[applicantNr] = customerCredits;

                otherApplications[applicantNr] = customerApplications
                    .Where(x => x.ApplicationNr != applicationNr && !customerCredits.Any(y => y.CreditNr == x.CreditNr))
                    .ToList();
            }

            return new ApplicationScoringDataForCustomers
            {
                OtherApplications = otherApplications,
                CustomerCreditHistoryByApplicantNr = customerCreditHistoryByApplicantNr
            };
        }
    }
}