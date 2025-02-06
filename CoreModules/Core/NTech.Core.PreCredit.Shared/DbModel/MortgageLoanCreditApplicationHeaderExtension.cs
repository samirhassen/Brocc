using NTech.Core.Module.Shared.Database;

namespace nPreCredit
{
    public class MortgageLoanCreditApplicationHeaderExtension : InfrastructureBaseItem
    {
        public string ApplicationNr { get; set; }
        public CreditApplicationHeader Application { get; set; }
        public CreditApplicationEvent CreatedByBusinessEvent { get; set; }
        public int CreatedByBusinessEventId { get; set; }
        public string CustomerOfferStatus { get; set; }
        public string DocumentCheckStatus { get; set; }
        public string AdditionalQuestionsStatus { get; set; }
        public string InitialCreditCheckStatus { get; set; }
        public string FinalCreditCheckStatus { get; set; }
        public string DirectDebitCheckStatus { get; set; }
    }

    public enum MortgageLoanCustomerOfferStatusCode
    {
        Initial,
        OfferCreated,
        OfferSent,
        OfferAcceptedByCustomer,
        OfferRejectedByCustomer,
    }
    public enum MortgageLoanDocumentCheckStatus
    {
        Initial,
        Accepted,
        Rejected
    }

    public enum MortgageLoanAdditionalQuestionsStatusCode
    {
        Initial,
        Pending,
        Answered,
        Accepted
    }

    public enum MortgageLoanDirectDebitCheckStatusCode
    {
        Initial,
        Pending,
        Accepted
    }
}