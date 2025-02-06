using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class ComplexApplicationList
    {
        private ComplexApplicationList(string listName, List<ComplexApplicationListItemBase> flattendItems)
        {
            rows = new Dictionary<int, Row>();
            foreach (var nrGroup in flattendItems.GroupBy(x => x.Nr))
            {
                rows[nrGroup.Key] = new Row(listName, nrGroup.Key, nrGroup.ToList());
            }

            ListName = listName;
        }

        public static TValue Opt<TKey, TValue>(Dictionary<TKey, TValue> source, TKey name) where TValue : class => source == null
            ? null
            : (source.ContainsKey(name) ? source[name] : null);

        public string ListName { get; }

        private Dictionary<int, Row> rows;

        public Row GetRow(int nr, bool emptyRowOnNotExists) => Opt(rows, nr) ??
                   (emptyRowOnNotExists ? new Row(ListName, nr, new List<ComplexApplicationListItemBase>()) : null);

        public List<int> GetRowNumbers() => rows.Keys.OrderBy(x => x).ToList();
        public List<Row> GetRows() => GetRowNumbers().Select(x => GetRow(x, true)).ToList();

        /// <summary>
        /// Assumed that all the items are for the same application nr.
        /// </summary>
        public static Dictionary<string, ComplexApplicationList> CreateListsFromFlattenedItems<TItem>(List<TItem> flattendItems) where TItem : ComplexApplicationListItemBase
        {
            var result = new Dictionary<string, ComplexApplicationList>();
            foreach (var listGroup in flattendItems.GroupBy(x => x.ListName))
            {
                result[listGroup.Key] = new ComplexApplicationList(listGroup.Key, listGroup.Cast<ComplexApplicationListItemBase>().ToList());
            }
            return result;
        }

        public static ComplexApplicationList CreateListFromFlattenedItems<TItem>(string listName, List<TItem> flattendItems) where TItem : ComplexApplicationListItemBase
        {
            var lists = CreateListsFromFlattenedItems(flattendItems?.Where(x => x.ListName == listName)?.ToList());
            if (lists.Count == 0)
                return CreateEmpty(listName);
            else if (lists.Count > 1)
                throw new Exception("Expected items from exactly one list");
            else if (!lists.ContainsKey(listName))
                throw new Exception("Expected items from only the list " + listName);
            else
                return lists[listName];
        }

        public static ComplexApplicationList CreateEmpty(string listName) => new ComplexApplicationList(listName, new List<ComplexApplicationListItemBase>());

        public List<ComplexApplicationListItemBase> Flatten()
        {
            var items = new List<ComplexApplicationListItemBase>();
            foreach (var row in GetRows())
            {
                items.AddRange(row.GetUniqueItemNames().Select(itemName => new ComplexApplicationListItemBase
                {
                    IsRepeatable = false,
                    ItemName = itemName,
                    ItemValue = row.GetUniqueItem(itemName),
                    ListName = ListName,
                    Nr = row.Nr
                }));
                items.AddRange(row.GetRepeatedItemNames().SelectMany(itemName => row.GetRepeatedItems(itemName).Select(itemValue => new ComplexApplicationListItemBase
                {
                    IsRepeatable = false,
                    ItemName = itemName,
                    ItemValue = itemValue,
                    ListName = ListName,
                    Nr = row.Nr
                })));
            }
            return items;
        }

        public Row AddRow(Dictionary<string, string> initialUniqueItems = null, Dictionary<string, List<string>> initialRepatedItems = null) =>
            AddOrReplaceRow(initialUniqueItems, initialRepatedItems, null);

        public Row SetRow(int nr, Dictionary<string, string> initialUniqueItems = null, Dictionary<string, List<string>> initialRepatedItems = null) =>
            AddOrReplaceRow(initialUniqueItems, initialRepatedItems, nr);

        private Row AddOrReplaceRow(Dictionary<string, string> initialUniqueItems, Dictionary<string, List<string>> initialRepatedItems, int? replaceNr)
        {
            var nr = replaceNr ?? (rows.Count == 0 ? 1 : (rows.Keys.Max() + 1));
            var items = new List<ComplexApplicationListItemBase>();
            if (initialUniqueItems != null)
                items.AddRange(initialUniqueItems.Where(x => x.Value != null).Select(x => new ComplexApplicationListItemBase { ItemName = x.Key, ItemValue = x.Value, ListName = ListName, Nr = nr, IsRepeatable = false }));
            if (initialRepatedItems != null)
                items.AddRange(initialRepatedItems.SelectMany(x => x.Value.Where(y => y != null).Select(y => new ComplexApplicationListItemBase { ItemName = x.Key, ItemValue = y, ListName = ListName, Nr = nr, IsRepeatable = true })));
            var row = new Row(this.ListName, nr, items); ;
            rows[nr] = row;
            return row;
        }

        public class Row
        {
            public Row(string listName, int nr, List<ComplexApplicationListItemBase> flattendItems)
            {
                ListName = listName;
                Nr = nr;
                uniqueItems = new Dictionary<string, string>();
                repeatedItems = new Dictionary<string, List<string>>();
                foreach (var item in flattendItems)
                {
                    if (item.IsRepeatable)
                    {
                        if (!repeatedItems.ContainsKey(item.ItemName))
                            repeatedItems[item.ItemName] = new List<string>();
                        repeatedItems[item.ItemName].Add(item.ItemValue);
                    }
                    else
                        uniqueItems[item.ItemName] = item.ItemValue;
                }
            }

            public string ListName { get; }
            public int Nr { get; }

            private Dictionary<string, string> uniqueItems;
            private Dictionary<string, List<string>> repeatedItems;

            public string GetUniqueItem(string itemName, bool require = false)
            {
                var value = Opt(uniqueItems, itemName);
                if (value == null && require)
                {
                    throw new NTechCoreWebserviceException($"Missing required complex list item {ListName}#{Nr}#{itemName}") { ErrorCode = "missingRequiredItem", IsUserFacing = true, ErrorHttpStatusCode = 400 };
                }
                return value;
            }

            public bool? GetUniqueItemBoolean(string itemName, bool require = false)
            {
                var value = GetUniqueItem(itemName, require: require);
                if (value == "true")
                    return true;
                else if (value == "false")
                    return false;
                else
                    return null;
            }

            public int? GetUniqueItemInteger(string itemName, bool require = false)
            {
                var rawValue = GetUniqueItem(itemName, require: require);
                if (rawValue == null)
                {
                    return null;
                }

                return int.Parse(rawValue);
            }

            /// <summary>
            /// Like 10d = 10 days or 10m = 10 months
            /// </summary>
            public (int Count, bool IsDays)? GetUniqueItemTimeCountWithPeriodMarker(string itemName, bool require = false)
            {
                var rawValue = GetUniqueItem(itemName, require: require);
                if (rawValue == null)
                {
                    return null;
                }
                var isDays = rawValue.EndsWith("d");
                return (Count: int.Parse(rawValue.Substring(0, rawValue.Length - 1)), IsDays: isDays);
            }

            public decimal? GetUniqueItemDecimal(string itemName, bool require = false)
            {
                var rawValue = GetUniqueItem(itemName, require: require);
                if (rawValue == null)
                {
                    return null;
                }

                return decimal.Parse(rawValue, System.Globalization.CultureInfo.InvariantCulture);
            }

            public List<string> GetRepeatedItems(string itemName) => Opt(repeatedItems, itemName) ?? new List<string>();

            public ICollection<string> GetUniqueItemNames() => uniqueItems.Keys;
            public ICollection<string> GetRepeatedItemNames() => repeatedItems.Keys;
        }
    }
}