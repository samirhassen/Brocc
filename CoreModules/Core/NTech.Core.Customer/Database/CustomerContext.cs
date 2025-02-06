using nCustomer;
using nCustomer.DbModel;
using NTech.Core.Customer.Shared.Database;

namespace NTech.Core.Customer.Database
{
    public class CustomerContext : CustomerContextBase, ICustomerContext
    {
        public IQueryable<KeyValueItem> KeyValueItemsQueryable => KeyValueItems;
        public IQueryable<CustomerCheckpoint> CustomerCheckpointsQueryable => CustomerCheckpoints;
        public IQueryable<TrapetsQueryResult> TrapetsQueryResultsQueryable => TrapetsQueryResults;
        public IQueryable<StoredCustomerQuestionSet> StoredCustomerQuestionSetsQueryable => StoredCustomerQuestionSets;
        public IQueryable<CustomerProperty> CustomerPropertiesQueryable => CustomerProperties;
        public IQueryable<CustomerRelation> CustomerRelationsQueryable => CustomerRelations;
        public IQueryable<CustomerSearchTerm> CustomerSearchTermsQueryable => CustomerSearchTerms;
        public IQueryable<CustomerMessage> CustomerMessagesQueryable => CustomerMessages;
        public IQueryable<KycQuestionTemplate> KycQuestionTemplatesQueryable => KycQuestionTemplates;
        public IQueryable<TrapetsQueryResultItem> TrapetsQueryResultItemsQueryable => TrapetsQueryResultItems;

        public void RemoveKeyValueItem(KeyValueItem item) => KeyValueItems.Remove(item);
        public void AddKeyValueItem(KeyValueItem item) => KeyValueItems.Add(item);
        public void AddCustomerCheckpoints(params CustomerCheckpoint[] checkpoints) => CustomerCheckpoints.AddRange(checkpoints);
        public void AddStoredCustomerQuestionSets(params StoredCustomerQuestionSet[] sets) => StoredCustomerQuestionSets.AddRange(sets);
        public void AddCustomerProperties(params CustomerProperty[] customerProperties) => CustomerProperties.AddRange(customerProperties);
        public void AddBusinessEvents(params BusinessEvent[] events) => BusinessEvents.AddRange(events);
        public void AddCustomerMessages(params CustomerMessage[] messages) => CustomerMessages.AddRange(messages);
        public void AddCustomerMessageAttachedDocuments(params CustomerMessageAttachedDocument[] documents) => CustomerMessageAttachedDocuments.AddRange(documents);
        public void AddKycQuestionTemplates(params KycQuestionTemplate[] templates) => KycQuestionTemplates.AddRange(templates);
        public void AddCustomerSearchTerms(params CustomerSearchTerm[] terms) => CustomerSearchTerms.AddRange(terms);
        public void AddTrapetsQueryResults(params TrapetsQueryResult[] results) => TrapetsQueryResults.AddRange(results);
        public void AddTrapetsQueryResultItems(params TrapetsQueryResultItem[] items) => TrapetsQueryResultItems.AddRange(items);
    }
}
