using NTech.Core.Module.Shared.Database;
using System;

namespace nPreCredit
{
    public enum CreditApplicationDocumentTypeCode
    {
        DocumentCheck,
        SignedAgreement,
        MortgageLoanDenuntiation,
        MortgageLoanCustomerAmortizationPlan,
        ProofOfIdentity,
        NotSignedAgreement,
        SignedDirectDebitConsent,
        MortgageLoanLagenhetsutdrag,
        ProxyAuthorization,
        Freeform,
        SignedApplicationAndPOA,
        SignedApplication,
        SignedPowerOfAttorney,
        MortgageLoanDocument
    }

    public class CreditApplicationDocumentHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public CreditApplicationHeader CreditApplication { get; set; }
        public string ApplicationNr { get; set; }
        public int? ApplicantNr { get; set; }
        public int? CustomerId { get; set; }
        public string DocumentType { get; set; }
        public DateTimeOffset AddedDate { get; set; }
        public int AddedByUserId { get; set; }
        public DateTimeOffset? RemovedDate { get; set; }
        public int? RemovedByUserId { get; set; }
        public string DocumentArchiveKey { get; set; }
        public string DocumentFileName { get; set; }
        public string DocumentSubType { get; set; }
        public DateTimeOffset? VerifiedDate { get; set; }
        public int? VerifiedByUserId { get; set; }
    }
}