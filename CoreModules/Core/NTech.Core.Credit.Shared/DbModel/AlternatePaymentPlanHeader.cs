using nCredit;
using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace NTech.Core.Credit.Shared.DbModel
{
    public class AlternatePaymentPlanHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string CreditNr { get; set; }
        public CreditHeader Credit { get; set; }
        public int CreatedByEventId { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public int? CancelledByEventId { get; set; }
        public BusinessEvent CancelledByEvent { get; set; }
        public int? FullyPaidByEventId { get; set; }
        /// <summary>
        /// When notifications are capitalized at the start of the paymentplan we store the earliest due date
        /// so it's at least in in principle possible to maintain an accurate performance metric (for things like IFRS)
        /// </summary>
        public DateTime? MinCapitalizedDueDate { get; set; }

        /// <summary>
        /// Nr of payments remaining on the amortization plan before the alternate payment plan was started.
        /// This is used when the plans ends to recalculate the annuity.
        /// </summary>
        public int FuturePaymentPlanMonthCount { get; set; }
        public BusinessEvent FullyPaidByEvent { get; set; }
        public virtual List<AlternatePaymentPlanMonth> Months { get; set; }
    }
}
