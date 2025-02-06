using Dapper;
using NTech.Core.Customer.Shared.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.Code.Services.Aml.Cm1
{
    public class Cm1AmlDataRepository
    {
        private readonly CustomerContextFactory contextFactory;

        public Cm1AmlDataRepository(CustomerContextFactory contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public void UpdateSentToCm1(List<CompleteCmlExportFileRequest.CustomerModel> customers,
            Func<ICustomerContextExtended, CustomerWriteRepository> createRepo)
        {
            using (var context = contextFactory.CreateContext())
            {
                context.BeginTransaction();
                try
                {
                    var customerRepo = createRepo(context);
                    var updatedProperties = new List<CustomerPropertyModel>();
                    foreach (var item in customers)
                    {
                        updatedProperties.Add(CreateSentToCm1CustomerUpdateItem(item.CustomerId, item.TransferedStatus == "Inserts" || item.TransferedStatus == "Updates", true));
                    }
                    customerRepo.UpdateProperties(updatedProperties, force: true, businessEventCode: null, updateEvenIfNotChanged: true);
                    context.SaveChanges();
                    context.CommitTransaction();
                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }
            }
        }

        public static CustomerPropertyModel CreateSentToCm1CustomerUpdateItem(int customerId, bool isSent, bool forceUpdate)
        {
            return new CustomerPropertyModel
            {
                CustomerId = customerId,
                Value = isSent ? "true" : "false",
                Name = "sentToCM1",
                Group = "cm1",
                IsSensitive = false,
                ForceUpdate = forceUpdate
            };
        }

        public List<CompleteCmlExportFileRequest.CustomerModel> FetchCustomersToExport(string relationTypes)
        {
            /*
             - If a customer has been sent or not is tracked by the CustomerProperty sentToCM1 (true|false)
             - CM1 should have all customers where there is at least one CustomerRelation with EndDate = null of the correct RelationType (Loan or SavingsAccount but not Application)
             */

            const string CustomerNeverSentToCm1 =
@"select	distinct 
		cp.CustomerId, 
		'Inserts' as TransferedStatus 
from	CustomerProperty cp
join	CustomerRelation cr on cr.CustomerId = cp.CustomerId and cr.EndDate is null and RelationType in(@RelationTypes)
where	not exists(select * from CustomerProperty cpy where cpy.CustomerId = cp.CustomerId  and cpy.IsCurrentData = 1 and cpy.Name = 'sentToCM1' and cpy.Value = 'true')";

            //Update either when a customer property has changed since last export or new kyc answers have been added for the product
            const string CustomersSentBeforeThatHaveChanged =
@"select	distinct 
		cp.CustomerId, 
		'Updates' as TransferedStatus 
from	CustomerProperty cp
join	CustomerRelation cr on cr.CustomerId = cp.CustomerId and cr.EndDate is null and RelationType in(@RelationTypes)
join	CustomerProperty cpnew on cpnew.CustomerId = cp.CustomerId and cpnew.IsCurrentData = 1 and cpnew.Name = 'sentToCM1' and cpnew.Value = 'true'
where 
(
	exists(select * from CustomerProperty cpy where (cpy.CustomerId = cpnew.CustomerId) and cpy.Id > cpnew.Id)
	or	
	exists(select 1 from StoredCustomerQuestionSet q where q.CustomerId = cpnew.CustomerId and q.SourceType in(@RelationTypes) and q.[Timestamp] > cpnew.[Timestamp]) 
)";

            const string CustomersSentBeforeThatShouldBeRemove =
@"select	distinct 
		cr.CustomerId, 
		'Deletes'as TransferedStatus
from	CustomerRelation cr
join	CustomerProperty cp on cp.CustomerId = cr.CustomerId and cp.Name = 'sentToCM1' and cp.Value ='true' and RelationType in(@RelationTypes)
where	not exists(select distinct cr1.CustomerId from CustomerRelation cr1 where cr1.CustomerId = cr.CustomerId and cr1.EndDate is null)
and		not exists(select distinct CustomerId from CustomerProperty cr1 where CustomerId = cr.CustomerId and IsCurrentData =1 and Name = 'sentToCM1' and Value ='false')";

            var query = $"{CustomerNeverSentToCm1} union {CustomersSentBeforeThatHaveChanged} union {CustomersSentBeforeThatShouldBeRemove}";

            using (var context = contextFactory.CreateContext())
            {
                return context.GetConnection().Query<CompleteCmlExportFileRequest.CustomerModel>(query, param: new
                {
                    RelationTypes = relationTypes
                }, commandTimeout: 60).ToList();       
            }
        }

        public List<CompleteCmlExportFileRequest.CustomerModel> FetchAllCustomersCurrentlySentToCm1(string relationTypes, bool sendAsUpdate)
        {
            /*
             * NOTE: We dont filter on cr.EndDate = null since the way that propagates to cm1 is by the regular job picking up
             * that sentToCM1 = true but everything has an EndDate and then changing sentToCM1 to false and sending a Delete.
             */
            const string Query =
@"select	distinct 
		c.CustomerId
from	CustomerProperty c
join	CustomerRelation cr on cr.CustomerId = c.CustomerId and cr.RelationType in(@RelationTypes)
where   c.IsCurrentData = 1 and c.Name = 'sentToCM1' and c.Value = 'true'";

            using (var context = contextFactory.CreateContext())
            {
                var customers = context.GetConnection().Query<CompleteCmlExportFileRequest.CustomerModel>(Query, param: new
                {
                    RelationTypes = relationTypes
                }, commandTimeout: 60).ToList();

                customers.ForEach(x => x.TransferedStatus = sendAsUpdate ? "Updates" : "Inserts");

                return customers;
            }
        }
    }
}