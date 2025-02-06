using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace nPreCredit
{
    public class PartialCreditReportModel
    {
        public class Item
        {
            public string Name { get; set; }

            public string Value { get; set; }
        }

        private List<Item> items;

        public PartialCreditReportModel(List<Item> items = null)
        {
            this.items = items ?? new List<Item>();
        }

        public List<string> GetAvailableNames()
        {
            return items.Select(x => x.Name).ToList();
        }

        public List<Item> GetItems()
        {
            return items;
        }

        public void FilterInPlace(Func<string, string, bool> shouldKeepItem)
        {
            if (this.items != null)
            {
                this.items = this.items.Where(x => shouldKeepItem(x.Name, x.Value)).ToList();
            }
        }

        public StringItem Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", "name");
            var v = items.SingleOrDefault(x =>
                x?.Name?.ToLowerInvariant() == name?.ToLowerInvariant());

            Action<string> set = newValue =>
            {
                var item = items.SingleOrDefault(x =>
                    x?.Name?.ToLowerInvariant() == name?.ToLowerInvariant());
                if (item != null)
                    item.Value = newValue;
                else
                    items.Add(new Item { Name = name, Value = newValue });
            };

            return new StringItem(v?.Value, name, set, null);
        }

        public string ToJson()
        {
            var e = new ExpandoObject();
            IDictionary<string, object> ed = e;
            foreach (var item in items)
            {
                ed[item.Name] = item.Value;
            }
            return JsonConvert.SerializeObject(e);
        }

        public static PartialCreditReportModel FromJson(string json)
        {
            var e = JsonConvert.DeserializeObject<ExpandoObject>(json, new Newtonsoft.Json.Converters.ExpandoObjectConverter());
            IDictionary<string, object> ed = e;
            List<Item> items = new List<Item>();
            foreach (var kvp in ed)
            {
                items.Add(new Item { Name = kvp.Key, Value = kvp.Value?.ToString() });
            }
            return new PartialCreditReportModel(items);
        }
    }
}
