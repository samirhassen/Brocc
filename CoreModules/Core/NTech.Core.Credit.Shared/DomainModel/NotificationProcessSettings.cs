using System;
using static nCredit.DbModel.DomainModel.NotificationProcessSettings;

namespace nCredit.DbModel.DomainModel
{
    public class NotificationProcessSettings: IAmortizationPlanNotificationSettings
    {
        public static int DefaultFirstReminderDaysBefore = 7;

        public int NotificationNotificationDay { get; set; }
        public int NotificationDueDay { get; set; }
        public int? PaymentFreeMonthMinNrOfPaidNotifications { get; set; }
        public bool PaymentFreeMonthExcludeNotificationFee { get; set; }
        public int PaymentFreeMonthMaxNrPerYear { get; set; }
        public decimal ReminderFeeAmount { get; set; }
        public int ReminderMinDaysBetween { get; set; }
        public decimal SkipReminderLimitAmount { get; set; }
        public int NotificationOverDueGraceDays { get; set; }
        public int MaxNrOfReminders { get; set; }
        public int NrOfFreeInitialReminders { get; set; }
        public int? MaxNrOfRemindersWithFees { get; set; }
        public int? TerminationLetterDueDay { get; set; }
        public int? FirstReminderDaysBefore { get; set; }
        public bool AreBackToBackPaymentFreeMonthsAllowed { get; set; }

        /// <summary>
        /// If not delivered over snailmail the address requirement can be relaxed
        /// </summary>
        public bool AllowMissingCustomerAddress { get; set; }
        public int MinMonthsBetweenPaymentFreeMonths { get; set; }

        public int GetNrOfPaidReminders()
        {
            var n = Math.Max(MaxNrOfReminders - NrOfFreeInitialReminders, 0);

            if (MaxNrOfRemindersWithFees.HasValue)
                n = Math.Min(MaxNrOfRemindersWithFees.Value, n);

            return n;
        }

        public decimal GetMaxTotalReminderFeePerNotification()
        {
            return GetNrOfPaidReminders() * ReminderFeeAmount;
        }

        public interface IAmortizationPlanNotificationSettings
        {
            int NotificationNotificationDay { get; set; }
            int NotificationDueDay { get; set; }
        }
    }
}