namespace nCustomerPages.Code
{
    public class MortgageLoanApplicationFinalCreditCheckStatusModel
    {
        public bool HasNonExpiredBindingOffer { get; set; }
        public bool IsNewCreditCheckPossible { get; set; }
        public string UnsignedAgreementDocumentArchiveKey { get; set; }
        public string CreditCheckStatus { get; set; }
        public bool IsViewDecisionPossible { get; set; }
    }
}