using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nPreCredit
{
    public class CreditApplicationHeader : InfrastructureBaseItem
    {
        public string ApplicationNr { get; set; }
        public string ApplicationType { get; set; }
        public string ProviderName { get; set; }
        public int NrOfApplicants { get; set; }
        public DateTimeOffset ApplicationDate { get; set; }
        public DateTimeOffset? ArchivedDate { get; set; }
        public int? ArchiveLevel { get; set; }
        public bool IsActive { get; set; }
        public string CreditCheckStatus { get; set; }
        public CreditDecision CurrentCreditDecision { get; set; }
        public int? CurrentCreditDecisionId { get; set; }
        public virtual List<CreditDecision> CreditDecisions { get; set; }
        public virtual List<FraudControl> FraudControls { get; set; }
        public string CustomerCheckStatus { get; set; }
        public string AgreementStatus { get; set; }
        public string FraudCheckStatus { get; set; }
        public bool IsFinalDecisionMade { get; set; }
        public int? FinalDecisionById { get; set; }
        public bool IsRejected { get; set; }
        public DateTimeOffset? RejectedDate { get; set; }
        public int? RejectedById { get; set; }
        public DateTimeOffset? FinalDecisionDate { get; set; }
        public bool IsPartiallyApproved { get; set; }
        public int? PartiallyApprovedById { get; set; }
        public bool IsCancelled { get; set; }
        public DateTimeOffset? CancelledDate { get; set; }
        public int? CancelledBy { get; set; }
        public string CancelledState { get; set; }
        public DateTimeOffset? PartiallyApprovedDate { get; set; }
        public DateTimeOffset? WaitingForAdditionalInformationDate { get; set; } //Waiting for feedback from the customer basically. Overrides most other statuses.
        public DateTimeOffset? HideFromManualListsUntilDate { get; set; } //To allow automation time to work without the uses having to fight with it
        public bool CanSkipAdditionalQuestions { get; set; } //When an application already has the required information included in the initial data we can skip this step
        public virtual List<CreditApplicationSearchTerm> SearchTerms { get; set; }
        public virtual List<CreditApplicationItem> Items { get; set; }
        public virtual List<CreditApplicationComment> Comments { get; set; }
        public virtual List<CreditApplicationOneTimeToken> OneTimeTokens { get; set; }
        public virtual List<CreditApprovalBatchItem> Approvals { get; set; }
        public virtual List<CreditApplicationCancellation> Cancellations { get; set; }
        public virtual MortgageLoanCreditApplicationHeaderExtension MortgageLoanExtension { get; set; }
        public virtual List<CreditApplicationEvent> Events { get; set; }
        public virtual List<CreditApplicationDocumentHeader> Documents { get; set; }
        public virtual List<CreditApplicationPauseItem> PauseItems { get; set; }
        public virtual List<CreditApplicationListMember> ListMemberships { get; set; }
        public virtual List<CreditApplicationListOperation> ListMembershipOperations { get; set; }
        public virtual List<CreditApplicationCustomerListMember> CustomerListMemberships { get; set; }
        public virtual List<CreditApplicationCustomerListOperation> CustomerListMembershipOperations { get; set; }
        public virtual List<ComplexApplicationListItem> ComplexApplicationListItems { get; set; }
    }

    public enum CreditApplicationTypeCode
    {
        unsecuredLoan,
        mortgageLoan,
        companyLoan,
    }
}