using nPreCredit.DbModel;
using NTech;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.PreCredit.Shared;
using NTech.Legacy.Module.Shared.Infrastructure;
using System.Linq;

namespace nPreCredit
{
    public class PreCreditContextExtended : PreCreditContextExtendedBase, IPreCreditContextExtended
    {
        //NOTE: clock intentionally ignored. Need to refactor the 100+ uses of this to use core clock instead but dont want the current pr to have that massive diff so leaving if for the future.
        public PreCreditContextExtended(INTechCurrentUserMetadata ntechCurrentUserMetadata, IClock clock) : this(ntechCurrentUserMetadata.UserId, NTech.Legacy.Module.Shared.Infrastructure.CoreClock.SharedInstance, ntechCurrentUserMetadata.InformationMetadata)
        {

        }

        public PreCreditContextExtended(INTechCurrentUserMetadata ntechCurrentUserMetadata, ICombinedClock clock) : this(ntechCurrentUserMetadata.UserId, clock, ntechCurrentUserMetadata.InformationMetadata)
        {
        }

        //NOTE: clock intentionally ignored. Need to refactor the 100+ uses of this to use core clock instead but dont want the current pr to have that massive diff so leaving if for the future.
        public PreCreditContextExtended(int currentUserId, IClock clock, string informationMetadata) : this(currentUserId, NTech.Legacy.Module.Shared.Infrastructure.CoreClock.SharedInstance, informationMetadata)
        {

        }

        public PreCreditContextExtended(int currentUserId, ICombinedClock clock, string informationMetadata) : base(currentUserId, clock, informationMetadata)
        {

        }

        public IQueryable<StandardPolicyFilterRuleSet> StandardPolicyFilterRuleSetsQueryable => StandardPolicyFilterRuleSets;
        public IQueryable<ComplexApplicationListItem> ComplexApplicationListItemsQueryable => ComplexApplicationListItems;
        public IQueryable<CreditApplicationListMember> CreditApplicationListMembersQueryable => CreditApplicationListMembers;
        public IQueryable<KeyValueItem> KeyValueItemsQueryable => KeyValueItems;
        public IQueryable<CreditApplicationItem> CreditApplicationItemsQueryable => CreditApplicationItems;
        public IQueryable<CreditApplicationChangeLogItem> CreditApplicationChangeLogItemsQueryable => CreditApplicationChangeLogItems;
        public IQueryable<CreditDecisionItem> CreditDecisionItemsQueryable => CreditDecisionItems;
        public IQueryable<CreditApplicationHeader> CreditApplicationHeadersQueryable => CreditApplicationHeaders;
        public IQueryable<CreditApplicationHeader> CreditApplicationHeadersWithItemsIncludedQueryable => CreditApplicationHeaders.Include("Items");
        public IQueryable<CreditApplicationCustomerListMember> CreditApplicationCustomerListMembersQueryable => CreditApplicationCustomerListMembers;
        public IQueryable<MortgageLoanCreditApplicationHeaderExtension> MortgageLoanCreditApplicationHeaderExtensionsQueryable => MortgageLoanCreditApplicationHeaderExtensions;
        public IQueryable<CreditApplicationOneTimeToken> CreditApplicationOneTimeTokensQueryable => CreditApplicationOneTimeTokens;
        public IQueryable<CreditApplicationDocumentHeader> CreditApplicationDocumentHeadersQueryable => CreditApplicationDocumentHeaders;
        public IQueryable<CreditApplicationComment> CreditApplicationCommentsQueryable => CreditApplicationComments;
        public IQueryable<FraudControl> FraudControlsQueryable => FraudControls;
        public IQueryable<AffiliateReportingEvent> AffiliateReportingEventsQueryable => AffiliateReportingEvents;
        public IQueryable<AffiliateReportingLogItem> AffiliateReportingLogItemsQueryable => AffiliateReportingLogItems;
        public IQueryable<HandlerLimitLevel> HandlerLimitLevelsQueryable => HandlerLimitLevels;

        public void AddHComplexApplicationListItem(HComplexApplicationListItem item) => HComplexApplicationListItems.Add(item);
        public void RemoveComplexApplicationListItem(ComplexApplicationListItem item) => ComplexApplicationListItems.Remove(item);
        public void AddComplexApplicationListItem(ComplexApplicationListItem item) => ComplexApplicationListItems.Add(item);
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
        public void AddCreditApplicationOneTimeTokens(params CreditApplicationOneTimeToken[] tokens) => CreditApplicationOneTimeTokens.AddRange(tokens);
        public void AddCreditApplicationDocumentHeaders(params CreditApplicationDocumentHeader[] documents) => CreditApplicationDocumentHeaders.AddRange(documents);
        public void AddCreditDecisions(params CreditDecision[] creditDecisions) => CreditDecisions.AddRange(creditDecisions);
        public void AddCreditDecisionPauseItems(params CreditDecisionPauseItem[] pauseItems) => CreditDecisionPauseItems.AddRange(pauseItems);
        public void AddCreditDecisionSearchTerms(params CreditDecisionSearchTerm[] searchTerms) => CreditDecisionSearchTerms.AddRange(searchTerms);
        public void AddAffiliateReportingEvents(params AffiliateReportingEvent[] affiliateReportingEvents) => AffiliateReportingEvents.AddRange(affiliateReportingEvents);
        public void AddCreditApplicationCancellations(params CreditApplicationCancellation[] cancellations) => CreditApplicationCancellations.AddRange(cancellations);
    }
}