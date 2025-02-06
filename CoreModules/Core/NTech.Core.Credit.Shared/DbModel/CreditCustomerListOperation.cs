using NTech.Core.Module.Shared.Database;
using System;

namespace nCredit
{
    public class CreditCustomerListOperation : InfrastructureBaseItem
    {
        public long Id { get; set; }
        public string CreditNr { get; set; }
        public CreditHeader Credit { get; set; }
        public string ListName { get; set; }
        public int CustomerId { get; set; }
        public bool IsAdd { get; set; }
        public DateTimeOffset OperationDate { get; set; }
        public int ByUserId { get; set; }
        public int? ByEventId { get; set; }
        public BusinessEvent ByEvent { get; set; }
    }
}