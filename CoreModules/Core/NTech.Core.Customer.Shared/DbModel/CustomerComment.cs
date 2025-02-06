using NTech.Core.Module.Shared.Database;
using System;

namespace nCustomer.DbModel
{
    public class CustomerComment : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public DateTimeOffset CommentDate { get; set; }
        public string Attachment { get; set; }
        public int CommentById { get; set; }
        public string EventType { get; set; }
        public string CommentText { get; set; }
    }
}