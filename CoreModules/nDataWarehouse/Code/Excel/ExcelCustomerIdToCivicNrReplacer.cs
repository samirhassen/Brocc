using Dapper;
using nDataWarehouse.Code.Clients;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;

namespace nDataWarehouse.Code.Excel
{
    public class ExcelCustomerIdToCivicNrReplacer
    {
        public ISet<int> GetAllCustomerIds(string customerIdQuery)
        {
            var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DataWarehouse"].ConnectionString;
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                return new HashSet<int>(conn.Query<int>(customerIdQuery, commandTimeout: 600));
            }
        }

        private static IEnumerable<IEnumerable<T>> SplitIntoGroupsOfN<T>(IList<T> items, int n)
        {
            for (var i = 0; i < (float)items.Count / n; i++)
            {
                yield return items.Skip(i * n).Take(n);
            }
        }

        private static void WithConnection(Action<SqlConnection> a)
        {
            var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DataWarehouse"].ConnectionString;
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                a(conn);
            }
        }

        const string TempTableName = "TempCustomerIdToCivicRegNrMapping";

        private static Dictionary<int, string> SetupCache()
        {
            Dictionary<int, string> d = new Dictionary<int, string>();
            WithConnection(c =>
            {
                if (!c.QueryFirstOrDefault<int?>($"select 1 from INFORMATION_SCHEMA.TABLES t where t.TABLE_NAME = '{TempTableName}'").HasValue)
                {
                    c.Execute($"create table [{TempTableName}] (CustomerId int not null primary key, CivicRegNr nvarchar(20) not null)");
                }
                foreach (var i in c.Query<TempTableNameItem>($"select CustomerId, CivicRegNr from  [{TempTableName}]", commandTimeout: 500))
                {
                    d[i.CustomerId] = i.CivicRegNr;
                }
            });
            return d;
        }

        private class TempTableNameItem
        {
            public int CustomerId { get; set; }
            public string CivicRegNr { get; set; }
        }

        public static Func<ISet<int>, IDictionary<int, string>> CreateCivicRegnrCustomerClientSource(Func<ICustomerClient> createCustomerClient)
        {
            var client = createCustomerClient();
            var cache = SetupCache();
            var dws = new DwSupport();
            return customerIds =>
            {
                var nonCachedcustomerIds = customerIds.Where(x => !cache.ContainsKey(x)).ToList();
                if (nonCachedcustomerIds.Any())
                {
                    var newItems = new List<ExpandoObject>(nonCachedcustomerIds.Count);
                    foreach (var customerIdGroup in SplitIntoGroupsOfN(nonCachedcustomerIds, 1500))
                    {
                        var r = client.BulkFetchPropertiesByCustomerIds(new HashSet<int>(customerIdGroup), "civicRegNr");
                        foreach (var i in r)
                        {
                            var customerId = i.Key;
                            var civicRegNr = i.Value.Properties[0].Value;
                            cache[customerId] = civicRegNr;
                            var ni = new ExpandoObject();
                            (ni as IDictionary<string, object>)["CustomerId"] = customerId;
                            (ni as IDictionary<string, object>)["CivicRegNr"] = civicRegNr;
                            newItems.Add(ni);
                        }
                        string errMsg;
                        if (!dws.TryMergeTable(TempTableName, newItems, out errMsg))
                            throw new Exception(errMsg);
                    }
                }

                return cache;
            };
        }
    }
}