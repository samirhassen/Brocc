using Dapper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SlxMigrator
{
    internal class LoansMigrator : IMigrator
    {
        private readonly ConnectionFactory connectionFactory;
        private readonly string encryptionKeyName;
        private readonly string encryptionKeyValue;
        private readonly MigrationDb migrationDb;
        private readonly CrossRunCacheDb crossRunCacheDb;

        public LoansMigrator(ConnectionFactory connectionFactory, string encryptionKeyName, string encryptionKeyValue, MigrationDb migrationDb, CrossRunCacheDb crossRunCacheDb)
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
			var loans = slx_loans.CreateForCustomers(customerIds, connectionFactory);
            var invoices = slx_invoices.CreateForCustomers(customerIds, connectionFactory);
            var invoice_line_items = slx_invoice_line_items.CreateForCustomers(customerIds, connectionFactory);
            var loan_details = slx_loan_details.CreateKeyedByLoanId(customerIds, connectionFactory);
            var transactions = slx_transactions.CreateForCustomers(customerIds, connectionFactory, true);
            var contracts = slx_contracts.CreateForCustomers(customerIds, connectionFactory, crossRunCacheDb);
            var applications = slx_loan_applications.CreateForCustomers(customerIds, connectionFactory, crossRunCacheDb);
            var loan_application_detail = slx_loan_application_detail.CreateForCustomers(customerIds, connectionFactory);
            var loan_app_decisions = slx_loan_app_decisions.CreateForCustomers(customerIds, connectionFactory);

            var customerNodes = new List<JObject>();
            foreach(var customerId in customerIds)
            {
                try
                {
                    var customer = customers[customerId];

                    var customerInvoices = invoices.GetWithDefault(customerId);
                    foreach (var customerInvoice in customerInvoices)
                    {
                        var invoiceId = customerInvoice["invoice_id"].Value<int>();
                        var invoiceLineItems = invoice_line_items.GetWithDefault(customerId).GetWithDefault(invoiceId);
                        customerInvoice.Add("invoicelineitems", new JArray(invoiceLineItems));
                    }
                    customer.Add("invoices", new JArray(customerInvoices));

                    var customerLoans = loans.GetWithDefault(customerId);
                    foreach (var customerLoan in customerLoans)
                    {
                        var creditNr = customerLoan["loan_id"].Value<string>();
                        var details = loan_details[creditNr];
                        customerLoan.Add("details", new JArray(details));

                        var loanContracts = contracts.GetWithDefault(slx_contracts.GetKey(customerId, creditNr));
                        customerLoan.Add("contracts", new JArray(loanContracts));
                    }
                    customer.Add("loans", new JArray(customerLoans));
                    if(customerLoans.Count > 0)
                    {
                        //NOTE: And customer that has or has had a loan is considered active in this model so it's intentional that loans can be closed
                        customer.AddOrReplaceJsonProperty("active", new JValue(1), true);
                    }

                    var customerTransactions = transactions.GetWithDefault(customerId);
                    customer.Add("transactions", new JArray(customerTransactions));

                    var customerApplications = applications.GetWithDefault(customerId);
                    foreach(var customerApplication in customerApplications)
                    {
                        var applicationNr = customerApplication["loan_application_id"].Value<string>();

                        var details = loan_application_detail.GetWithDefault(slx_loan_application_detail.GetKey(applicationNr, customerId));
                        customerApplication.Add("details", new JArray(details));

                        var decisions = loan_app_decisions.GetWithDefault(slx_loan_app_decisions.GetKey(applicationNr, customerId));
                        customerApplication.Add("decisions", new JArray(decisions));
                    }
                    customer.Add("loan_applications", new JArray(customerApplications));

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
            using (var connection = connectionFactory.CreateOpenConnection(DatabaseCode.Credit))
            {
                var query = "select distinct CustomerId from CreditCustomer";
                if (startAtCustomerId.HasValue)
                {
                    //A bit convoluted to make sure we dont set the cutoff where one customer is included and the other is not
                    //Beware that this may cause a bunch of wierdness so dont use for production only for testing.
                    query = $"select distinct c2.CustomerId from CreditCustomer c2 where c2.CreditNr in(select c1.CreditNr from CreditCustomer c1 where CustomerId >= {startAtCustomerId.Value})";
                }

                var loanCustomerIds = connection
                    .Query<int>(query)
                    .ToHashSet();
                migrationDb.AddCustomerIdsToMigrate(loanCustomerIds, true);
            }

            var queryBase = @"with CustomerIdItem
as
(
select	cast(i.[Value] as int) as CustomerId,
		i.Id
from	CreditApplicationItem i
join	CreditApplicationHeader h on h.ApplicationNr = i.ApplicationNr
where	i.GroupName in ('Applicant1', 'Applicant2')    
and		i.[Name] = 'customerId'
and		h.IsFinalDecisionMade = 0
and     h.ArchivedDate is null
) ";
            using (var connection = connectionFactory.CreateOpenConnection(DatabaseCode.PreCredit))
            {
                var maxMigratedItemId = 0;
                var count = 1;
                while(count > 0)
                {
                    var items = connection
                        .Query<CustomerIdItemData>(
                            queryBase + "select top 1000 i.* from CustomerIdItem i where i.Id > @maxMigratedItemId order by i.Id asc",
                            param: new { maxMigratedItemId })
                        .ToList();
                        
                    if(items.Count > 0)
                    {
                        maxMigratedItemId = items[items.Count - 1].Id;
                        migrationDb.AddCustomerIdsToMigrate(items.Select(x => x.CustomerId).ToHashSet(), true);
                    }
                    count = items.Count;
                }
            }
        }

        private class CustomerIdItemData
        {
            public int CustomerId { get; set; }
            public int Id { get; set; }
        }
	}
}
