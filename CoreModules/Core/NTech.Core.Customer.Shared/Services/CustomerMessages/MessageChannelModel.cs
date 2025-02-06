using System;

namespace nCustomer.Code.Services
{
    public class MessageChannelModel
    {
        public int CustomerId { get; set; }
        public string ChannelType { get; set; }
        public string ChannelId { get; set; }
        public bool IsRelation { get; set; } //General is not
        public DateTime? RelationStartDate { get; set; }
        public DateTime? RelationEndDate { get; set; }
    }
}