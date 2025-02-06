using System;
using System.Collections.Generic;

namespace nCustomer.Code.Services
{
    public class CustomerMessageModel
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string TextFormat { get; set; }
        public int CustomerId { get; set; }
        public bool IsFromCustomer { get; set; }
        public DateTime CreationDate { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime? HandledDate { get; set; }
        public int? HandledByUserId { get; set; }
        public string ChannelType { get; set; }
        public string ChannelId { get; set; }
        public List<CustomerMessageAttachedDocumentModel> AttachedDocuments { get; set; }
    }
}