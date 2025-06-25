using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;

namespace NTech.Core.Savings.Shared.DbModel
{
    public class SharedSavingsInterestRate : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string AccountTypeCode { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime ValidFromDate { get; set; }
        public BusinessEvent BusinessEvent { get; set; }
        public int BusinessEventId { get; set; }
        public decimal InterestRatePercent { get; set; }
        public BusinessEvent RemovedByBusinessEvent { get; set; }
        public int? RemovedByBusinessEventId { get; set; }
        public BusinessEvent AppliesToAccountsSinceBusinessEvent { get; set; }
        public int? AppliesToAccountsSinceBusinessEventId { get; set; }
        public virtual List<SharedSavingsInterestRateChangeHeader> AllAccountsHeaders { get; set; }
        public virtual List<SharedSavingsInterestRateChangeHeader> NewAccountsHeaders { get; set; }
    }
}