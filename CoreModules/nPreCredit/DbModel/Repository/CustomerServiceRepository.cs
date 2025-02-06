using NTech.Banking.ScoringEngine;
using NTech.Core.PreCredit.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit
{
    public class CustomerServiceRepository : BaseRepository, ICustomerServiceRepository
    {
        public CustomerServiceRepository() : base()
        {

        }
        public CustomerServiceRepository(
            string currentEncryptionKeyName,
            IDictionary<string, string> encryptionKeysByName) : base(currentEncryptionKeyName, encryptionKeysByName)
        {
        }

        public List<HistoricalApplication> FindByCustomerId(int customerId)
        {
            return FindByCustomerIds(customerId)[customerId];
        }

        private class TempModel
        {
            public CreditApplicationHeader H { get; set; }
            public IEnumerable<string> CustomerIds { get; set; }
        }

        public int[] GetComplexApplicationListCustomerIds(string applicationNr)
        {
            using (var context = new PreCreditContext())
            {
                var customerIdsByComplexApplicationList = context
                .ComplexApplicationListItems
                .Where(x => x.ItemName == "customerIds" && x.ApplicationNr == applicationNr)
                .Select(x => x.ItemValue)
                .ToArray();

                return Array.ConvertAll(customerIdsByComplexApplicationList, int.Parse);
            };
        }
        
        public Dictionary<int, List<HistoricalApplication>> FindByCustomerIds(params int[] customerIds) =>
            FindByCustomerIdsExtended(customerIds)
            ?.ToDictionary(x => x.Key, x => x.Value?.Cast<HistoricalApplication>()?.ToList());

        public Dictionary<int, List<HistoricalApplicationExtended>> FindByCustomerIdsExtended(params int[] customerIds)
        {
            using (var context = new PreCreditContext())
            {
                var customerIdStr = customerIds.Select(x => x.ToString()).ToList();

                IQueryable<TempModel> pre;

                if (!NEnv.IsCompanyLoansEnabled)
                {
                    pre = context
                            .CreditApplicationHeaders
                            .Select(x => new TempModel
                            {
                                H = x,
                                CustomerIds = x.Items.Where(y => y.Name == "customerId" && !y.IsEncrypted).Select(y => y.Value)
                            });
                }
                else
                {
                    pre = context
                            .CreditApplicationHeaders
                            .Select(x => new TempModel
                            {
                                H = x,
                                CustomerIds = x.Items.Where(y => (y.Name == "customerId" || y.Name == "companyCustomerId" || y.Name == "applicantCustomerId") && !y.IsEncrypted).Select(y => y.Value)
                            });
                }

                var tmp = pre
                    .Where(x => x.CustomerIds.Any(y => customerIdStr.Contains(y)))
                    .Select(x => new
                    {
                        CustomerIds = x.CustomerIds,
                        ApplicationNr = x.H.ApplicationNr,
                        ApplicationDate = x.H.ApplicationDate,
                        IsActive = x.H.IsActive,
                        ArchivedDate = x.H.ArchivedDate,
                        CreditCheckStatus = x.H.CreditCheckStatus,
                        CreditNr = x.H.Items.Where(y => y.Name == "creditnr" && !y.IsEncrypted).Select(y => y.Value).FirstOrDefault(),
                        IsFinalDecisionMade = x.H.IsFinalDecisionMade,
                        IsMortgageLoanApplication = x.H.MortgageLoanExtension != null,
                        ApplicationType = x.H.ApplicationType,
                        PauseItems = x.H.CurrentCreditDecision.PauseItems.Select(y => new HistoricalApplicationPauseItem
                        {
                            CustomerId = y.CustomerId,
                            PausedUntilDate = y.PausedUntilDate,
                            RejectionReasonName = y.RejectionReasonName
                        }).Concat(x.H.PauseItems.Where(y => !y.RemovedBy.HasValue).Select(y => new HistoricalApplicationPauseItem
                        {
                            CustomerId = y.CustomerId,
                            PausedUntilDate = y.PausedUntilDate,
                            RejectionReasonName = y.PauseReasonName
                        })),
                        RejectionReasonSearchTerms = x
                            .H
                            .CurrentCreditDecision
                            .SearchTerms
                            .Where(y => y.TermName == CreditDecisionSearchTerm.CreditDecisionSearchTermCode.RejectionReason.ToString())
                            .Select(y => y.TermValue),
                        CurrentCreditDecisionDate = (DateTimeOffset?)x.H.CurrentCreditDecision.DecisionDate
                    })
                    .ToList();
                return customerIds.ToDictionary(x => x, x => tmp
                    .Where(y => y.CustomerIds.Contains(x.ToString()))
                    .Select(y => new HistoricalApplicationExtended
                    {
                        ApplicationNr = y.ApplicationNr,
                        ApplicationDate = y.ApplicationDate,
                        IsActive = y.ArchivedDate.HasValue ? false : y.IsActive,
                        CreditCheckStatus = y.ArchivedDate.HasValue ? null : y.CreditCheckStatus,
                        CreditNr = y.CreditNr,
                        IsFinalDecisionMade = y.IsFinalDecisionMade,
                        PauseItems = y.ArchivedDate.HasValue ? null : y.PauseItems?.ToList(),
                        RejectionReasonSearchTerms = y.ArchivedDate.HasValue ? null : y.RejectionReasonSearchTerms?.ToList(),
                        IsMortgageLoanApplication = y.IsMortgageLoanApplication,
                        ArchivedDate = y.ArchivedDate,
                        ApplicationType = y.ApplicationType ?? "unsecuredLoan",
                        CurrentCreditDecisionDate = y.CurrentCreditDecisionDate
                    })
                    .ToList());
            }
        }

        private class DecryptedItem
        {
            public string ApplicationNr { get; set; }
            public string GroupName { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
        }

        public IDictionary<int, HashSet<string>> FindApplicationNrsPerCustomerId(IList<int> customerIds)
        {
            if (customerIds.Any())
            {
                var customerIdsStrings = customerIds.Select(x => x.ToString()).ToList();
                using (var context = new PreCreditContext())
                {
                    return context
                        .CreditApplicationItems
                        .Where(x => x.Name == "customerId" && customerIdsStrings.Contains(x.Value))
                        .Select(x => new { x.ApplicationNr, CustomerId = x.Value })
                        .ToList()
                        .GroupBy(x => x.CustomerId)
                        .ToDictionary(x => int.Parse(x.Key), x => new HashSet<string>(x.Select(y => y.ApplicationNr)));
                }
            }
            else
            {
                return new Dictionary<int, HashSet<string>>();
            }
        }


        public Dictionary<int, List<HistoricalApplication>> FindApplicationObjectsByCustomerIds(IList<int> customerIds)
        {
            if (customerIds?.Any() == true)
            {
                var customerIdsStrings = customerIds.Select(x => x.ToString()).ToList();

                using (var context = new PreCreditContext())
                {
                    var customerIdsByComplexApplicationList = context
                         .ComplexApplicationListItems
                         .Where(x => customerIdsStrings.Contains(x.ItemValue) && x.ItemName == "customerIds" && x.ListName == "ApplicationObject")
                         .Select(x => new { x, x.ApplicationNr, CustomerIds = x.ItemValue })
                         .ToList();



                    return customerIds.ToDictionary(x => x, x => customerIdsByComplexApplicationList
                        .Where(y => y.CustomerIds.Contains(x.ToString()))
                        .Select(y => new HistoricalApplication
                        {
                            ApplicationNr = y.ApplicationNr
                        })
                        .ToList());
                }
            }
            else
            {
                return new Dictionary<int, List<HistoricalApplication>>();
            }
        }

    }
}