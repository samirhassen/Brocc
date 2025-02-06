using nPreCredit.DbModel;
using nPreCredit.DbModel.Repository;
using NTech;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services.SharedStandard
{
    public class LoanStandardCustomerRelationService : ILoanStandardCustomerRelationService
    {
        private const string ActiveEndDateMarker = "active";
        private const string ListName = "CustomerRelationReplication";
        private const string ItemName = "EndDate";
        private readonly INTechCurrentUserMetadata user;
        private readonly IClock clock;

        private static Lazy<string> ApplicationType = new Lazy<string>(() =>
        {
            if (NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans.standard"))
                return "Application_UnsecuredLoan";
            if (NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.mortgageloans.standard"))
                return "Application_MortgageLoan";
            throw new NotImplementedException();
        });

        private static bool IsEnabled =>
            NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans.standard")
            || NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.mortgageloans.standard");

        public LoanStandardCustomerRelationService(INTechCurrentUserMetadata user, IClock clock)
        {
            this.user = user;
            this.clock = clock;
        }

        public void AddNewApplication(IEnumerable<int> customerIds, string applicationNr, DateTimeOffset applicationDate)
        {
            if (!IsEnabled)
                return;

            using (var context = new PreCreditContextExtended(user, clock))
            {
                new PreCreditCustomerClient().MergeCustomerRelations(customerIds.Select(x =>
                    new CustomerClientCustomerRelation
                    {
                        CustomerId = x,
                        RelationType = ApplicationType.Value,
                        RelationId = applicationNr,
                        StartDate = applicationDate.Date,
                        EndDate = null
                    }).ToList());

                ComplexApplicationListService.SetSingleUniqueItem(applicationNr, ListName, ItemName, 1, ActiveEndDateMarker, context);

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Since applications can be closed and re-opened multiple times we basically need to recheck this any time an application changes
        /// What we do is use timestamp (a global, strictly increasing number of each change in the database) as an event flow we track.
        /// 
        /// For each change we diff it with a local copy of the last end date sent to the customer module and if anything is different
        /// we send a synch. 
        /// 
        /// We keep track of our position using SystemItemCode.StandardApplication_CustomerRelationMerge.
        /// </summary>
        public void SynchronizeExistingApplications()
        {
            if (!IsEnabled)
                return;

            //Pick up the global max before starting so we wont loop forever as new applications keep arriving.
            byte[] globalMaxTs;
            using (var context = new PreCreditContextExtended(user, clock))
            {
                globalMaxTs = context.CreditApplicationHeaders.Max(x => x.Timestamp);
            }

            var lastBatchSize = 1;
            var guard = 0;
            while (lastBatchSize > 0)
            {
                lastBatchSize = SynchronizeApplicationBatch(globalMaxTs);
                if (guard++ > 1000)
                    throw new Exception("Infinite loop. Something is wrong with this logic.");
            }
        }

        private int SynchronizeApplicationBatch(byte[] globalMaxTs)
        {
            var customerClient = new PreCreditCustomerClient();

            var relations = new List<CustomerClientCustomerRelation>();
            var operations = new List<ComplexApplicationListOperation>();

            var systemItemRepo = new SystemItemRepository(user.UserId, user.InformationMetadata, clock);

            using (var context = new PreCreditContextExtended(user, clock))
            {
                var latestSeenTs = systemItemRepo.GetTimestamp(SystemItemCode.StandardApplication_CustomerRelationMerge, context);
                var batch = GetApplicationBatch(context, globalMaxTs, latestSeenTs);

                byte[] newLatestSeenTs = null;
                foreach (var application in batch)
                {
                    var newEndDates = GetEndDates(application.IsActive, application.ChangedDate);

                    if (application.LocallyStoredEndDate != newEndDates.LocallyStoredEndDate)
                    {
                        //We dont check if ApplicationDate changed since it never does
                        //EndDate though can change multiple times back and forth since closed applications can be re-activated.
                        relations.AddRange(application.ApplicantCustomerIds.Select(x => new CustomerClientCustomerRelation
                        {
                            CustomerId = x,
                            RelationType = ApplicationType.Value,
                            RelationId = application.ApplicationNr,
                            StartDate = application.ApplicationDate.Date,
                            EndDate = newEndDates.CustomerRelationEndDate
                        }));
                        operations.Add(new ComplexApplicationListOperation
                        {
                            ApplicationNr = application.ApplicationNr,
                            ListName = ListName,
                            Nr = 1,
                            ItemName = ItemName,
                            UniqueValue = newEndDates.LocallyStoredEndDate
                        });
                    }

                    newLatestSeenTs = application.Timestamp;
                }

                if (relations.Count > 0)
                {
                    customerClient.MergeCustomerRelations(relations);
                }

                if (operations.Count > 0)
                {
                    ComplexApplicationListService.ChangeListComposable(operations, context);
                }

                if (newLatestSeenTs != null)
                {
                    systemItemRepo.SetTimestamp(SystemItemCode.StandardApplication_CustomerRelationMerge, newLatestSeenTs, context);
                }

                context.SaveChanges();

                return batch.Count;
            }
        }

        private List<ApplicationReplicationData> GetApplicationBatch(PreCreditContextExtended context, byte[] globalMaxTs, byte[] latestSeenTs)
        {
            const int BatchSize = 500;
            var q = context
                .CreditApplicationHeaders
                .Select(x => new ApplicationReplicationData
                {
                    ApplicationNr = x.ApplicationNr,
                    Timestamp = x.Timestamp,
                    ApplicationDate = x.ApplicationDate,
                    IsActive = x.IsActive,
                    ChangedDate = x.ChangedDate,
                    ApplicantCustomerIds = x.CustomerListMemberships
                        .Where(y => y.ListName == "Applicant")
                        .Select(y => y.CustomerId),
                    LocallyStoredEndDate = x
                        .ComplexApplicationListItems
                        .Where(y => y.ListName == ListName && y.Nr == 1 && y.ItemName == ItemName)
                        .Select(y => y.ItemValue)
                        .FirstOrDefault()
                })
                .Where(x => BinaryComparer.Compare(x.Timestamp, globalMaxTs) <= 0);

            if (latestSeenTs != null)
                q = q.Where(x => BinaryComparer.Compare(x.Timestamp, latestSeenTs) > 0);

            return q
                .OrderBy(x => x.Timestamp)
                .Take(BatchSize)
                .ToList();
        }

        private class ApplicationReplicationData
        {
            public string ApplicationNr { get; set; }
            public byte[] Timestamp { get; set; }
            public DateTimeOffset ApplicationDate { get; set; }
            public bool IsActive { get; set; }
            public DateTimeOffset ChangedDate { get; set; }
            public IEnumerable<int> ApplicantCustomerIds { get; set; }
            public string LocallyStoredEndDate { get; set; }
        }

        private (string LocallyStoredEndDate, DateTime? CustomerRelationEndDate) GetEndDates(bool isActive, DateTimeOffset changedDate) =>
            isActive
                ? (LocallyStoredEndDate: ActiveEndDateMarker, CustomerRelationEndDate: new DateTime?())
                : (LocallyStoredEndDate: changedDate.Date.ToString("yyyy-MM-dd"), CustomerRelationEndDate: changedDate.Date);

        public static class BinaryComparer
        {
            public static int Compare(byte[] b1, byte[] b2)
            {
                throw new NotImplementedException();
            }
        }
    }
}