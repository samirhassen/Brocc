namespace nPreCredit.Code
{
    public class AgreementSigningProviderShared
    {

        public const string SignedDocumentItemName = "signed_initial_agreement_key";

        public static string GetAlternateSignicatKey(string applicationNr)
        {
            return $"LoanAgreementSigningSessionV1_{applicationNr}";
        }
    }
}