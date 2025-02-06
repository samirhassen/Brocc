using Dapper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SlxMigrator
{
    internal class SavingsMigrator : IMigrator
    {
        private readonly ConnectionFactory connectionFactory;
        private readonly string encryptionKeyName;
        private readonly string encryptionKeyValue;
        private readonly MigrationDb migrationDb;
        private readonly CrossRunCacheDb crossRunCacheDb;

        public SavingsMigrator(ConnectionFactory connectionFactory, string encryptionKeyName, string encryptionKeyValue, MigrationDb migrationDb, CrossRunCacheDb crossRunCacheDb)
        {
            this.connectionFactory = connectionFactory;
            this.encryptionKeyName = encryptionKeyName;
            this.encryptionKeyValue = encryptionKeyValue;
            this.migrationDb = migrationDb;
            this.crossRunCacheDb = crossRunCacheDb;
        }

		public JObject CreateLoansFileCustomers(HashSet<int> customerIds)
		{
            var customers = slx_customer.CreateForCustomers(customerIds, connectionFactory, encryptionKeyName, encryptionKeyValue);
            var savings = slx_savings.CreateForCustomers(customerIds, connectionFactory);
            var bank_accounts = slx_bank_accounts.CreateForCustomers(customerIds, connectionFactory, false);
            var profiles = slx_profiles.CreateForCustomers(customerIds, connectionFactory, false);
            var transactions = slx_transactions.CreateForCustomers(customerIds, connectionFactory, false);

            var customerNodes = new List<JObject>();
            foreach(var customerId in customerIds)
            {
                try
                {
                    var customer = customers[customerId];

                    var customerSavings = savings.GetWithDefault(customerId);

                    foreach(var customerSavingsAccount in customerSavings)
                    {
                        var savingsAccountNr = customerSavingsAccount.GetStringPropertyValue("savings_id", false);
                        
                        var bankAccounts = bank_accounts.GetWithDefault(slx_bank_accounts.GetKey(savingsAccountNr, customerId));
                        customerSavingsAccount.Add("bank_accounts", new JArray(bankAccounts));

                        var accountProfiles = profiles.GetWithDefault(slx_profiles.GetSavingsKey(savingsAccountNr, customerId));
                        customerSavingsAccount.Add("profiles", new JArray(accountProfiles));
                    }

                    customer.Add("savings", new JArray(customerSavings));
                    if (customerSavings.Count > 0)
                    {
                        //NOTE: And customer that has or has had a savings account is considered active in this model so it's intentional that accounts can be closed
                        customer.AddOrReplaceJsonProperty("active", new JValue(1), true);
                    }

                    var customerTransactions = transactions.GetWithDefault(customerId);
                    customer.Add("transactions", new JArray(customerTransactions));

                    customerNodes.Add(customer);
                }
                catch(Exception ex)
                {
                    throw new Exception($"Error processing customer: {customerId}", ex);
                }
            }

            var file = new JObject();
            file.Add("customers", new JArray(customerNodes));

            return file;
		}

        public void AddCustomersToMigration(int? startAtCustomerId)
        {
            using (var connection = connectionFactory.CreateOpenConnection(DatabaseCode.Savings))
            {
                var customerIds = connection
                    .Query<int>("select distinct MainCustomerId from SavingsAccountHeader")
                    .ToHashSet();
                migrationDb.AddCustomerIdsToMigrate(customerIds, false);
            }
        }
	}
}
