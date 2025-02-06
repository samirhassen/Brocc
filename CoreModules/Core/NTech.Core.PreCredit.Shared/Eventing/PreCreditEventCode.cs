namespace nPreCredit
{
    public enum PreCreditEventCode
    {
        CreditApplicationCreated,
        CreditApplicationCreditCheckRejected,
        CreditApplicationCreditCheckAccepted,
        CreditApplicationRejected,
        CreditApplicationAdditionalQuestionsSent,
        CreditApplicationPartiallyApproved,
        SignedAgreementAdded,
        CreditApplicationExternalProviderEvent,
        TimeMachineTimeChanged,
        MortgageLoanInitialCreditCheckRejected,
        MortgageLoanInitialCreditCheckAccepted,
        MortgageLoanPartiallyApproved,
        MortgageLoanFinalCreditCheckRejected,
        MortgageLoanFinalCreditCheckAccepted,
        MortgageLoanAddedSignedAgreement,
        MortgageLoanCreated,
        SettingChanged
    }
}