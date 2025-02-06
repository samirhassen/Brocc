using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nCustomer.DbModel
{
    public enum KycScreeningQueryResultListNameCode
    {
        Pep,
        Sanction
    }
    public class TrapetsQueryResult : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public DateTime QueryDate { get; set; }
        public bool IsPepHit { get; set; }
        public bool IsSanctionHit { get; set; }
        public virtual List<TrapetsQueryResultItem> Items { get; set; }
    }
}