using NTech.Core.Module.Shared.Database;
using System;

namespace nCredit
{
    public enum SharedDatedValueCode
    {
        ReferenceInterestRate,
    }
    //For things like reference interest rate that can change over time but where the historical values have impact
    public class SharedDatedValue : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime TransactionDate { get; set; }
        public BusinessEvent BusinessEvent { get; set; }
        public int BusinessEventId { get; set; }
        public decimal Value { get; set; }
    }
}