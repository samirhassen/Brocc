using NTech;
using NTech.Core;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.PreCredit.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public interface IComplexApplicationListService : IComplexApplicationListReadOnlyService
    {
        bool ChangeList(List<ComplexApplicationListOperation> requestItems);
    }

    public interface IComplexApplicationListReadOnlyService
    {
        Dictionary<string, ComplexApplicationList> GetListsForApplication(string applicationNr, bool emptyListIfNotExists, IPreCreditContextExtended optionalCompositionContext, params string[] listNames);
    }

    public class ComplexApplicationListReadOnlyService : IComplexApplicationListReadOnlyService
    {
        protected readonly ICoreClock clock;
        protected readonly PreCreditContextFactory preCreditContextFactory;

        public ComplexApplicationListReadOnlyService(ICoreClock clock, PreCreditContextFactory preCreditContextFactory)
        {
            this.clock = clock;
            this.preCreditContextFactory = preCreditContextFactory;
        }

        public Dictionary<string, ComplexApplicationList> GetListsForApplication(string applicationNr, bool emptyListIfNotExists, IPreCreditContextExtended optionalCompositionContext, params string[] listNames)
        {
            Func<IPreCreditContextExtended, Dictionary<string, ComplexApplicationList>> withContext = c =>
            {
                var items = c.ComplexApplicationListItemsQueryable.Where(x => x.ApplicationNr == applicationNr && listNames.Contains(x.ListName)).ToList();
                var result = ComplexApplicationList.CreateListsFromFlattenedItems(items);
                if (emptyListIfNotExists)
                {
                    foreach (var listName in listNames)
                    {
                        if (!result.ContainsKey(listName))
                            result[listName] = ComplexApplicationList.CreateEmpty(listName);
                    }
                }
                return result;
            };
            if (optionalCompositionContext == null)
                using (var context = preCreditContextFactory.CreateContext())
                {
                    return withContext(context);
                }
            else
                return withContext(optionalCompositionContext);
        }
    }

    public class ComplexApplicationListService : ComplexApplicationListReadOnlyService, IComplexApplicationListService
    {
        private readonly INTechCurrentUserMetadata currentUser;

        public ComplexApplicationListService(INTechCurrentUserMetadata currentUser, ICoreClock clock, PreCreditContextFactory preCreditContextFactory) : base(clock, preCreditContextFactory)
        {
            this.currentUser = currentUser;
        }

        public bool ChangeList(List<ComplexApplicationListOperation> requestItems)
        {
            if (requestItems == null || requestItems.Count == 0)
                return false;

            using (var context = preCreditContextFactory.CreateContext())
            {
                var wasChanged = ChangeListComposable(requestItems, context);
                if (wasChanged)
                {
                    context.SaveChanges();
                }
                return wasChanged;
            }
        }

        public static bool ChangeListComposable(List<ComplexApplicationListOperation> requestItems, IPreCreditContextExtended context, Action<CreditApplicationEvent> observeEvents = null, Func<string, CreditApplicationEvent> getEvent = null)
        {
            var applicationNrs = requestItems.Select(x => x.ApplicationNr).Distinct().ToList();
            var allExistingItems = context.ComplexApplicationListItemsQueryable.Where(x => applicationNrs.Contains(x.ApplicationNr)).ToList();
            var result = ChangeListTestable(allExistingItems, requestItems);
            if (!(result.DeletedExistingItems.Any() || result.AddedNewItems.Any() || result.UpdatedExistingItems.Any()))
            {
                return false;
            }

            var evts = new Dictionary<string, CreditApplicationEvent>();
            getEvent = getEvent ?? (x =>
              {
                  if (!evts.ContainsKey(x))
                  {
                      var evt = context.CreateAndAddEvent(CreditApplicationEventCode.ComplexApplicationListChange, x, null);
                      evts[x] = evt;
                      observeEvents?.Invoke(evt);
                  }
                  return evts[x];
              });

            foreach (var deletedItem in result.DeletedExistingItems)
            {
                var evt = getEvent(deletedItem.ApplicationNr);
                var i = new HComplexApplicationListItem
                {
                    ApplicationNr = deletedItem.ApplicationNr,
                    ChangeEvent = evt,
                    IsRepeatable = deletedItem.IsRepeatable,
                    ItemName = deletedItem.ItemName,
                    ItemValue = deletedItem.ItemValue,
                    ListName = deletedItem.ListName,
                    Nr = deletedItem.Nr,
                    ChangeTypeCode = "d"
                };
                context.AddHComplexApplicationListItem(i);
                context.RemoveComplexApplicationListItem(deletedItem);
            }
            foreach (var updatedItem in result.UpdatedExistingItems)
            {
                var evt = getEvent(updatedItem.ApplicationNr);
                //NOTE: Value was already changed by SetTestable
                updatedItem.LatestChangeEvent = evt;
                context.AddHComplexApplicationListItem(new HComplexApplicationListItem
                {
                    ApplicationNr = updatedItem.ApplicationNr,
                    ChangeEvent = evt,
                    IsRepeatable = updatedItem.IsRepeatable,
                    ItemName = updatedItem.ItemName,
                    ItemValue = updatedItem.ItemValue,
                    ListName = updatedItem.ListName,
                    Nr = updatedItem.Nr,
                    ChangeTypeCode = "u"
                });
            }
            foreach (var insertedItem in result.AddedNewItems)
            {
                var evt = getEvent(insertedItem.ApplicationNr);
                insertedItem.CreatedByEvent = evt;
                insertedItem.LatestChangeEvent = evt;
                context.AddComplexApplicationListItem(insertedItem);
                context.AddHComplexApplicationListItem(new HComplexApplicationListItem
                {
                    ApplicationNr = insertedItem.ApplicationNr,
                    ChangeEvent = evt,
                    IsRepeatable = insertedItem.IsRepeatable,
                    ItemName = insertedItem.ItemName,
                    ItemValue = insertedItem.ItemValue,
                    ListName = insertedItem.ListName,
                    Nr = insertedItem.Nr,
                    ChangeTypeCode = "i"
                });
            }

            return true;
        }

        public static List<ComplexApplicationListOperation> CreateDeleteOperations(List<ComplexApplicationListItem> existingItems) =>
            (existingItems ?? new List<ComplexApplicationListItem>()).Select(x => new ComplexApplicationListOperation
            {
                ApplicationNr = x.ApplicationNr,
                IsDelete = true,
                ListName = x.ListName,
                Nr = x.Nr,
                ItemName = x.ItemName
            }).ToList();

        public static List<ComplexApplicationListOperation> CreateDeleteRowOperations(string applicationNr, string listName, int nr, IPreCreditContextExtended context)
        {
            return context
                .ComplexApplicationListItemsQueryable
                .Where(x => x.ApplicationNr == applicationNr && x.ListName == listName && x.Nr == nr)
                .Select(x => x.ItemName)
                .Distinct()
                .ToList()
                .Select(x => new ComplexApplicationListOperation
                {
                    ApplicationNr = applicationNr,
                    IsDelete = true,
                    ListName = listName,
                    Nr = nr,
                    ItemName = x
                })
                .ToList();
        }

        public static bool SetUniqueItems(string applicationNr, string listName, int nr,
            Dictionary<string, string> namesAndValues, IPreCreditContextExtended context, Action<CreditApplicationEvent> observeEvents = null,
            Func<string, CreditApplicationEvent> getEvent = null)
        {
            var changes = namesAndValues.Select(x => new ComplexApplicationListOperation
            {
                ApplicationNr = applicationNr,
                IsDelete = false,
                ListName = listName,
                ItemName = x.Key,
                Nr = nr,
                UniqueValue = x.Value
            }).ToList();
            return ChangeListComposable(changes, context, observeEvents: observeEvents, getEvent: getEvent);
        }

        public static (Dictionary<string, string> UniqueItems, Dictionary<string, List<string>> RepeatedItems) GetListRow(string applicationNr, string listName, int nr, IPreCreditContextExtended context)
        {
            var items = context
                .ComplexApplicationListItemsQueryable
                .Where(x => x.ApplicationNr == applicationNr && x.ListName == listName && x.Nr == nr)
                .Select(x => new
                {
                    x.ItemName,
                    x.ItemValue,
                    x.IsRepeatable
                })
                .ToList();
            return (
                UniqueItems: items.Where(x => !x.IsRepeatable).ToDictionary(x => x.ItemName, x => x.ItemValue),
                RepeatedItems: items.Where(x => x.IsRepeatable).GroupBy(x => x.ItemName).ToDictionary(x => x.Key, x => x.Select(y => y.ItemValue).ToList()));
        }

        public static bool SetSingleUniqueItem(string applicationNr, string listName, string itemName, int nr, string value, IPreCreditContextExtended context, Action<CreditApplicationEvent> observeEvents = null, Func<string, CreditApplicationEvent> getEvent = null)
        {
            return ChangeListComposable(new List<ComplexApplicationListOperation>
            {
                new ComplexApplicationListOperation
                {
                    ApplicationNr = applicationNr,
                    IsDelete = false,
                    ListName = listName,
                    ItemName = itemName,
                    Nr = nr,
                    UniqueValue = value
                }
            }, context, observeEvents: observeEvents, getEvent: getEvent);
        }

        //Note exposed in the inteface, just used for unit testing
        public static ChangeListResult ChangeListTestable(
            List<ComplexApplicationListItem> allExistingItems,
            List<ComplexApplicationListOperation> requestItems)
        {
            var result = new ChangeListResult
            {
                AddedNewItems = new List<ComplexApplicationListItem>(),
                DeletedExistingItems = new List<ComplexApplicationListItem>(),
                UpdatedExistingItems = new List<ComplexApplicationListItem>()
            };

            foreach (var item in requestItems)
            {
                if (!Booleans.ExactlyOneIsTrue(item.RepeatedValue != null, item.UniqueValue != null, item.IsDelete))
                {
                    throw new NTechCoreWebserviceException("Exactly one of RepeatedValue, UniqueValue and IsDelete can be used")
                    {
                        ErrorCode = "uniqueRepatedOrDelete",
                        IsUserFacing = true,
                        ErrorHttpStatusCode = 400
                    };
                }

                var existingItems = allExistingItems
                    .Where(x => x.ApplicationNr == item.ApplicationNr && x.ListName == item.ListName && x.Nr == item.Nr && x.ItemName == item.ItemName)
                    .OrderBy(x => x.Id) //do not remove, preserves the order of repeated items
                    .ToList();

                if (item.UniqueValue != null)
                {
                    var existingItem = existingItems.FirstOrDefault();
                    if (existingItem == null)
                    {
                        result.AddedNewItems.Add(new ComplexApplicationListItem
                        {
                            ApplicationNr = item.ApplicationNr,
                            ListName = item.ListName,
                            Nr = item.Nr,
                            IsRepeatable = false,
                            ItemName = item.ItemName,
                            ItemValue = item.UniqueValue
                        });
                    }
                    else if (existingItem.ItemValue != item.UniqueValue)
                    {
                        existingItem.ItemValue = item.UniqueValue;
                        result.UpdatedExistingItems.Add(existingItem);
                        //Next part should not be possible but this will at least make us recover from mistakes
                        result.DeletedExistingItems.AddRange(existingItems.Skip(1));
                    }
                }
                else if (item.RepeatedValue != null)
                {
                    var commonPrefixLength = 0; //This part of the list is preserved the rest is deleted and readded to preserve order by id
                    for (var i = 0; i < item.RepeatedValue.Count; i++)
                    {
                        if (existingItems.Count > i && existingItems[i].ItemValue == item.RepeatedValue[i])
                            commonPrefixLength++;
                        else
                            break;
                    }
                    if (existingItems.Count > commonPrefixLength)
                    {
                        result.DeletedExistingItems.AddRange(existingItems.Skip(commonPrefixLength));
                    }
                    if (item.RepeatedValue.Count > commonPrefixLength)
                        result.AddedNewItems.AddRange(item.RepeatedValue.Skip(commonPrefixLength).Select(x => new ComplexApplicationListItem
                        {
                            ApplicationNr = item.ApplicationNr,
                            ListName = item.ListName,
                            Nr = item.Nr,
                            IsRepeatable = true,
                            ItemName = item.ItemName,
                            ItemValue = x
                        }));
                }
                else if (item.IsDelete)
                {
                    result.DeletedExistingItems.AddRange(existingItems);
                }
                else
                    throw new NotImplementedException();
            }

            return result;
        }

        public static List<ComplexApplicationListOperation> CreateReplaceRowOperations(
            List<ComplexApplicationListItem> existingItems,
            string applicationNr,
            string listName,
            int rowNr,
            Dictionary<string, string> newUniqueItems)
        {
            var changes = new List<ComplexApplicationListOperation>();
            void AddUniqueValueChange(string itemName, string value, bool isDelete)
            {
                changes.Add(new ComplexApplicationListOperation
                {
                    ApplicationNr = applicationNr,
                    ListName = listName,
                    Nr = rowNr,
                    ItemName = itemName,
                    UniqueValue = isDelete ? null : value,
                    RepeatedValue = null,
                    IsDelete = isDelete
                });
            }
            var updatedNames = new HashSet<string>();
            foreach (var existingItem in (existingItems ?? new List<ComplexApplicationListItem>()))
            {
                if (existingItem.ApplicationNr != applicationNr || existingItem.ListName != listName || existingItem.Nr != rowNr)
                    throw new Exception("Invalid existingItems. Items from a different list or row are present");
                if (existingItem.IsRepeatable)
                    throw new Exception("Repeated items are not supported yet"); //NOTE: Alternatively they could just be removed here
                if (newUniqueItems.ContainsKey(existingItem.ItemName))
                {
                    AddUniqueValueChange(existingItem.ItemName, newUniqueItems[existingItem.ItemName], false);
                    updatedNames.Add(existingItem.ItemName);
                }
                else
                    AddUniqueValueChange(existingItem.ItemName, null, true);
            }
            foreach (var insertName in newUniqueItems.Keys.Except(updatedNames))
            {
                AddUniqueValueChange(insertName, newUniqueItems[insertName], false);
            }
            return changes;
        }

        public class ChangeListResult
        {
            public List<ComplexApplicationListItem> DeletedExistingItems { get; set; }
            public List<ComplexApplicationListItem> AddedNewItems { get; set; }
            public List<ComplexApplicationListItem> UpdatedExistingItems { get; set; }
        }

        /// <summary>
        /// Used for things like the list HouseholdChildren that just represents an array like this:
        /// [{ ageInYears, sharedCustody }, { ageInYears, sharedCustody }, ....]
        /// 
        /// The Nr here is not hugely important other than to impose the order of items.
        /// 
        /// The easiest way to save an edit is to delete the entire list and then add the items back but that will create
        /// a super messy history so this method is used to find a smaller set of changes. 
        /// 
        /// Like if you just edit say the age of one row that will just be one edit in the history.
        /// 
        /// We don't allow repeated items here just to simplify. It can probably be added by throwing more code at the problem.
        /// </summary>        
        public static List<ComplexApplicationListOperation> SynchListTreatedAsArray(string applicationNr, string listName, List<ComplexApplicationListItem> currentListItems, List<Dictionary<string, string>> newRows)
        {
            var actualNewRows = newRows.Select(x =>
            {
                var d = new Dictionary<string, string>(x.Count + 1);
                d["exists"] = "true";
                foreach (var kvp in x)
                {
                    if (kvp.Value != null)
                        d[kvp.Key] = kvp.Value;
                }
                return d;
            }).ToList();
            return SynchListTreatedAsArrayInternal(applicationNr, listName, currentListItems, actualNewRows);
        }

        private static List<ComplexApplicationListOperation> SynchListTreatedAsArrayInternal(string applicationNr, string listName, List<ComplexApplicationListItem> currentListItems, List<Dictionary<string, string>> newRows)
        {
            currentListItems = currentListItems ?? new List<ComplexApplicationListItem>();
            if (currentListItems.Any(x => x.ListName != listName || x.ApplicationNr != applicationNr || x.IsRepeatable))
                throw new Exception("Invalid list");

            var changes = new List<ComplexApplicationListOperation>();

            var existingRowsByNr = currentListItems.GroupBy(x => x.Nr).OrderBy(x => x.Key).Select(x => new
            {
                Nr = x.Key,
                Items = x.ToDictionary(y => y.ItemName, y => y.ItemValue)
            }).ToDictionary(x => x.Nr, x => x);

            void AddChange(int nr, string name, string value, bool isDelete) => changes.Add(new ComplexApplicationListOperation
            {
                ApplicationNr = applicationNr,
                ListName = listName,
                Nr = nr,
                IsDelete = isDelete,
                ItemName = name,
                UniqueValue = value
            });

            var newNrs = new HashSet<int>();
            for (var i = 0; i < newRows.Count; i++)
            {
                var newRow = newRows[i];
                var newNr = i + 1;
                var existingRow = ComplexApplicationList.Opt(existingRowsByNr, newNr);
                var namesInNewRow = new HashSet<string>();
                foreach (var keyAndValue in newRow)
                {
                    namesInNewRow.Add(keyAndValue.Key);
                    AddChange(newNr, keyAndValue.Key, keyAndValue.Value, false);
                }
                if (existingRow != null)
                {
                    foreach (var existingNameToRemove in existingRow.Items.Keys.Except(namesInNewRow))
                    {
                        AddChange(newNr, existingNameToRemove, null, true);
                    }
                }
                newNrs.Add(newNr);
            }
            foreach (var nrToRemove in existingRowsByNr.Keys.Except(newNrs))
            {
                var existingRowToRemove = existingRowsByNr[nrToRemove];
                foreach (var nameToRemove in existingRowToRemove.Items.Select(x => x.Key))
                {
                    AddChange(existingRowToRemove.Nr, nameToRemove, null, true);
                }
            }
            return changes;
        }
    }

    public class ComplexApplicationListOperation
    {
        public string ApplicationNr { get; set; }
        public string ListName { get; set; }
        public int Nr { get; set; }
        public string ItemName { get; set; }
        public string UniqueValue { get; set; }
        public List<string> RepeatedValue { get; set; }
        public bool IsDelete { get; set; }

        public static List<ComplexApplicationListOperation> CreateNewRow(
            string applicationNr,
            string listName,
            int nr,
            Dictionary<string, string> uniqueValues = null,
            Dictionary<string, List<string>> repeatedValues = null)
        {
            var ops = new List<ComplexApplicationListOperation>();
            if (uniqueValues != null)
            {
                foreach (var un in uniqueValues)
                    ops.Add(new ComplexApplicationListOperation { ApplicationNr = applicationNr, ListName = listName, Nr = nr, ItemName = un.Key, UniqueValue = un.Value });
            }
            if (repeatedValues != null)
            {
                foreach (var rn in repeatedValues)
                    ops.Add(new ComplexApplicationListOperation { ApplicationNr = applicationNr, ListName = listName, Nr = nr, ItemName = rn.Key, RepeatedValue = rn.Value });
            }
            return ops;
        }
    }
}