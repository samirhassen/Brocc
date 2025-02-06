using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nPreCredit
{
    public enum CreditApplicationEventCode
    {
        CreditApplicationCreated,
        CreditApplicationDocumentCheckAccepted,
        CreditApplicationDocumentCheckRejected,
        CreditApplicationDocumentCheckRestarted,
        CreditApplicationUserChoseDocumentSource,
        CreditApplicationUserAddedDocuments,
        MortgageLoanInitialScoringAccepted,
        MortgageLoanInitialScoringRejected,
        CreditApplicationInitialScoringRejected,
        MortgageLoanInitialAcceptedOfferReportedToProvider,
        MortgageLoanInitialRejectionReportedToProvider,
        MortgageLoanInitialOfferAcceptedByCustomer,
        MortgageLoanInitialOfferRejectedByCustomer,
        MortgageLoanFinalScoringRejected,
        MortgageLoanFinalScoringAccepted,
        MortgageLoanAnsweredAdditionalQuestions,
        MortgageLoanApproved,
        MortgageLoanCreated,
        MortgageLoanApprovalRevoked,
        CompanyLoanApplicationCreated,
        CompanyLoanInitialCreditCheck,
        CompanyLoanAnsweredAdditionalQuestions,
        CompanyLoanKycCheckAccepted,
        CompanyLoanFraudCheckAccepted,
        CompanyLoanAgreementAccepted,
        CreditApplicationItemEdited,
        MortgageLoanApplicationCreated,
        ComplexApplicationListChange,
        SharedBankAccountDataAttached,
        UnsecuredLoanStandardCreditCheck,
        MortgageLoanStandardInitialCreditCheck,
        MortgageLoanStandardFinalCreditCheck,
    }

    public class CreditApplicationEvent : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string EventType { get; set; }
        public DateTimeOffset EventDate { get; set; }
        public DateTime TransactionDate { get; set; }
        public CreditApplicationHeader Application { get; set; }
        public string ApplicationNr { get; set; }
        public virtual List<MortgageLoanCreditApplicationHeaderExtension> CreatedExtensions { get; set; }
        public virtual List<CreditApplicationListOperation> CreatedCreditApplicationListOperations { get; set; }
        public virtual List<CreditApplicationCustomerListOperation> CreatedCreditApplicationCustomerListOperations { get; set; }
        public virtual List<CreditApplicationChangeLogItem> CreatedCreditApplicationChangeLogItems { get; set; }
        public virtual List<ComplexApplicationListItem> CreatedComplexApplicationListItems { get; set; }
        public virtual List<ComplexApplicationListItem> ChangedComplexApplicationListItems { get; set; }
        public virtual List<HComplexApplicationListItem> CreatedHComplexApplicationListItems { get; set; }
    }
}