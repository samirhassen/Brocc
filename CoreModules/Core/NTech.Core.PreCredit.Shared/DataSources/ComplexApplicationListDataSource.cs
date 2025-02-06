using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace nPreCredit.Code.Datasources
{
    public class ComplexApplicationListDataSource : IApplicationDataSource
    {
        public ComplexApplicationListDataSource(ICreditApplicationCustomEditableFieldsService creditApplicationCustomEditableFieldsService, IPreCreditContextFactoryService preCreditContextFactoryService)
        {
            this.creditApplicationCustomEditableFieldsService = creditApplicationCustomEditableFieldsService;
            this.preCreditContextFactoryService = preCreditContextFactoryService;
        }

        public string DataSourceName => DataSourceNameShared;

        public bool IsSetDataSupported => true;

        public const string DataSourceNameShared = "ComplexApplicationList";

        private readonly ICreditApplicationCustomEditableFieldsService creditApplicationCustomEditableFieldsService;
        private readonly IPreCreditContextFactoryService preCreditContextFactoryService;

        /// <summary>
        /// Format of names:
        /// [ListName]#[Nr|*]#[u|r|*]#[itemName|*]
        ///
        /// So fetch a specific unique item on the MortageObject lists first row
        /// MortageObject#1#u#priceAmount -> '150000'
        ///
        /// Fetch a specific repeated item on the MortageObject lists first row
        /// MortageObject#1#r#customerIds -> '["42","43"]' or '["42"] (so array even if only one)
        ///
        /// Fetch an entire row
        /// MortageObject#1#*#*
        ///
        /// Fetch all the rows
        /// MortageObject#*#*#*
        ///
        /// Fetch all prices on all rows
        /// MortageObject#*#u#priceAmount
        /// </summary>
        public Dictionary<string, string> GetItems(string applicationNr, ISet<string> names,
                ApplicationDataSourceMissingItemStrategy missingItemStrategy,
                Action<string> observeMissingItems = null,
                Func<string, string> getDefaultValue = null,
                Action<string> observeChangedItems = null)
        {
            var requests = new List<ItemRequest>();

            foreach (var name in names)
            {
                if (TryParseCompoundName(name, out var listNameFilter, out var nrFilter, out var isRepeatableFilter, out var itemNameFilter, out var isFullySpecifiedItem))
                {
                    List<CreditApplicationCustomEditableFieldsModel.FieldModel> fieldModels;
                    if (itemNameFilter == null)
                    {
                        //All items in the list
                        fieldModels = creditApplicationCustomEditableFieldsService
                            .GetCustomizedItemNames(DataSourceNameShared)
                            .Select(x => creditApplicationCustomEditableFieldsService.GetFieldModel(DataSourceNameShared, x))
                            .ToList();

                        if (isRepeatableFilter.HasValue)
                            fieldModels = fieldModels.Where(x => (x.CustomData["isRepeatable"] == "true") == isRepeatableFilter.Value).ToList();

                        if (listNameFilter != null)
                            fieldModels = fieldModels.Where(x => x.CustomData["listName"] == listNameFilter).ToList();

                        if (listNameFilter != null && !nrFilter.HasValue && !fieldModels.Any(x => x.CustomData["itemName"] == "exists"))
                        {
                            //Inject exists-item so we dont have to declare these on every single list when trying to find which rows already exist
                            fieldModels.Add(CreditApplicationCustomEditableFieldsService.CreateComplexApplicationListFieldModel(
                                $"{listNameFilter}#*#u#exists", "exists", listNameFilter, false,
                                dataType: "string",
                                editorType: "dropdownRaw",
                                labelText: "exists",
                                dropdownOptionsAndTexts: Tuple.Create(new List<string> { "true", "false" }, new List<string> { "yes", "no" })));
                        }
                    }
                    else
                    {
                        var fm = creditApplicationCustomEditableFieldsService.GetFieldModel(DataSourceNameShared, name);
                        fieldModels = new List<CreditApplicationCustomEditableFieldsModel.FieldModel> { fm };
                    }

                    foreach (var fm in fieldModels)
                    {
                        requests.Add(new ItemRequest
                        {
                            FieldModel = fm,
                            IsRepeatable = fm.CustomData["isRepeatable"] == "true",
                            ListNameExact = listNameFilter,
                            ItemNameExact = itemNameFilter ?? GetItemNameFromCompoundName(fm.ItemName),
                            NrFilter = nrFilter,
                            IsConcreteItem = nrFilter.HasValue
                        });
                    }
                }
            }

            return GetItemsExact(applicationNr, requests, missingItemStrategy, observeMissingItems: observeMissingItems, getDefaultValue: getDefaultValue, observeChangedItems: observeChangedItems);
        }

        public Dictionary<string, List<int>> GetNrs(string applicationNr, List<string> listNames)
        {
            var items = GetItems(applicationNr,
                listNames.Select(x => $"{x}#*#u#exists").ToHashSetShared(),
                ApplicationDataSourceMissingItemStrategy.Skip);
            var result = new Dictionary<string, List<int>>();
            foreach (var listName in listNames)
                result[listName] = new List<int>();

            foreach (var i in items.Where(x => x.Value == "true"))
            {
                if (!TryParseCompoundName(i.Key, out var listName, out var nr, out var _, out var __, out var isFullySpecified))
                    throw new NTechCoreWebserviceException("Invalid item");
                else if (!isFullySpecified)
                    throw new NTechCoreWebserviceException("Not fully specified item");
                result[listName].Add(nr.Value);
            }

            return result;
        }

        private Dictionary<string, string> GetItemsExact(string applicationNr,
            List<ItemRequest> items,
            ApplicationDataSourceMissingItemStrategy missingItemStrategy,
            Action<string> observeMissingItems = null,
            Func<string, string> getDefaultValue = null,
            Action<string> observeChangedItems = null)
        {
            using (var context = preCreditContextFactoryService.CreateExtended())
            {
                //NOTE: If this ever becomes slow it can be optimized for some or all paths to only fetch a subset based on the names
                //      but the logic is way cleaner doing it in memory
                var applicationItemsPre = context
                    .ComplexApplicationListItemsQueryable
                    .Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => new { x, FirstEventId = x.Application.Events.Select(y => (int?)y.Id).FirstOrDefault() })
                    .ToList();

                var applicationItems = applicationItemsPre.Select(x => x.x).ToList();
                var firstApplicationEventId = applicationItemsPre.FirstOrDefault()?.FirstEventId;

                var result = new Dictionary<string, string>();

                foreach (var item in items)
                {
                    var matchingItems = applicationItems
                        .Where(x =>
                            x.ListName == item.ListNameExact
                            && x.ItemName == item.ItemNameExact
                            && x.IsRepeatable == item.IsRepeatable
                            && (!item.NrFilter.HasValue || x.Nr == item.NrFilter.Value))
                        .ToList();

                    var matchingItemGroups = matchingItems.GroupBy(x => x.Nr);

                    if (matchingItems.Count == 0)
                    {
                        if (item.IsConcreteItem)
                        {
                            var exactName = CreateCompoundName(item.ListNameExact, item.NrFilter.Value, item.IsRepeatable, item.ItemNameExact);
                            observeMissingItems?.Invoke(exactName);
                            if (missingItemStrategy == ApplicationDataSourceMissingItemStrategy.ThrowException)
                                throw new NTechCoreWebserviceException($"Application {applicationNr}: Item '{exactName}' is missing in the datasource '{DataSourceName}'") { ErrorCode = "missingItem", IsUserFacing = true };
                            else if (missingItemStrategy == ApplicationDataSourceMissingItemStrategy.UseDefaultValue)
                                result[exactName] = getDefaultValue(exactName);
                        }
                    }
                    foreach (var g in matchingItemGroups)
                    {
                        var exactName = CreateCompoundName(item.ListNameExact, g.Key, item.IsRepeatable, item.ItemNameExact);
                        if (item.IsRepeatable)
                            result[exactName] = JsonConvert.SerializeObject(g.Select(x => x.ItemValue).ToArray());
                        else
                        {
                            var v = g.First();
                            if (firstApplicationEventId.HasValue && v.LatestChangeEventId > firstApplicationEventId.Value)
                            {
                                //Last change was after the application was created
                                observeChangedItems?.Invoke(exactName);
                            }
                            result[exactName] = v.ItemValue;
                        }
                    }
                }

                return result;
            }
        }

        private class ItemRequest
        {
            public string ListNameExact { get; set; }
            public string ItemNameExact { get; set; }
            public CreditApplicationCustomEditableFieldsModel.FieldModel FieldModel { get; set; }
            public int? NrFilter { get; set; }
            public bool IsRepeatable { get; set; }
            public bool IsConcreteItem { get; set; }
        }

        public static string CreateCompoundName(string listName, int nr, bool isRepeatable, string itemName)
        {
            return $"{listName}#{nr}#{(isRepeatable ? "r" : "u")}#{itemName}";
        }

        public static string GetItemNameFromCompoundName(string compoundName)
        {
            if (!TryParseCompoundName(compoundName, out var _, out var __, out var ___, out var itemName, out var ____))
                throw new Exception("Invalid name: " + compoundName);
            if (itemName == null)
                throw new Exception("Compound name has no item name: " + compoundName);
            return itemName;
        }

        public static FullySpecifiedCompoundNameModel ParseFullySpecifiedCompoundName(string compoundName)
        {
            if (!TryParseCompoundName(compoundName, out var ln, out var nr, out var rep, out var itemName, out var isSpec))
            {
                throw new Exception("Invalid fully specified compound name");
            }
            return new FullySpecifiedCompoundNameModel
            {
                RowNr = nr.Value,
                IsRepeatable = rep.Value,
                ItemName = itemName,
                ListName = ln
            };
        }

        public class FullySpecifiedCompoundNameModel
        {
            public int RowNr { get; set; }
            public string ListName { get; set; }
            public string ItemName { get; set; }
            public bool IsRepeatable { get; set; }
        }

        public static bool TryParseCompoundName(string compoundName, out string listNameFilter, out int? nrFilter, out bool? isRepeatableFilter, out string itemNameFilter, out bool isFullySpecifiedItem)
        {
            isFullySpecifiedItem = false;
            listNameFilter = null;
            nrFilter = null;
            isRepeatableFilter = null;
            itemNameFilter = null;

            if (string.IsNullOrWhiteSpace(compoundName))
            {
                return false;
            }

            var m = Regex.Match(compoundName, @"^([\w]+)#([\d\*]+)#([ur\*])#([\w\*]+)$");
            if (!m.Success)
            {
                return false;
            }

            listNameFilter = m.Groups[1].Value;

            var nrRaw = m.Groups[2].Value;
            if (nrRaw == "*")
                nrFilter = new int?();
            else if (!int.TryParse(nrRaw, out var nrInt))
                return false;
            else
                nrFilter = nrInt;

            var rep = m.Groups[3].Value;
            if (rep == "u")
                isRepeatableFilter = false;
            else if (rep == "r")
                isRepeatableFilter = true;
            else if (rep == "*")
                isRepeatableFilter = null;
            else
                return false;

            var iName = m.Groups[4].Value;
            if (iName == "*")
                itemNameFilter = null;
            else
                itemNameFilter = iName;

            isFullySpecifiedItem = listNameFilter != null && nrFilter.HasValue && isRepeatableFilter.HasValue && itemNameFilter != null;

            return true;
        }

        public int? SetData(string applicationNr, string compoundItemName, bool isDelete, bool isMissingCurrentValue, string currentValue, string newValue, INTechCurrentUserMetadata currentUser)
        {
            if (!TryParseCompoundName(compoundItemName, out var listName, out var nr, out var isRepeatable, out var itemName, out var isFullySpecified))
                throw new NTechCoreWebserviceException("Invalid name: " + compoundItemName)
                {
                    ErrorCode = "invalidCompoundItemName",
                    IsUserFacing = true,
                    ErrorHttpStatusCode = 400
                };

            using (var context = preCreditContextFactoryService.CreateExtended())
            {
                List<ComplexApplicationListOperation> changes;
                if (!isDelete)
                {
                    if (!isFullySpecified)
                        throw new NTechCoreWebserviceException("Item must be fully specified: " + compoundItemName)
                        {
                            ErrorCode = "itemNotFullySpecified",
                            IsUserFacing = true,
                            ErrorHttpStatusCode = 400
                        };
                    string uniqueValue = null;
                    List<string> repeatedValue = null;
                    if (isRepeatable.Value)
                    {
                        repeatedValue = JsonConvert.DeserializeObject<List<string>>(newValue);
                    }
                    else
                    {
                        uniqueValue = newValue;
                    }

                    changes = new List<ComplexApplicationListOperation> {
                        new ComplexApplicationListOperation
                        {
                            ApplicationNr = applicationNr,
                            IsDelete = isDelete,
                            ItemName = itemName,
                            ListName = listName,
                            Nr = nr.Value,
                            RepeatedValue = repeatedValue,
                            UniqueValue =  uniqueValue
                        }
                    };
                }
                else
                {
                    var currentItems = context.ComplexApplicationListItemsQueryable.Where(x => x.ApplicationNr == applicationNr);

                    //out var nr, out var isRepeatable, out var itemName, out var isFullySpecified
                    var hasMinimalFilters = false;
                    if (!string.IsNullOrWhiteSpace(listName))
                    {
                        currentItems = currentItems.Where(x => x.ListName == listName);
                        hasMinimalFilters = true;
                    }
                    if (nr.HasValue)
                    {
                        currentItems = currentItems.Where(x => x.Nr == nr.Value);
                        hasMinimalFilters = true;
                    }
                    if (isRepeatable.HasValue)
                    {
                        currentItems = currentItems.Where(x => x.IsRepeatable == isRepeatable.Value);
                        //NOTE: We dont allow only this filter as it's hard to come up with a case where this would not be a mistake ... if you come across one. Change this to be allowed
                    }
                    if (!string.IsNullOrWhiteSpace(itemName))
                    {
                        currentItems = currentItems.Where(x => x.ItemName == itemName);
                        hasMinimalFilters = true;
                    }
                    if (!hasMinimalFilters)
                        throw new NTechCoreWebserviceException("Deletes require at least one filter on listName, nr or itemName: " + compoundItemName)
                        {
                            ErrorCode = "deleteRequiresMoreFilters",
                            IsUserFacing = true,
                            ErrorHttpStatusCode = 400
                        };

                    changes = currentItems.Select(x => new ComplexApplicationListOperation
                    {
                        ApplicationNr = x.ApplicationNr,
                        IsDelete = true,
                        ItemName = x.ItemName,
                        ListName = x.ListName,
                        Nr = x.Nr
                    }).ToList();
                }

                CreditApplicationEvent evt = null;

                var r = ComplexApplicationListService.ChangeListComposable(changes, context, observeEvents: x => evt = x);

                if (r)
                {
                    context.SaveChanges();
                }

                return evt?.Id;
            }
        }

        public class ComplexListRow
        {
            public string ListName { get; set; }
            public int Nr { get; set; }
            public Dictionary<string, string> UniqueItems { get; set; }
            public Dictionary<string, List<string>> RepeatedItems { get; set; }

            public string GetCompoundName(string itemName, bool isUnique)
            {
                return $"{ListName}#{Nr}#{(isUnique ? "u" : "r")}#{itemName}";
            }

            public List<T> GetRepeatedItem<T>(string name, Func<string, T> parse)
            {
                return RepeatedItems?.Opt(name)?.Where(x => x != null)?.Select(parse)?.ToList();
            }
        }

        public static List<ComplexListRow> ToRows(ApplicationDataSourceResult.DataSourceResult complexListResult)
        {
            return complexListResult
                .ItemNames()
                .Select(x => new { RawName = x, ParsedName = ParseFullySpecifiedCompoundName(x) })
                .GroupBy(x => new
                {
                    x.ParsedName.ListName,
                    x.ParsedName.RowNr
                })
                .Select(x => new ComplexListRow
                {
                    ListName = x.Key.ListName,
                    Nr = x.Key.RowNr,
                    UniqueItems = x
                        .Where(y => !y.ParsedName.IsRepeatable && complexListResult.Item(y.RawName).Exists)
                        .ToDictionary(y => y.ParsedName.ItemName, y => complexListResult.Item(y.RawName).StringValue.Required),
                    RepeatedItems = x
                        .Where(y => y.ParsedName.IsRepeatable && complexListResult.Item(y.RawName).Exists)
                        .ToDictionary(y => y.ParsedName.ItemName, y => JsonConvert.DeserializeObject<List<string>>(complexListResult.Item(y.RawName).StringValue.Required)),
                })
                .ToList();
        }
    }
}