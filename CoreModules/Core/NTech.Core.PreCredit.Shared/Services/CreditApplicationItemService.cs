using nPreCredit;
using nPreCredit.Code.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.PreCredit.Shared.Services
{
    public class CreditApplicationItemService
    {
        private readonly IPreCreditContextFactoryService contextFactoryService;
        private readonly EncryptionService encryptionService;

        public CreditApplicationItemService(IPreCreditContextFactoryService contextFactoryService, EncryptionService encryptionService)
        {
            this.contextFactoryService = contextFactoryService;
            this.encryptionService = encryptionService;
        }

        public Dictionary<string, Dictionary<string, Dictionary<string, string>>> BulkFetchCreditApplicationItems(BulkFetchCreditApplicationItemsRequest request)
        {
            using(var context = contextFactoryService.CreateExtended())
            {
                var query = context.CreditApplicationItemsQueryable.Where(x => request.ApplicationNrs.Contains(x.ApplicationNr));
                if (request.ItemNames != null && request.ItemNames.Count > 0)
                    query = query.Where(x => request.ItemNames.Contains(x.Name));
                var matchingItems = query.Select(x => new { x.ApplicationNr, x.IsEncrypted, x.GroupName, x.Name, x.Value }).ToList();
                var encryptedItemIds = matchingItems.Where(x => x.IsEncrypted).Select(x => long.Parse(x.Value)).Distinct().ToArray();
                var decryptedItems = encryptionService.DecryptEncryptedValues(context, encryptedItemIds);

                var itemGroupsByApplicationNr = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
                foreach(var matchingItem in matchingItems)
                {
                    var itemsByGroupName = GetOrAdd(itemGroupsByApplicationNr, matchingItem.ApplicationNr, () => new Dictionary<string, Dictionary<string, string>>());
                    var valuesByItemName = GetOrAdd(itemsByGroupName, matchingItem.GroupName, () => new Dictionary<string, string>());
                    var value = matchingItem.IsEncrypted
                        ? decryptedItems[long.Parse(matchingItem.Value)]
                        : matchingItem.Value;
                    valuesByItemName[matchingItem.Name] = value;
                }
                return itemGroupsByApplicationNr;
            }
        }

        private TValue GetOrAdd<TValue>(Dictionary<string, TValue> source, string key, Func<TValue> create)
        {
            if (!source.ContainsKey(key))
                source[key] = create();

            return source[key];
        }

        public static bool SetNonEncryptedItemComposable(IPreCreditContextExtended context, 
            string applicationNr, string itemName, string groupName, string value, string stepName)
        {
            CreditApplicationItem creditApplicationItem = context
                   .CreditApplicationItemsQueryable
                   .Where(x => x.ApplicationNr == applicationNr && x.GroupName == groupName && x.Name == itemName)
                   .SingleOrDefault();

            if (creditApplicationItem != null)
            {
                var logItem = context.FillInfrastructureFields(new CreditApplicationChangeLogItem
                {
                    ApplicationNr = applicationNr,
                    Name = itemName,
                    GroupName = groupName,
                    OldValue = creditApplicationItem.Value,
                    TransactionType = CreditApplicationChangeLogItem.TransactionTypeCode.Update.ToString()
                });
                context.AddCreditApplicationChangeLogItems(logItem);

                if (creditApplicationItem.Value != value)
                {
                    creditApplicationItem.Value = value;
                    return true;
                }
                else
                    return false;
            }
            else
            {
                context.AddCreditApplicationItems(context.FillInfrastructureFields(new CreditApplicationItem
                {
                    ApplicationNr = applicationNr,
                    AddedInStepName = stepName,
                    GroupName = groupName,
                    Name = itemName,
                    Value = value
                }));
                return true;
            }
        }
    }
}
