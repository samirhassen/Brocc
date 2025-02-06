using System;
using System.Collections.Generic;
namespace nCustomer.DbModel
{
    public class CustomerMessage
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string ChannelType { get; set; }
        public string ChannelId { get; set; }
        public bool IsFromCustomer { get; set; }
        public string Text { get; set; }
        public string TextFormat { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedByUserId { get; set; }
        public int? HandledByUserId { get; set; }
        public DateTime? HandledDate { get; set; }
        public virtual List<CustomerMessageAttachedDocument> CustomerMessageAttachedDocuments { get; set; }
    }
}