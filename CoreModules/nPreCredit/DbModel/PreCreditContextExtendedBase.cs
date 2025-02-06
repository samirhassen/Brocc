using Newtonsoft.Json;
using nPreCredit.DbModel;
using NTech;
using NTech.Core;
using NTech.Core.Module.Shared.Database;
using NTech.Legacy.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit
{
    public class PreCreditContextExtendedBase : PreCreditContext
    {
        private readonly ICombinedClock clock;
        public PreCreditContextExtendedBase(int currentUserId, ICombinedClock clock, string informationMetadata) : base()
        {
            this.CurrentUserId = currentUserId;
            this.clock = clock;
            this.InformationMetadata = informationMetadata;
        }

        public int CurrentUserId { get; }
        public string InformationMetadata { get; }
        public IClock Clock => clock;
        public ICoreClock CoreClock => clock;

        public T FillInfrastructureFields<T>(T b) where T : InfrastructureBaseItem
        {
            b.ChangedById = this.CurrentUserId;
            b.ChangedDate = this.Clock.Now;
            b.InformationMetaData = this.InformationMetadata;
            return b;
        }

        public void CancelCreditApplication(CreditApplicationHeader creditApplicationHeader, string cancelledState, bool wasAutomated)
        {
            creditApplicationHeader.IsCancelled = true;
            creditApplicationHeader.CancelledBy = CurrentUserId;
            creditApplicationHeader.CancelledDate = Clock.Now;
            creditApplicationHeader.CancelledState = cancelledState;
            creditApplicationHeader.IsActive = false;
            var c = new CreditApplicationCancellation
            {
                CreditApplication = creditApplicationHeader,
                CancelledBy = CurrentUserId,
                CancelledDate = Clock.Now,
                CancelledState = cancelledState,
                WasAutomated = wasAutomated
            };
            FillInfrastructureFields(c);
            CreditApplicationCancellations.Add(c);
        }

        public CreditApplicationEvent CreateAndAddEvent(CreditApplicationEventCode eventCode, string applicationNr = null, CreditApplicationHeader creditApplicationHeader = null)
        {
            var evt = CreateEvent(eventCode, applicationNr: applicationNr, creditApplicationHeader: creditApplicationHeader);
            this.CreditApplicationEvents.Add(evt);
            return evt;
        }

        public CreditApplicationEvent CreateEvent(CreditApplicationEventCode eventCode, string applicationNr = null, CreditApplicationHeader creditApplicationHeader = null)
        {
            var e = new CreditApplicationEvent
            {
                ApplicationNr = applicationNr,
                Application = creditApplicationHeader,
                EventType = eventCode.ToString(),
                EventDate = Clock.Now,
                TransactionDate = Clock.Today
            };
            FillInfrastructureFields(e);
            return e;
        }

        public MortgageLoanCreditApplicationHeaderExtension CreateMortgageLoanCreditApplicationHeaderExtension()
        {
            var m = new MortgageLoanCreditApplicationHeaderExtension();
            FillInfrastructureFields(m);
            return m;
        }

        public class CreditApplicationItemModel
        {
            public string GroupName { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
            public bool IsEncrypted { get; set; }
        }

        public CreditApplicationItemModel CreateApplicationItemModel(string groupName, string name, string value, bool isEncrypted)
        {
            return new CreditApplicationItemModel
            {
                GroupName = groupName,
                Name = name,
                Value = value,
                IsEncrypted = isEncrypted
            };
        }

        public void AddOrUpdateCreditApplicationItems(CreditApplicationHeader creditApplicationHeader, List<CreditApplicationItemModel> newOrUpdatedItems, string addedInStepName)
        {
            this.EnsureCurrentTransaction();

            List<Tuple<string, CreditApplicationItem>> itemsToEncrypt = new List<Tuple<string, CreditApplicationItem>>();

            foreach (var g in newOrUpdatedItems.GroupBy(x => new { x.GroupName, x.Name }))
            {
                var i = g.Single();
                var existingItem = creditApplicationHeader.Items.SingleOrDefault(x => x.Name == i.Name && x.GroupName == i.GroupName);
                if (existingItem != null)
                {
                    if (i.IsEncrypted)
                    {
                        //Add a new encrypted item and then set the id on i
                        existingItem.IsEncrypted = true;
                        existingItem.Value = null;
                        itemsToEncrypt.Add(Tuple.Create(i.Value, existingItem));
                    }
                    else
                    {
                        existingItem.Value = i.Value;
                        existingItem.IsEncrypted = false;
                    }
                    FillInfrastructureFields(existingItem);
                    existingItem.AddedInStepName = addedInStepName;
                }
                else
                {
                    var newItem = new CreditApplicationItem
                    {
                        AddedInStepName = addedInStepName,
                        ApplicationNr = creditApplicationHeader.ApplicationNr,
                        CreditApplication = creditApplicationHeader,
                        GroupName = i.GroupName,
                        IsEncrypted = i.IsEncrypted,
                        Name = i.Name
                    };
                    FillInfrastructureFields(newItem);
                    if (i.IsEncrypted)
                    {
                        itemsToEncrypt.Add(Tuple.Create(i.Value, newItem));
                    }
                    else
                    {
                        newItem.Value = i.Value;
                    }

                    creditApplicationHeader.Items.Add(newItem);
                    CreditApplicationItems.Add(newItem);
                }
            }
            if (itemsToEncrypt.Any())
            {
                var enc = NEnv.EncryptionKeys;
                EncryptionContext<PreCreditContextExtended>.SaveEncryptItemsShared(
                    itemsToEncrypt.ToArray(),
                    x => x.Item1,
                    (x, y) => x.Item2.Value = y.ToString(),
                    CurrentUserId,
                    enc.CurrentKeyName, enc.AsDictionary(),
                    Clock,
                    this);
            }
        }

        public void SetDocumentCheckStatus(CreditApplicationHeader creditApplicationHeader, bool? isAccepted, List<string> rejectionsReasons)
        {
            this.EnsureCurrentTransaction();
            var documentCheckStatusCode = isAccepted.HasValue ? (isAccepted.Value ? "Accepted" : "Rejected") : "Initial";
            var items = new List<CreditApplicationItemModel>();

            items.Add(CreateApplicationItemModel("application", "documentCheckRejectionReasons", JsonConvert.SerializeObject(rejectionsReasons ?? new List<string>()), true));
            items.Add(CreateApplicationItemModel("application", "documentCheckStatus", documentCheckStatusCode, false));

            AddOrUpdateCreditApplicationItems(creditApplicationHeader, items, "DocumentCheck");
        }

        public KeyValueItem CreateAndAddKeyValueItem(string keySpace, string key, string value)
        {
            var item = FillInfrastructureFields(new KeyValueItem
            {
                Key = key,
                KeySpace = keySpace,
                Value = value
            });
            KeyValueItems.Add(item);
            return item;
        }
    }
}