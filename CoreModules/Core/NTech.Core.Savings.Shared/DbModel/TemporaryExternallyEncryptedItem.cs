using System;

namespace nSavings
{
    public class TemporaryExternallyEncryptedItem
    {
        //These items are encrypted with a temporary key that the system intentionally doesnt know about
        public string Id { get; set; }
        public string CipherText { get; set; }
        public string ProtocolVersionName { get; set; }
        public DateTime AddedDate { get; set; }
        public DateTime DeleteAfterDate { get; set; }
    }
}