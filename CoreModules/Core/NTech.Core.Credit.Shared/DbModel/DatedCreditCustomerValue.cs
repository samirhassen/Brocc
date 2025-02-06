using NTech.Core.Module.Shared.Database;
using System;

namespace nCredit
{
    public enum DatedCreditCustomerValueCode
    {
        OwnerShipPercent
    }

    //For things like annuity and base/margin interest rate that can change over time but where the historical values have impact
    public class DatedCreditCustomerValue : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public CreditHeader Credit { get; set; }
        public string CreditNr { get; set; }
        public string Name { get; set; }
        public DateTime TransactionDate { get; set; }
        public BusinessEvent BusinessEvent { get; set; }
        public int BusinessEventId { get; set; }
        public decimal Value { get; set; }
        public int CustomerId { get; set; }
    }
}