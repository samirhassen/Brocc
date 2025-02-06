using System;

namespace nCreditReport
{
    public class AddressLookupCachedResult
    {
        public int Id { get; set; }
        public string ProviderName { get; set; }
        public int CustomerId { get; set; }
        public DateTimeOffset RequestDate { get; set; }
        public string EncryptionKeyName { get; set; }
        public byte[] EncryptedData { get; set; }
        public DateTimeOffset DeleteAfterDate { get; set; }
    }
}