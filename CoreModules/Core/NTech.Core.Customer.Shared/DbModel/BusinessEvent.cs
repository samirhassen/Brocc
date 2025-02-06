using nCustomer.DbModel;
using System;
using System.Collections.Generic;

namespace nCustomer
{
    public class BusinessEvent
    {
        public int Id { get; set; }
        public string EventType { get; set; }
        public int UserId { get; set; }
        public DateTimeOffset EventDate { get; set; }
        public DateTime TransactionDate { get; set; }
        public virtual List<CustomerProperty> CreatedProperties { get; set; }
    }

    public enum BusinessEventCode
    {
        Generic
    }
}