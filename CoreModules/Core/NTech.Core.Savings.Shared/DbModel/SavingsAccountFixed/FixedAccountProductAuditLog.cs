using System;

namespace NTech.Core.Savings.Shared.DbModel.SavingsAccountFixed
{
    public class FixedAccountProductAuditLog
    {
        public long Id { get; set; }
        public string Message { get; set; }
        public string User { get; set; }
        public DateTime CreatedAt { get; set; }
        public BusinessEvent BusinessEvent { get; set; }
    }
}