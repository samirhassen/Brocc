using Newtonsoft.Json;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class MortgageLoanFixedInterestChangeEventManager : BusinessEventManagerOrServiceBase
    {
        private readonly CreditContextFactory creditContextFactory;
        private readonly ICreditEnvSettings envSettings;

        public MortgageLoanFixedInterestChangeEventManager(INTechCurrentUserMetadata currentUser, CreditContextFactory creditContextFactory,
            ICreditEnvSettings envSettings, ICoreClock clock, IClientConfigurationCore clientConfiguration) : base(currentUser, clock, clientConfiguration)
        {
            this.creditContextFactory = creditContextFactory;
            this.envSettings = envSettings;
        }

        public (PendingChangeStorageModel PendingChange, bool IsPendingChangeCommitAllowed, List<FixedMortgageLoanInterestRateBase> CurrentRates) GetCurrent()
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var pendingChangeModelRaw = context
                   .KeyValueItemsQueryable
                   .SingleOrDefault(x => x.Key == PendingChangeKeyAndKeySpaceName && x.KeySpace == PendingChangeKeyAndKeySpaceName)
                   ?.Value;

                PendingChangeStorageModel pendingChange = null;
                var isPendingChangeCommitAllowed = false;
                if (pendingChangeModelRaw != null)
                {
                    pendingChange = JsonConvert.DeserializeObject<PendingChangeStorageModel>(pendingChangeModelRaw);
                    isPendingChangeCommitAllowed = IsCommitAllowed(pendingChange, false);
                }

                var currentRates = context.FixedMortgageLoanInterestRatesQueryable.OrderBy(x => x.MonthCount).ToList().Cast<FixedMortgageLoanInterestRateBase>().ToList();
                if (currentRates.Count == 0)
                    currentRates = new List<FixedMortgageLoanInterestRateBase>
                    {
                        new FixedMortgageLoanInterestRateBase
                        {
                            MonthCount = FallbackMonthCount,
                            RatePercent = 0
                        }
                    };

                return (PendingChange: pendingChange, IsPendingChangeCommitAllowed: isPendingChangeCommitAllowed, CurrentRates: currentRates);
            }
        }

        public void BeginChange(Dictionary<int, decimal> newRateByMonthCount)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var pendingChangeModelItem = GetPendingModel(context);

                if (newRateByMonthCount == null)
                    throw new NTechCoreWebserviceException("NewRateByMonthCount required") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                if (!newRateByMonthCount.ContainsKey(FallbackMonthCount))
                    throw new NTechCoreWebserviceException($"A value for {FallbackMonthCount} is required in NewRateByMonthCount") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                if (pendingChangeModelItem != null)
                    throw new NTechCoreWebserviceException("There is already a pending change") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                var now = Clock.Now;

                var storageModel = new PendingChangeStorageModel
                {
                    InitiatedByUserId = UserId,
                    InitiatedDate = now.DateTime,
                    NewRateByMonthCount = newRateByMonthCount
                };

                context.AddKeyValueItems(new KeyValueItem
                {
                    ChangedById = UserId,
                    ChangedDate = now,
                    InformationMetaData = InformationMetadata,
                    Key = PendingChangeKeyAndKeySpaceName,
                    KeySpace = PendingChangeKeyAndKeySpaceName,
                    Value = JsonConvert.SerializeObject(storageModel)
                });


                context.SaveChanges();
            }
        }

        public bool CancelChange()
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var pendingChangeModelItem = GetPendingModel(context);

                bool wasRemoved = false;
                if (pendingChangeModelItem != null)
                {
                    context.RemoveKeyValueItem(pendingChangeModelItem);
                    wasRemoved = true;
                    context.SaveChanges();
                }

                return wasRemoved;
            }
        }

        public BusinessEvent CommitChange(bool overrideDualityCommitRequirement)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var pendingChangeModelItem = GetPendingModel(context);

                if (pendingChangeModelItem == null)
                    throw new NTechCoreWebserviceException("No pending change to commit exists") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                var pendingChange = JsonConvert.DeserializeObject<PendingChangeStorageModel>(pendingChangeModelItem.Value);

                if (!IsCommitAllowed(pendingChange, overrideDualityCommitRequirement))
                    throw new NTechCoreWebserviceException("Duality required. A different user must commit the change.") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                var currentItems = context.FixedMortgageLoanInterestRatesQueryable.ToList();

                foreach (var existingItem in currentItems)
                {
                    context.RemoveFixedMortgageLoanInterestRates(existingItem);
                }

                var now = context.CoreClock.Now;
                var evt = AddBusinessEvent(BusinessEventType.ChangedMortgageLoanFixedInterestRate, context);

                foreach (var r in pendingChange.NewRateByMonthCount)
                {
                    var monthCount = r.Key;
                    var ratePercent = r.Value;
                    context.AddFixedMortgageLoanInterestRates(new FixedMortgageLoanInterestRate
                    {
                        CreatedByEvent = evt,
                        MonthCount = monthCount,
                        RatePercent = ratePercent
                    });
                    context.AddHFixedMortgageLoanInterestRates(new HFixedMortgageLoanInterestRate
                    {
                        CreatedByEvent = evt,
                        MonthCount = monthCount,
                        RatePercent = ratePercent
                    });
                }

                context.RemoveKeyValueItem(pendingChangeModelItem);

                context.SaveChanges();

                return evt;
            }
        }

        public void HandleChange(bool isCancel, bool isCommit, Dictionary<int, decimal> newRateByMonthCount, bool overrideDualityCommitRequirement)
        {
            if (isCancel)
            {
                if (newRateByMonthCount != null)
                    throw new NTechCoreWebserviceException("NewRateByMonthCount cannot be used with Cancel") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                CancelChange();
            }
            else if (isCommit)
            {
                if (newRateByMonthCount != null)
                    throw new NTechCoreWebserviceException("NewRateByMonthCount cannot be used with Commit") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                CommitChange(overrideDualityCommitRequirement);
            }
            else
            {
                BeginChange(newRateByMonthCount);
            }
        }

        private bool IsCommitAllowed(PendingChangeStorageModel pendingChange, bool requestOverride)
        {
            if (pendingChange.InitiatedByUserId != UserId)
                return true;

            if (envSettings.IsProduction)
                return false;

            return requestOverride;
        }

        private KeyValueItem GetPendingModel(ICreditContextExtended context) => context
                    .KeyValueItemsQueryable
                    .SingleOrDefault(x => x.Key == PendingChangeKeyAndKeySpaceName && x.KeySpace == PendingChangeKeyAndKeySpaceName);

        public class PendingChangeStorageModel
        {
            public Dictionary<int, decimal> NewRateByMonthCount { get; set; }
            public int InitiatedByUserId { get; set; }
            public DateTime InitiatedDate { get; set; }
        }

        public static string PendingChangeKeyAndKeySpaceName = "PendingFixedInterestChange";

        public static int FallbackMonthCount = 3;
    }
}