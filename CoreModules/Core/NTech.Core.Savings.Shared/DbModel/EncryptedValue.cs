using System;

namespace NTech.Core.Savings.Shared.DbModel
{
    public class EncryptedValue
    {
        public long Id { get; set; }
        public string EncryptionKeyName { get; set; }
        public byte[] Value { get; set; }
        public byte[] Timestamp { get; set; }
        public int CreatedById { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
    }
}