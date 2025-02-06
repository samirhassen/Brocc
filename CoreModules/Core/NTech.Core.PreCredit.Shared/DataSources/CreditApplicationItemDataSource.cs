using nPreCredit.Code.Services;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Core.PreCredit.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Datasources
{
    public class CreditApplicationItemDataSource : IApplicationDataSource
    {
        public const string DataSourceNameShared = "CreditApplicationItem";
        private readonly ICreditApplicationCustomEditableFieldsService creditApplicationCustomEditableFieldsService;
        private readonly IPreCreditContextFactoryService preCreditContextFactoryService;
        private readonly EncryptionService encryptionService;

        public CreditApplicationItemDataSource(ICreditApplicationCustomEditableFieldsService creditApplicationCustomEditableFieldsService, IPreCreditContextFactoryService preCreditContextFactoryService,
            EncryptionService encryptionService)
        {
            this.creditApplicationCustomEditableFieldsService = creditApplicationCustomEditableFieldsService;
            this.preCreditContextFactoryService = preCreditContextFactoryService;
            this.encryptionService = encryptionService;
        }

        public string DataSourceName => DataSourceNameShared;

        public bool IsSetDataSupported => true;

        public Dictionary<string, string> GetItems(string applicationNr, ISet<string> names, ApplicationDataSourceMissingItemStrategy missingItemStrategy, Action<string> observeMissingItems = null, Func<string, string> getDefaultValue = null, Action<string> observeChangedItems = null)
        {
            using (var context = preCreditContextFactoryService.CreateExtended())
            {
                if (names.Contains("*"))
                {
                    //Get all
                    names = new HashSet<string>();
                    foreach (var n in context.CreditApplicationItemsQueryable.Where(x => x.ApplicationNr == applicationNr).Select(x => new { x.GroupName, x.Name }).ToList())
                    {
                        names.Add($"{n.GroupName}.{n.Name}");
                    }
                    names.UnionWith(creditApplicationCustomEditableFieldsService.GetCustomizedItemNames(DataSourceNameShared));
                }
                else if (names.Any(x => x.EndsWith(".*")))
                {
                    //Get all in group
                    var groupName = names.First().Substring(0, names.First().IndexOf('.'));
                    names = new HashSet<string>();
                    foreach (var n in context.CreditApplicationItemsQueryable.Where(x => x.ApplicationNr == applicationNr && x.GroupName.StartsWith(groupName)).Select(x => new { x.GroupName, x.Name }).ToList())
                    {
                        names.Add($"{n.GroupName}.{n.Name}");
                    }
                    names.UnionWith(creditApplicationCustomEditableFieldsService.GetCustomizedItemNames(DataSourceNameShared).Where(x => x.StartsWith(groupName)));
                }

                //NOTE: This will overfetch slightly. If this turns out to hinder performance we can use dynamic expression trees to construct a query thats more like
                //      select ... from CreditApplicationItem i where i.ApplicationNr = <..> and ((i.groupname = @g1 and i.itemname = @i1) or (i.groupname = @g2 and i.itemname = @i2) or ...)
                //      item names are almost never reused across groups though so filtering in memory seems reasonable as opposed to taking on the massive extra complexity.
                var groupAndItemNames = names.Select(x => GroupNameAndItemNameFromName(x, applicationNr)).ToList();
                var result = new Dictionary<string, string>(groupAndItemNames.Count);

                var groupNames = groupAndItemNames.Select(x => x.Item1).Distinct().ToList();
                var itemNames = groupAndItemNames.Select(x => x.Item2).Distinct().ToList();

                var pre1 = context
                    .CreditApplicationItemsQueryable
                    .Where(x => x.ApplicationNr == applicationNr && groupNames.Contains(x.GroupName) && itemNames.Contains(x.Name));

                var pre2 = observeChangedItems == null
                    ? pre1
                        .Select(x => new FetchData
                        {
                            GroupName = x.GroupName,
                            Name = x.Name,
                            Value = x.Value,
                            IsEncrypted = x.IsEncrypted
                        })
                        .ToList()
                    : pre1
                        .Select(x => new FetchData
                        {
                            GroupName = x.GroupName,
                            Name = x.Name,
                            Value = x.Value,
                            IsEncrypted = x.IsEncrypted,
                            IsChanged = context.CreditApplicationChangeLogItemsQueryable.Any(y => y.ApplicationNr == x.ApplicationNr && y.GroupName == x.GroupName && y.Name == x.Name)
                        })
                        .ToList();

                var items = pre2
                    .ToDictionary(x => Tuple.Create(x.GroupName, x.Name));

                var changedNames = pre2
                    .Where(x => x.IsChanged.HasValue && x.IsChanged.Value)
                    .Select(x => NameFromGroupNameAndItemName(Tuple.Create(x.GroupName, x.Name)))
                    .ToHashSetShared();

                var idsToDecrypt = groupAndItemNames
                    .Select(x => (items.ContainsKey(x) && items[x].IsEncrypted) ? items[x] : null)
                    .Where(x => x != null)
                    .Select(x => long.Parse(x.Value))
                    .ToArray();

                IDictionary<long, string> decryptedValues = null;
                if (idsToDecrypt.Any())
                    decryptedValues = encryptionService.DecryptEncryptedValues(context, idsToDecrypt);

                foreach (var i in groupAndItemNames)
                {
                    if (items.ContainsKey(i))
                    {
                        var item = items[i];
                        if (item.IsEncrypted)
                        {
                            var key = long.Parse(item.Value);
                            if (decryptedValues.ContainsKey(key))
                                result[NameFromGroupNameAndItemName(i)] = decryptedValues[key];
                        }
                        else
                            result[NameFromGroupNameAndItemName(i)] = item.Value;
                    }
                }

                if (missingItemStrategy != ApplicationDataSourceMissingItemStrategy.Skip)
                {
                    foreach (var i in groupAndItemNames)
                    {
                        var name = NameFromGroupNameAndItemName(i);
                        if (!result.ContainsKey(name) && missingItemStrategy == ApplicationDataSourceMissingItemStrategy.ThrowException)
                            throw new NTechCoreWebserviceException($"Application {applicationNr}: Item '{NameFromGroupNameAndItemName(i)}' is missing in the datasource '{DataSourceName}'");
                        else if (!result.ContainsKey(name) && missingItemStrategy == ApplicationDataSourceMissingItemStrategy.UseDefaultValue)
                            result[NameFromGroupNameAndItemName(i)] = getDefaultValue(NameFromGroupNameAndItemName(i));
                    }
                }

                if (observeChangedItems != null)
                {
                    foreach (var i in groupAndItemNames)
                    {
                        var name = NameFromGroupNameAndItemName(i);
                        if (changedNames.Contains(name))
                            observeChangedItems(name);
                    }
                }

                return result;
            }
        }

        private class FetchData
        {
            public string GroupName { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
            public bool IsEncrypted { get; set; }
            public bool? IsChanged { get; set; }
        }

        private string NameFromGroupNameAndItemName(Tuple<string, string> names)
        {
            return $"{names.Item1}.{names.Item2}";
        }

        public static Tuple<string, string> GroupNameAndItemNameFromName(string name, string applicationNr)
        {
            var i = name.IndexOf('.');
            if (i < 0)
                throw new NTechCoreWebserviceException($"Application {applicationNr}: Invalid name '{name}'. The datasource '{DataSourceNameShared}' requires names on the format 'GroupName.ItemName'.");
            return Tuple.Create(name.Substring(0, i), name.Substring(i + 1));
        }

        public int? SetData(string applicationNr, string compoundItemName,
            bool isDelete, bool isMissingCurrentValue, string currentValue, string newValue,
            INTechCurrentUserMetadata currentUser)
        {
            using (var context = preCreditContextFactoryService.CreateExtended())
            {
                var groupAndItemName = GroupNameAndItemNameFromName(compoundItemName, applicationNr);
                var groupName = groupAndItemName.Item1;
                var itemName = groupAndItemName.Item2;

                var evt = new Lazy<CreditApplicationEvent>(() => context.CreateAndAddEvent(CreditApplicationEventCode.CreditApplicationItemEdited, applicationNr, null));

                var ci = ChangeCreditApplicationItem(applicationNr, isDelete, newValue, groupName, itemName, isMissingCurrentValue, currentValue, context, evt, false);

                if (ci != null)
                    context.SaveChanges();

                return ci?.Id;
            }
        }

        public static CreditApplicationChangeLogItem ChangeCreditApplicationItem(string applicationNr, bool isDelete, string newValue, string groupName, string itemName, bool isMissingCurrentValue, string currentValue, IPreCreditContextExtended context, Lazy<CreditApplicationEvent> evt, bool forceUpdate)
        {
            var wasChanged = false;

            var currentItem = context
                .CreditApplicationItemsQueryable
                .SingleOrDefault(x => x.ApplicationNr == applicationNr && x.GroupName == groupName && x.Name == itemName);

            if (isDelete && currentItem != null)
            {
                context.RemoveCreditApplicationItems(currentItem);
                wasChanged = true;
            }
            else if (currentValue != newValue || forceUpdate)
            {
                if (currentItem != null)
                {
                    currentItem.IsEncrypted = false;
                    currentItem.Value = newValue.Trim();
                    wasChanged = true;
                }
                else
                {
                    context.AddCreditApplicationItems(context.FillInfrastructureFields(new CreditApplicationItem
                    {
                        AddedInStepName = "EditApplicationData",
                        ApplicationNr = applicationNr,
                        GroupName = groupName,
                        IsEncrypted = false,
                        Name = itemName,
                        Value = newValue.Trim()
                    }));
                    wasChanged = true;
                }
            }

            if (wasChanged)
            {
                var ci = context.FillInfrastructureFields(new CreditApplicationChangeLogItem
                {
                    ApplicationNr = applicationNr,
                    GroupName = groupName,
                    Name = itemName,
                    TransactionType = isDelete
                        ? CreditApplicationChangeLogItem.TransactionTypeCode.Delete.ToString()
                        : (currentItem == null
                            ? CreditApplicationChangeLogItem.TransactionTypeCode.Insert.ToString()
                            : CreditApplicationChangeLogItem.TransactionTypeCode.Update.ToString()),
                    OldValue = isMissingCurrentValue ? "-" : currentValue,
                    EditEvent = evt.Value
                });
                context.AddCreditApplicationChangeLogItems(ci);

                return ci;
            }
            else
                return null;
        }
    }
}