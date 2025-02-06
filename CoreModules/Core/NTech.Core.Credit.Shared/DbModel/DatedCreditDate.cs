using NTech.Core.Module.Shared.Database;
using System;

namespace nCredit
{
    public enum DatedCreditDateCode
    {
        TerminationLettersPausedUntilDate,
        PromisedToPayDate,
        DebtCollectionPausedUntilDate,
        MortgageLoanInitialSettlementDate,
        MortgageLoanEndDate,
        /// <summary>
        /// Actual amortization will be ExceptionAmortizationAmount until this date is passed then it will fall back to ActualAmortizationAmount
        /// </summary>
        AmortizationExceptionUntilDate,
        MortgageLoanNextInterestRebindDate,
        LastMlRebindingReminderMessageDate
    }

    public class DatedCreditDate : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public CreditHeader Credit { get; set; }
        public string CreditNr { get; set; }
        public string Name { get; set; }
        public DateTime TransactionDate { get; set; }
        public BusinessEvent BusinessEvent { get; set; }
        public int BusinessEventId { get; set; }
        public BusinessEvent RemovedByBusinessEvent { get; set; }
        public int? RemovedByBusinessEventId { get; set; }
        public DateTime Value { get; set; }
    }
}