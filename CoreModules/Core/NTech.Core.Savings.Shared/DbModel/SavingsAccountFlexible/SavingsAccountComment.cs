using System;
using NTech.Core.Module.Shared.Database;

namespace NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible
{
    public class SavingsAccountComment : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public SavingsAccountHeader SavingsAccount { get; set; }
        public string SavingsAccountNr { get; set; }
        public string EventType { get; set; }
        public DateTimeOffset CommentDate { get; set; }
        public string Attachment { get; set; }
        public int CommentById { get; set; }
        public string CommentText { get; set; }
        public int? CustomerSecureMessageId { get; set; }
    }
}