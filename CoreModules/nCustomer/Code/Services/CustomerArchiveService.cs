using Dapper;
using nCustomer.DbModel;
using NTech;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Configuration;

namespace nCustomer.Code.Services
{
    public class CustomerArchiveService
    {
        public CustomerArchiveService(IClock clock, NtechCurrentUserMetadata currentUserMetadata)
        {
            this.clock = clock;
            this.currentUserMetadata = currentUserMetadata;
        }

        public int ArchiveCustomersBasedOnModuleCandidates(ISet<int> moduleCandidateCustomerIds, string moduleName)
        {
            if (!moduleName.IsOneOfIgnoreCase("nPreCredit"))
                throw new NotImplementedException();

            ISet<int> customerIdsToArchive;
            using (var context = new CustomersContext())
            {
                var archiveContext = new ArchiveContext(context,
                    new Lazy<CreditClient>(() => new CreditClient()),
                    new Lazy<SavingsClient>(() => new SavingsClient()));
                customerIdsToArchive = GetCustomerIdsBatchToArchive(moduleCandidateCustomerIds, archiveContext, moduleName);
            }

            RemoveCustomers(customerIdsToArchive);

            return customerIdsToArchive.Count;
        }

        public static ISet<int> GetCustomerIdsBatchToArchive(ISet<int> candidateCustomerIds, IArchiveContext archiveContext, string candidatesSourceModuleName)
        {
            var localCandidateCustomerIds = new HashSet<int>(candidateCustomerIds);

            //Customer that have or have ever had relations are not replicated
            //We dont fully trust this so the modules are allowed to a veto also so this
            //acts more like a speedup so its not super important that this is up to date exactly.
            var customerIdsWithRelations = archiveContext
                .CustomerRelations
                .Where(x => candidateCustomerIds.Contains(x.CustomerId))
                .Select(x => x.CustomerId)
                .ToHashSet();
            localCandidateCustomerIds.ExceptWith(customerIdsWithRelations);

            /*
            Let nCredit have a chance to veto archiving these (for instance if there are loans)
            */
            if (!candidatesSourceModuleName.IsOneOfIgnoreCase("nCredit"))
            {
                var creditCustomerIdsToArchive = archiveContext.GetCustomerIdsThatNCreditThinksCanBeArchived(localCandidateCustomerIds);
                localCandidateCustomerIds.IntersectWith(creditCustomerIdsToArchive);
            }

            /*
            Let nSavings have a chance to veto archiving these (for instance if there are savings accounts)
            */
            if (!candidatesSourceModuleName.IsOneOfIgnoreCase("nSavings"))
            {
                var savingsCustomerIdsToArchive = archiveContext.GetCustomerIdsThatNSavingsThinksCanBeArchived(localCandidateCustomerIds);
                localCandidateCustomerIds.IntersectWith(savingsCustomerIdsToArchive);
            }

            return localCandidateCustomerIds;
        }

        private void RemoveCustomersBatch(ISet<int> customerIds, SqlConnection connection, SqlTransaction tr)
        {
            if (customerIds.Count > 200)
                throw new ArgumentException("Max 200 customers at a time allowed", "customerIds");

            var timeout = (int)TimeSpan.FromMinutes(5).TotalMilliseconds;


            Func<DynamicParameters> getStandardParameters = () =>
            {
                var p = new DynamicParameters();
                p.AddDynamicParams(new
                {
                    customerIds
                });
                return p;
            };

            void Execute(string query, object extraParameters = null)
            {
                var parameters = getStandardParameters();
                if (extraParameters != null)
                    parameters.AddDynamicParams(extraParameters);
                try
                {
                    connection.Execute(query, param: parameters, transaction: tr, commandTimeout: timeout);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Execute failed. Example customerIds: [{string.Join(", ", customerIds.Take(5))}]. Statement:{Environment.NewLine}{query}", ex);
                }
            }

            Execute("select cast(p.Value as bigint) as EncryptedValueId into ##tempEncryptionIds from CustomerProperty p where p.CustomerId in @customerIds and p.IsEncrypted = @isEncrypted", new { isEncrypted = true });
            try
            {
                Execute("delete from EncryptedValue where Id in (select EncryptedValueId from ##tempEncryptionIds)");
            }
            finally
            {
                Execute("drop table ##tempEncryptionIds");
            }

            Execute("delete from CustomerProperty where CustomerId in @customerIds");
            //Leave archive comments so we can reason about if the archive process is working or not
            Execute("delete from CustomerComment where CustomerId in @customerIds and EventType <> @eventType", new { eventType = "ArchiveCustomer" });
            foreach (var customerId in customerIds)
            {
                Execute(@"INSERT INTO [CustomerComment] 
                    (CustomerId, CommentDate, CommentById, EventType, CommentText, ChangedById, ChangedDate, InformationMetaData)
                    values
                    (@CustomerId, @CommentDate, @CommentById, @EventType, @CommentText, @ChangedById, @ChangedDate, @InformationMetaData)",
                    new
                    {
                        CustomerId = customerId,
                        CommentText = "Customer archived",
                        InformationMetaData = currentUserMetadata.InformationMetadata,
                        ChangedById = currentUserMetadata.UserId,
                        ChangedDate = clock.Now,
                        CommentById = currentUserMetadata.UserId,
                        CommentDate = clock.Now,
                        EventType = "ArchiveCustomer"
                    });
            }

            //CustomerCardConflict has been outphased as a concept, but removing existing conflicts remains supported 
            //At least for a while, after which this should be removed
            Execute("delete from CustomerCardConflict where CustomerId in @customerIds");

            Execute("delete from TrapetsQueryResult where CustomerId in @customerIds");
            //TrapetsQueryResultItems: Will be removed by cascading from TrapetsQueryResults
            Execute("delete from CustomerMessage where CustomerId in @customerIds");
            //CustomerMessageAttachedDocument: Will be removed by cascading from CustomerMessages

            /*
                Intentionally not removed:
                CustomerIdSequence: To preserve customerId being a pseudonym for civicRegNr/orgnr
                CustomerRelation: Needed for logical cohesion of things like the archivejob             
                */
        }

        private void RemoveCustomers(ISet<int> customerIds)
        {
            using (var connection = new SqlConnection(WebConfigurationManager.ConnectionStrings["CustomersContext"].ConnectionString))
            {
                connection.Open();
                var tr = connection.BeginTransaction();
                try
                {
                    foreach (var customerIdsGroup in customerIds.ToArray().SplitIntoGroupsOfN(200))
                    {
                        RemoveCustomersBatch(customerIdsGroup.ToHashSet(), connection, tr);
                    }
                    tr.Commit();
                }
                catch
                {
                    tr.Rollback();
                    throw;
                }
            }
        }

        private readonly IClock clock;
        private readonly NtechCurrentUserMetadata currentUserMetadata;

        public interface IArchiveContext //This abstraction only exists to allow testing.
        {
            IQueryable<CustomerRelation> CustomerRelations { get; }
            ISet<int> GetCustomerIdsThatNCreditThinksCanBeArchived(ISet<int> customerIds);
            ISet<int> GetCustomerIdsThatNSavingsThinksCanBeArchived(ISet<int> customerIds);
        }

        public class ArchiveContext : IArchiveContext
        {
            private readonly CustomersContext context;
            private readonly Lazy<CreditClient> creditClient;
            private readonly Lazy<SavingsClient> savingsClient;

            public ArchiveContext(CustomersContext context, Lazy<CreditClient> creditClient, Lazy<SavingsClient> savingsClient)
            {
                this.context = context;
                this.creditClient = creditClient;
                this.savingsClient = savingsClient;
            }

            public IQueryable<CustomerRelation> CustomerRelations => context.CustomerRelations;

            public ISet<int> GetCustomerIdsThatNCreditThinksCanBeArchived(ISet<int> customerIds)
            {
                if (NEnv.ServiceRegistry.ContainsService("nCredit"))
                {
                    return creditClient.Value.FetchCustomerIdsThatCanBeArchived(customerIds);
                }
                else
                    return customerIds; //Filter does not apply in this case
            }

            public ISet<int> GetCustomerIdsThatNSavingsThinksCanBeArchived(ISet<int> customerIds)
            {
                if (NEnv.ServiceRegistry.ContainsService("nSavings"))
                {
                    return savingsClient.Value.FetchCustomerIdsThatCanBeArchived(customerIds);
                }
                else
                    return customerIds; //Filter does not apply in this case
            }
        }
    }
}