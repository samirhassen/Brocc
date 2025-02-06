namespace nCustomerPages.Code.ElectronicIdSignature
{
    public class SavingsAgreementElectronicIdSignatureResult
    {
        public bool Success { get; set; }
        public string SignedAgreementArchiveKey { get; set; }
        public string PlainData { get; set; }
    }
}
