using Dapper;
using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlxMigrator
{
    /// <summary>
    /// This database lives in Current and in not preserved between migrations
    /// </summary>
    internal class MigrationDb : IDisposable
    {
        private SQLiteConnection connection;

        public MigrationDb(string dir)
        {
            Directory.CreateDirectory(dir);
            connection = new SQLiteConnection($"Data Source={Path.Combine(dir, "migrationdata.db")};Version=3");
            connection.Open();
            
            var doesTableExist = connection.ExecuteScalar<int>("SELECT count(*) FROM sqlite_master WHERE type='table' AND name='KeyValueItem'") > 0;
            if(!doesTableExist)
            {
                ReCreate();
                Set("StartDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                Set("Status", "PendingSetup");
                return;
            }
        }

        public void Set(string key, string value)
        {
            connection.Execute(@"insert or replace into KeyValueItem (ItemKey, ItemValue) VALUES (@key, @value)", new { key, value });
        }

        public string Get(string key)
        {
            return connection.Query<string>("select ItemValue from KeyValueItem where ItemKey = @key", new { key }).FirstOrDefault();
        }

        public void SetTyped<T>(string key, T value) where T : class
        {
            Set(key, JsonConvert.SerializeObject(new { typeName = typeof(T).FullName, value = value }));
        }

        public T GetTyped<T>(string key) where T : class
        {
            var stored = Get(key);
            if (stored == null)
                return null;
            var result = JsonConvert.DeserializeAnonymousType(stored, new { typeName = "", value = (T)null });
            if(result?.typeName != typeof(T).FullName)
                throw new Exception($"Stored value is not of type {typeof(T).FullName}");
            return result?.value;
        }

        public void AddCustomerIdsToMigrate(HashSet<int> customerIds, bool isLoan)
        {
            var tableName = isLoan ? "LoanMigrationCustomer" : "SavingsMigrationCustomer";
            using (var tr = connection.BeginTransaction())
            {
                foreach (var customerId in customerIds)
                    connection.Execute($"insert or ignore into {tableName}(CustomerId) values (@customerId)", param: new { customerId }, transaction: tr);

                tr.Commit();
            }
        }

        public bool WithCustomersIdsToMigrateBatch(int batchSize, bool isLoan, Action<(int TotalCount, int CountAfter, HashSet<int> CustomerIds, string FileName)> migrate)
        {
            var filePrefix = isLoan ? "ConsumerCreditCustomer" : "ConsumerSavingsCustomer";
            var tableName = isLoan ? "LoanMigrationCustomer" : "SavingsMigrationCustomer";
            var totalCount = connection.ExecuteScalar<int>($"select count(*) from {tableName}");
            var countMigrated = connection.ExecuteScalar<int>($"select count(*) from {tableName} where MigrationFileName is not null");            
            var customerIds = connection.Query<int>($"select CustomerId from {tableName} where MigrationFileName is null order by CustomerId asc limit {batchSize} ").ToHashSet();
            if (customerIds.Count == 0)
                return false;
            var minCustomerId = customerIds.Min();
            var maxCustomerId = customerIds.Max();
            var fileName = $"{filePrefix}_{minCustomerId}_{maxCustomerId}.json";
            var countAfter = totalCount - countMigrated - customerIds.Count;
            migrate((TotalCount: totalCount, CountAfter: countAfter, CustomerIds: customerIds, FileName: fileName));
            if(customerIds.Count > 0)
                connection.Execute($"update {tableName} set MigrationFileName = @fileName  where CustomerId in @customerIds", param: new { customerIds, fileName });
            return countAfter > 0;
        }

        public void ReCreate()
        {
            connection.Execute("create table if not exists KeyValueItem (ItemKey TEXT NOT NULL PRIMARY KEY, ItemValue TEXT)");            
            
            connection.Execute("create table if not exists LoanMigrationCustomer (CustomerId INT NOT NULL PRIMARY KEY, MigrationFileName TEXT)");
            connection.Execute("create index if not exists LoanMigrationCustomerIdx on LoanMigrationCustomer(CustomerId asc, MigrationFileName)");
            
            connection.Execute("create table if not exists SavingsMigrationCustomer (CustomerId INT NOT NULL PRIMARY KEY, MigrationFileName TEXT)");
            connection.Execute("create index if not exists SavingsMigrationCustomerIdx on SavingsMigrationCustomer(CustomerId asc, MigrationFileName)");
            
            Set("CacheFromDate", DateTime.Today.ToString("yyyy-MM-dd"));
        }

        public void Dispose()
        {
            connection.Close();
            connection.Dispose();
        }
    }
}
