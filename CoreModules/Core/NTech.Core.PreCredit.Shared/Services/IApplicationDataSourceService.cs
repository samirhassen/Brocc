using nPreCredit.Code.Datasources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class ApplicationDataSourceEditModel
    {
        public string DataSourceName { get; set; }
        public string CompoundItemName { get; set; }
        public bool IsDelete { get; set; }
        public string NewValue { get; set; }
    }

    public class ApplicationDataSourceServiceRequest
    {
        public string DataSourceName { get; set; }
        public ISet<string> Names { get; set; }
        public ApplicationDataSourceMissingItemStrategy MissingItemStrategy { get; set; }
        public Action<string> ObserveMissingItems { get; set; }
        public Action<string> ObserveChangedItems { get; set; }

        /// <summary>
        /// If not present when using the missing strategy all missing items will have the string 'null' as value.
        /// </summary>
        public Func<string, string> GetDefaultValue { get; set; }
    }

    public class ApplicationDataSourceResult
    {
        private readonly string applicationNr;
        private readonly Dictionary<string, Dictionary<string, string>> r;

        public const string MissingItemValue = "ce92568b-db5a-4824-ae7d-f1491cff7cc4";

        public ApplicationDataSourceResult(string applicationNr, Dictionary<string, Dictionary<string, string>> r)
        {
            this.applicationNr = applicationNr;
            this.r = r;
        }

        public List<string> ItemNames(string dataSourceName)
        {
            return r.ContainsKey(dataSourceName) ? r[dataSourceName].Keys.ToList() : new List<string>();
        }

        public StringItem Item(string dataSourceName, string itemName)
        {
            return new StringItem(Raw(dataSourceName, itemName), $"{applicationNr}.{dataSourceName}.{itemName}", s => { throw new NotImplementedException(); }, null);
        }

        public DataSourceResult DataSource(string dataSourceName)
        {
            return new DataSourceResult(dataSourceName, this);
        }

        private string Raw(string datasourceName, string itemName)
        {
            if (!r.ContainsKey(datasourceName))
                return null;

            var v = r[datasourceName].Opt(itemName);
            if (v == MissingItemValue || v == null)
                return null;
            else
                return v;
        }

        public class DataSourceResult
        {
            private readonly string dataSourceName;
            private readonly ApplicationDataSourceResult result;

            public DataSourceResult(string dataSourceName, ApplicationDataSourceResult result)
            {
                this.dataSourceName = dataSourceName;
                this.result = result;
            }

            public List<string> ItemNames()
            {
                return result.ItemNames(dataSourceName);
            }

            public StringItem Item(string itemName)
            {
                return result.Item(dataSourceName, itemName);
            }

            [System.Runtime.CompilerServices.IndexerName("IndexItem")]
            public StringItem this[string itemName]
            {
                get { return result.Item(dataSourceName, itemName); }
            }
        }
    }
}