namespace nPreCredit.Code.MortgageLoans
{
    public class MortgageLoanNotificationSettingsModel
    {
        public decimal ReminderFeeAmount { get; set; }
        public decimal OverdueNotificationLatePaymentInterestRateModifier { get; set; }
        public decimal TerminatedLoanLatePaymentInterestRateModifier { get; set; }
    }
}