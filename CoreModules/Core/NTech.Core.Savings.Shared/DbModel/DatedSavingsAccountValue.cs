using System;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;

namespace NTech.Core.Savings.Shared.DbModel
{
    public enum DatedSavingsAccountValueCode
    {

    }

    public class DatedSavingsAccountValue : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public SavingsAccountHeader SavingsAccount { get; set; }
        public string SavingsAccountNr { get; set; }
        public string Name { get; set; }
        public DateTime TransactionDate { get; set; }
        public BusinessEvent BusinessEvent { get; set; }
        public int BusinessEventId { get; set; }
        public decimal Value { get; set; }
    }
}