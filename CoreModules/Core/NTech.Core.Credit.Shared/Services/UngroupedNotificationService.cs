using NTech;
using System;

namespace nCredit.Code.Services
{
    public class UngroupedNotificationService
    {
        public static Tuple<bool, DateTime?, string> GetNotificationDueDateOrSkipReason(CreditNotificationStatusCommon credit, DateTime today, int? fixedDueDay)
        {
            Tuple<bool, DateTime?, string> dontNotify(string x)
            {
                return Tuple.Create(false, new DateTime?(), x);
            }

            Tuple<bool, DateTime?, string> notify(DateTime x)
            {
                return Tuple.Create(true, new DateTime?(x), (string)null);
            }

            if (credit.CreditStatus != CreditStatus.Normal.ToString())
                return dontNotify("Status");

            if (credit.IsCreditProcessSuspendedByTerminationLetter == true)
                return dontNotify("SuspendedByTerminationLetter");

            if (credit.IsStandardDefaultProcessSuspended == true)
                return dontNotify("StandardDefaultProcessSuspended");

            if(credit.IsMissingNotNotifiedCapital)
                return dontNotify("ZeroNotNotifiedCapital");

            var latestDueDate = Dates.MaxN(credit.LatestNotificationDueDate, credit.LatestPaymentFreeMonthDueDate);

            if(credit.SinglePaymentLoanRepaymentDays.HasValue)
            {
                if (credit.LatestNotificationDueDate.HasValue)
                    return dontNotify("AlreadyNotified");

                //At most 14, at least 10. Allow 10-14 variation based on SinglePaymentLoanRepaymentDays
                var minPaymentDaysAllowed = Math.Max(Math.Min(14, credit.SinglePaymentLoanRepaymentDays.Value), 10);
                var agreementDueDate = credit.CreditStartDate.AddDays(credit.SinglePaymentLoanRepaymentDays.Value);

                var minAllowedNotificationDate = agreementDueDate.AddDays(-minPaymentDaysAllowed);
                if (today < minAllowedNotificationDate)
                    return dontNotify($"Earliest possible notification date is: {minAllowedNotificationDate} ({minPaymentDaysAllowed} before due date {agreementDueDate})");

                return notify(Dates.Max(agreementDueDate, today.AddDays(minPaymentDaysAllowed)));                
            }
            else if(credit.PerLoanDueDay.HasValue || fixedDueDay.HasValue)
            {
                if(credit.LatestNotificationDueDate.HasValue && credit.LatestNotificationDueDate.Value >= today)
                    return dontNotify("AlreadyNotified");                

                var loanDueDay = fixedDueDay ?? credit.PerLoanDueDay.Value;
                var nextPotentialDueDate = Dates.GetNextDateWithDayNrAfterDate(loanDueDay, today);
                var nrOfDaysUntilNextPotentialDueDate = Dates.GetAbsoluteNrOfDaysBetweenDates(nextPotentialDueDate, today);
                var nrOfDaysSinceCreditCreated = Dates.GetAbsoluteNrOfDaysBetweenDates(credit.CreditStartDate, today);
                if (!credit.LatestNotificationDueDate.HasValue)
                {
                    if (nrOfDaysSinceCreditCreated < 28 && nrOfDaysUntilNextPotentialDueDate < 14)
                        return dontNotify($"First notification: {nrOfDaysUntilNextPotentialDueDate} days until {nextPotentialDueDate:yyyy-MM-dd}. Must be exactly 14 during the first month.");
                }

                var nrOfDaysBetweenDueDates = latestDueDate.HasValue ? new int?(Dates.GetAbsoluteNrOfDaysBetweenDates(latestDueDate.Value, nextPotentialDueDate)) : null;

                //At least 7 days until the due date
                //At most 14 days until the due date
                if (nrOfDaysUntilNextPotentialDueDate < 7 || nrOfDaysUntilNextPotentialDueDate > 14)
                    return dontNotify($"{nrOfDaysUntilNextPotentialDueDate} days until {nextPotentialDueDate:yyyy-MM-dd}. Must be between 7 and 14.");

                //At least three weeks between due dates
                if (nrOfDaysBetweenDueDates.HasValue && nrOfDaysBetweenDueDates.Value < 21)
                    return dontNotify($"{nrOfDaysBetweenDueDates.Value} days between due dates {latestDueDate.Value:yyyy-MM-dd} and {nextPotentialDueDate:yyyy-MM-dd}. Must be at least 21 days.");

                return notify(nextPotentialDueDate);
            }
            else
                throw new Exception($"Credit {credit.CreditNr} is missing PerLoanDueDay");
        }

        public class CreditNotificationStatusCommon
        {
            public string CreditNr { get; set; }  
            public string CreditStatus { get; set; }
            public int? Applicant1CustomerId { get; set; }
            public DateTime CreditStartDate { get; set; }
            public DateTime? LatestNotificationDueDate { get; set; }
            public DateTime? LatestPaymentFreeMonthDueDate { get; set; }
            public int? PerLoanDueDay { get; set; }
            public bool? IsCreditProcessSuspendedByTerminationLetter { get; set; }
            public bool? IsStandardDefaultProcessSuspended { get; set; }
            public int? SinglePaymentLoanRepaymentDays { get; set; }
            public bool IsMissingNotNotifiedCapital { get; set; }
        }

    }
}