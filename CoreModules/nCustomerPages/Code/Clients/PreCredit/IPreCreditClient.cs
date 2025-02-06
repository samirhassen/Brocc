namespace nCustomerPages.Code
{
    public interface IPreCreditClient
    {
        CreditApplicationResponse CreateCreditApplication(CreditApplicationRequest request, bool? disableAutomation);
        MortgageLoanAdditionalQuestionsStatusResponse GetMortgageLoanAdditionalQuestionsStatus(MortgageLoanAdditionalQuestionsStatusRequest request, bool? disableAutomation);
        void AnswerMortgageLoanAdditionalQuestions(MortgageLoanAnswerAdditionalQuestionsRequest request, bool? disableAutomation);
        MortgageLoanInitialScoringResponse ScoreMortgageLoanApplicationInitial(string applicationNr, bool skipProviderCallback, bool wasReportedDirectlyToProvider);
        ApplicationDocumentResponse GetApplicationDocument(string applicationNr, string externalId, string providerName, string documentType);
        string StoreTemporarilyEncryptedData(string plaintextMessage, int? expireAfterHours);
        bool TryGetTemporarilyEncryptedData(string compoundKey, out string plaintextMessage, bool removeAfter = false);
        MortgageLoanApplicationFinalCreditCheckStatusModel FetchMortgageLoanFinalStatus(string applicationNr);
        MortgageLoanApplicationResponse CreateMortgageLoanApplication(MortgageLoanApplicationRequest request, bool? disableAutomation, bool skipDirectScoring);
    }
}