using NTech.Core.Module.Shared.Database;
using System;

namespace NTech.Core.Credit.Shared.DbModel
{
    public class AlternatePaymentPlanMonth : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public int AlternatePaymentPlanId { get; set; }
        public AlternatePaymentPlanHeader PaymentPlan { get; set; }
        public DateTime DueDate { get; set; }
        public decimal MonthAmount { get; set; }
        /// <summary>
        /// All month amounts up to and including this month.
        /// If the sum of all payments placed against the credit from after AlternatePaymentPlanHeader.CreatedByEvent.TransactionDate >= TotalAmount then the customer is on plan.
        /// </summary>
        public decimal TotalAmount { get; set; }
    }
}
