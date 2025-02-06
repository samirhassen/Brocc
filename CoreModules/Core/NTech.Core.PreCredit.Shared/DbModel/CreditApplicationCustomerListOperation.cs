using System;

namespace nPreCredit
{
    public class CreditApplicationCustomerListOperation
    {
        public long Id { get; set; }
        public string ApplicationNr { get; set; }
        public CreditApplicationHeader CreditApplication { get; set; }
        public string ListName { get; set; }
        public int CustomerId { get; set; }
        public bool IsAdd { get; set; }
        public DateTimeOffset OperationDate { get; set; }
        public int ByUserId { get; set; }
        public int? CreditApplicationEventId { get; set; }
        public CreditApplicationEvent ByEvent { get; set; }
    }
}