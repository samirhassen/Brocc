namespace nCredit.Code.MortgageLoans
{
    public class MortgageLoanNotificationSettingsModel
    {
        public decimal ReminderFeeAmount { get; set; }
        public decimal OverdueNotificationLatePaymentInterestRateModifier { get; set; }
        public decimal TerminatedLoanLatePaymentInterestRateModifier { get; set; }
        public int NotificationNotificationDay { get; set; }
        public int NotificationDueDay { get; set; }
        public int NrOfFreeInitialReminders { get; set; }
        public int MaxNrOfReminders { get; set; }
        public int? TerminationLetterDueDay { get; set; }
    }
}