using nPreCredit;
using nPreCredit.DbModel;
using NTech.Core.Module.Shared.Database;
using System.Linq;

namespace NTech.Core.PreCredit.Shared
{
    /// <summary>
    /// This needs to exist since DbSet[T] is not the same type for ef and ef core so we cant just have a shared baseclass.
    /// The [X]Queryable rather than just [X] is because of weak generic support. It would be much better to not have these to keep the amount of admin down.
    /// </summary>
    public interface IPreCreditContextExtended : INTechDbContext
    {
        ICoreClock CoreClock { get; }
        int CurrentUserId { get; }
        string InformationMetadata { get; }
        int SaveChanges();
        T FillInfrastructureFields<T>(T b) where T : InfrastructureBaseItem;

        CreditApplicationEvent CreateAndAddEvent(CreditApplicationEventCode eventCode, string applicationNr, CreditApplicationHeader creditApplicationHeader);

        IQueryable<ComplexApplicationListItem> ComplexApplicationListItemsQueryable { get; }
        IQueryable<StandardPolicyFilterRuleSet> StandardPolicyFilterRuleSetsQueryable { get; }
        IQueryable<CreditApplicationListMember> CreditApplicationListMembersQueryable { get; }
        IQueryable<KeyValueItem> KeyValueItemsQueryable { get; }
        IQueryable<CreditApplicationItem> CreditApplicationItemsQueryable { get; }
        IQueryable<CreditApplicationChangeLogItem> CreditApplicationChangeLogItemsQueryable { get; }
        IQueryable<CreditDecisionItem> CreditDecisionItemsQueryable { get; }
        IQueryable<CreditApplicationHeader> CreditApplicationHeadersQueryable { get; }
        //NOTE: This is really hacky but the code that depends on this include is tricky to test
        IQueryable<CreditApplicationHeader> CreditApplicationHeadersWithItemsIncludedQueryable { get; }
        IQueryable<CreditApplicationCustomerListMember> CreditApplicationCustomerListMembersQueryable { get; }
        IQueryable<CreditApplicationOneTimeToken> CreditApplicationOneTimeTokensQueryable { get; }
        IQueryable<MortgageLoanCreditApplicationHeaderExtension> MortgageLoanCreditApplicationHeaderExtensionsQueryable { get; }
        IQueryable<CreditApplicationDocumentHeader> CreditApplicationDocumentHeadersQueryable { get; }
        IQueryable<CreditApplicationComment> CreditApplicationCommentsQueryable { get; }
        IQueryable<FraudControl> FraudControlsQueryable { get; }
        IQueryable<AffiliateReportingEvent> AffiliateReportingEventsQueryable { get; }
        IQueryable<AffiliateReportingLogItem> AffiliateReportingLogItemsQueryable { get; }
        IQueryable<HandlerLimitLevel> HandlerLimitLevelsQueryable { get; }

        void AddHComplexApplicationListItem(HComplexApplicationListItem item);
        void RemoveComplexApplicationListItem(ComplexApplicationListItem item);
        void AddComplexApplicationListItem(ComplexApplicationListItem item);
        void AddStandardPolicyFilterRuleSets(params StandardPolicyFilterRuleSet[] ruleSets);
        KeyValueItem CreateAndAddKeyValueItem(string keySpace, string key, string value);
        void AddCreditApplicationListMembers(params CreditApplicationListMember[] members);
        void RemoveCreditApplicationListMembers(params CreditApplicationListMember[] members);
        void AddCreditApplicationListOperations(params CreditApplicationListOperation[] operations);
        void AddKeyValueItems(params KeyValueItem[] items);
        void RemoveKeyValueItems(params KeyValueItem[] items);
        void AddCreditApplicationKeySequences(params CreditApplicationKeySequence[] sequences);
        void RemoveCreditApplicationItems(params CreditApplicationItem[] items);
        void AddCreditApplicationItems(params CreditApplicationItem[] items);
        void AddCreditApplicationChangeLogItems(params CreditApplicationChangeLogItem[] items);
        void AddCreditApplicationComments(params CreditApplicationComment[] comments);
        void AddCreditApplicationHeaders(params CreditApplicationHeader[] headers);
        void AddMortgageLoanCreditApplicationHeaderExtensions(params MortgageLoanCreditApplicationHeaderExtension[] extensions);
        void AddCreditApplicationCustomerListOperations(params CreditApplicationCustomerListOperation[] operations);
        void AddCreditApplicationCustomerListMembers(params CreditApplicationCustomerListMember[] members);
        void RemoveCreditApplicationCustomerListMembers(params CreditApplicationCustomerListMember[] members);
        void AddCreditApplicationOneTimeTokens(params CreditApplicationOneTimeToken[] tokens);
        void AddCreditApplicationDocumentHeaders(params CreditApplicationDocumentHeader[] documents);
        void AddCreditDecisions(params CreditDecision[] creditDecisions);
        void AddCreditDecisionPauseItems(params CreditDecisionPauseItem[] pauseItems);
        void AddCreditDecisionSearchTerms(params CreditDecisionSearchTerm[] searchTerms);
        void AddAffiliateReportingEvents(params AffiliateReportingEvent[] affiliateReportingEvents);
        void AddCreditApplicationCancellations(params CreditApplicationCancellation[] cancellations);
    }
}
