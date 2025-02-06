using NTech.Core.Module.Shared.Database;
using System;

namespace nSavings
{
    public enum SharedDatedValueCode
    {

    }

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