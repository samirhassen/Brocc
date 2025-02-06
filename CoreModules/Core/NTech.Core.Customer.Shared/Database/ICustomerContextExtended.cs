using nCustomer;
using nCustomer.DbModel;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System.Linq;

namespace NTech.Core.Customer.Shared.Database
{
    public interface ICustomerContext : INTechDbContext
    {
        IQueryable<KeyValueItem> KeyValueItemsQueryable { get; }
        IQueryable<CustomerCheckpoint> CustomerCheckpointsQueryable { get; }
        IQueryable<TrapetsQueryResult> TrapetsQueryResultsQueryable { get; }
        IQueryable<StoredCustomerQuestionSet> StoredCustomerQuestionSetsQueryable { get; }
        IQueryable<CustomerProperty> CustomerPropertiesQueryable { get; }
        IQueryable<CustomerRelation> CustomerRelationsQueryable { get; }
        IQueryable<CustomerSearchTerm> CustomerSearchTermsQueryable { get; }
        IQueryable<CustomerMessage> CustomerMessagesQueryable { get; }
        IQueryable<KycQuestionTemplate> KycQuestionTemplatesQueryable { get; }
        IQueryable<TrapetsQueryResultItem> TrapetsQueryResultItemsQueryable { get; }

        void RemoveKeyValueItem(KeyValueItem item);
        void AddKeyValueItem(KeyValueItem item);
        void AddCustomerCheckpoints(params CustomerCheckpoint[] checkpoints);
        void AddStoredCustomerQuestionSets(params StoredCustomerQuestionSet[] sets);
        void AddCustomerProperties(params CustomerProperty[] customerProperties);
        void AddBusinessEvents(params BusinessEvent[] events);
        void AddCustomerMessages(params CustomerMessage[] messages);
        void AddCustomerMessageAttachedDocuments(params CustomerMessageAttachedDocument[] documents);
        void AddKycQuestionTemplates(params KycQuestionTemplate[] templates);
        void AddCustomerSearchTerms(params CustomerSearchTerm[] terms);
        void AddTrapetsQueryResults(params TrapetsQueryResult[] results);
        void AddTrapetsQueryResultItems(params TrapetsQueryResultItem[] items);

        int SaveChanges();
    }

    public interface ICustomerContextExtended : ICustomerContext, INTechDbContext
    {
        T FillInfrastructureFields<T>(T b) where T : InfrastructureBaseItem;
        ICoreClock CoreClock { get; }
        INTechCurrentUserMetadata CurrentUser { get; }
    }
}
