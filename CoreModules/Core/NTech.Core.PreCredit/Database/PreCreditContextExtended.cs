using Microsoft.EntityFrameworkCore;
using nPreCredit;
using nPreCredit.DbModel;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.PreCredit.Shared;

namespace NTech.Core.PreCredit.Database
{
    public class PreCreditContextExtended : PreCreditContext, IPreCreditContextExtended
    {
        private readonly INTechCurrentUserMetadata ntechCurrentUserMetadata;
        private readonly ICoreClock clock;

        public PreCreditContextExtended(INTechCurrentUserMetadata ntechCurrentUserMetadata, ICoreClock clock)
        {
            this.ntechCurrentUserMetadata = ntechCurrentUserMetadata;
            this.clock = clock;
        }

        public ICoreClock CoreClock => clock;
        public int CurrentUserId => ntechCurrentUserMetadata.UserId;
        public string InformationMetadata => ntechCurrentUserMetadata.InformationMetadata;

        public T FillInfrastructureFields<T>(T b) where T : InfrastructureBaseItem
        {
            b.ChangedById = ntechCurrentUserMetadata.UserId;
            b.ChangedDate = clock.Now;
            b.InformationMetaData = ntechCurrentUserMetadata.InformationMetadata;
            return b;
        }

        public bool IsCollectionLoaded<TEntity>(TEntity entity, string propertyName)
            where TEntity : class => Entry(entity).Collection(propertyName).IsLoaded;

        private CreditApplicationEvent CreateEvent(CreditApplicationEventCode eventCode, string applicationNr = null, CreditApplicationHeader creditApplicationHeader = null)
        {
            var e = new CreditApplicationEvent
            {
                ApplicationNr = applicationNr,
                Application = creditApplicationHeader,
                EventType = eventCode.ToString(),
                EventDate = clock.Now,
                TransactionDate = clock.Today
            }.PopulateInfraFields(ntechCurrentUserMetadata, clock);
            return e;
        }

        public CreditApplicationEvent CreateAndAddEvent(CreditApplicationEventCode eventCode, string applicationNr = null, CreditApplicationHeader creditApplicationHeader = null)
        {
            var e = new CreditApplicationEvent
            {
                ApplicationNr = applicationNr,
                Application = creditApplicationHeader,
                EventType = eventCode.ToString(),
                EventDate = clock.Now,
                TransactionDate = clock.Today
            };
            var evt = CreateEvent(eventCode, applicationNr: applicationNr, creditApplicationHeader: creditApplicationHeader);
            this.CreditApplicationEvents.Add(evt);
            return evt;
        }

        public KeyValueItem CreateAndAddKeyValueItem(string keySpace, string key, string value)
        {
            var item = new KeyValueItem
            {
                Key = key,
                KeySpace = keySpace,
                Value = value
            }.PopulateInfraFields(ntechCurrentUserMetadata, clock);
            KeyValueItems.Add(item);
            return item;
        }

        public IQueryable<ComplexApplicationListItem> ComplexApplicationListItemsQueryable => ComplexApplicationListItems;
        public IQueryable<StandardPolicyFilterRuleSet> StandardPolicyFilterRuleSetsQueryable => StandardPolicyFilterRuleSets;
        public IQueryable<CreditApplicationListMember> CreditApplicationListMembersQueryable => CreditApplicationListMembers;
        public IQueryable<KeyValueItem> KeyValueItemsQueryable => KeyValueItems;
        public IQueryable<CreditApplicationHeader> CreditApplicationHeadersQueryable => CreditApplicationHeaders;
        public IQueryable<CreditApplicationHeader> CreditApplicationHeadersWithItemsIncludedQueryable => CreditApplicationHeaders.Include("Items");
        public IQueryable<CreditApplicationItem> CreditApplicationItemsQueryable => CreditApplicationItems;
        public IQueryable<CreditApplicationChangeLogItem> CreditApplicationChangeLogItemsQueryable => CreditApplicationChangeLogItems;
        public IQueryable<CreditDecisionItem> CreditDecisionItemsQueryable => CreditDecisionItems;
        public IQueryable<CreditApplicationCustomerListMember> CreditApplicationCustomerListMembersQueryable => CreditApplicationCustomerListMembers;
        public IQueryable<CreditApplicationDocumentHeader> CreditApplicationDocumentHeadersQueryable => CreditApplicationDocumentHeaders;
        public IQueryable<MortgageLoanCreditApplicationHeaderExtension> MortgageLoanCreditApplicationHeaderExtensionsQueryable => MortgageLoanCreditApplicationHeaderExtensions;
        public IQueryable<CreditApplicationOneTimeToken> CreditApplicationOneTimeTokensQueryable => CreditApplicationOneTimeTokens;
        public IQueryable<CreditApplicationComment> CreditApplicationCommentsQueryable => CreditApplicationComments;
        public IQueryable<FraudControl> FraudControlsQueryable => FraudControls;
        public IQueryable<AffiliateReportingEvent> AffiliateReportingEventsQueryable => AffiliateReportingEvents;
        public IQueryable<AffiliateReportingLogItem> AffiliateReportingLogItemsQueryable => AffiliateReportingLogItems;
        public IQueryable<HandlerLimitLevel> HandlerLimitLevelsQueryable => HandlerLimitLevels;

        public void AddComplexApplicationListItem(ComplexApplicationListItem item) => ComplexApplicationListItems.Add(item);
        public void AddHComplexApplicationListItem(HComplexApplicationListItem item) => HComplexApplicationListItems.Add(item);
        public void RemoveComplexApplicationListItem(ComplexApplicationListItem item) => ComplexApplicationListItems.Remove(item);
        public void AddStandardPolicyFilterRuleSets(params StandardPolicyFilterRuleSet[] ruleSets) => StandardPolicyFilterRuleSets.AddRange(ruleSets);
        public void AddCreditApplicationListMembers(params CreditApplicationListMember[] members) => CreditApplicationListMembers.AddRange(members);
        public void RemoveCreditApplicationListMembers(params CreditApplicationListMember[] members) => CreditApplicationListMembers.RemoveRange(members);
        public void AddCreditApplicationListOperations(params CreditApplicationListOperation[] operations) => CreditApplicationListOperations.AddRange(operations);
        public void AddKeyValueItems(params KeyValueItem[] items) => KeyValueItems.AddRange(items);
        public void RemoveKeyValueItems(params KeyValueItem[] items) => KeyValueItems.RemoveRange(items);
        public void AddCreditApplicationKeySequences(params CreditApplicationKeySequence[] sequences) => CreditApplicationKeySequences.AddRange(sequences);
        public void RemoveCreditApplicationItems(params CreditApplicationItem[] items) => CreditApplicationItems.AddRange(items);
        public void AddCreditApplicationItems(params CreditApplicationItem[] items) => CreditApplicationItems.AddRange(items);
        public void AddCreditApplicationChangeLogItems(params CreditApplicationChangeLogItem[] items) => CreditApplicationChangeLogItems.AddRange(items);
        public void AddCreditApplicationComments(params CreditApplicationComment[] comments) => CreditApplicationComments.AddRange(comments);
        public void AddCreditApplicationHeaders(params CreditApplicationHeader[] headers) => CreditApplicationHeaders.AddRange(headers);
        public void AddMortgageLoanCreditApplicationHeaderExtensions(params MortgageLoanCreditApplicationHeaderExtension[] extensions) => MortgageLoanCreditApplicationHeaderExtensions.AddRange(extensions);
        public void AddCreditApplicationCustomerListOperations(params CreditApplicationCustomerListOperation[] operations) => CreditApplicationCustomerListOperations.AddRange(operations);
        public void AddCreditApplicationCustomerListMembers(params CreditApplicationCustomerListMember[] members) => CreditApplicationCustomerListMembers.AddRange(members);
        public void RemoveCreditApplicationCustomerListMembers(params CreditApplicationCustomerListMember[] members) => CreditApplicationCustomerListMembers.RemoveRange(members);
        public void AddCreditApplicationDocumentHeaders(params CreditApplicationDocumentHeader[] documents) => CreditApplicationDocumentHeaders.AddRange(documents);
        public void AddCreditApplicationOneTimeTokens(params CreditApplicationOneTimeToken[] tokens) => CreditApplicationOneTimeTokens.AddRange(tokens);
        public void AddCreditDecisions(params CreditDecision[] creditDecisions) => CreditDecisions.AddRange(creditDecisions);
        public void AddCreditDecisionPauseItems(params CreditDecisionPauseItem[] pauseItems) => CreditDecisionPauseItems.AddRange(pauseItems);
        public void AddCreditDecisionSearchTerms(params CreditDecisionSearchTerm[] searchTerms) => CreditDecisionSearchTerms.AddRange(searchTerms);
        public void AddAffiliateReportingEvents(params AffiliateReportingEvent[] affiliateReportingEvents) => AffiliateReportingEvents.AddRange(affiliateReportingEvents);
        public void AddCreditApplicationCancellations(params CreditApplicationCancellation[] cancellations) => CreditApplicationCancellations.AddRange(cancellations);
    }
}
