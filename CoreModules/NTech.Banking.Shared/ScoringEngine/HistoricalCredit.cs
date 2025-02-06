using System;
using System.Collections.Generic;

namespace NTech.Banking.ScoringEngine
{
    public class HistoricalCredit
    {
        public string CreditNr { get; set; }
        public List<int> CustomerIds { get; set; }
        public int NrOfApplicants { get; set; }
        public string ProviderName { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public string Status { get; set; }
        public decimal CapitalBalance { get; set; }
        public decimal? MarginInterestRatePercent { get; set; }
        public decimal? AnnuityAmount { get; set; }
        public decimal? ReferenceInterestRatePercent { get; set; }
        public decimal? NotificationFeeAmount { get; set; }
        public DateTime? CurrentlyOverdueSinceDate { get; set; }
        public int? MaxNrOfDaysBetweenDueDateAndPaymentLastSixMonths { get; set; }
        public int? MaxNrOfDaysBetweenDueDateAndPaymentEver { get; set; }
        public bool IsOrHasBeenOnDebtCollection { get; set; }
        public int NrOfClosedNotifications { get; set; }
        public string ApplicationNr { get; set; }
        public bool IsMortgageLoan { get; set; }
    }
}
