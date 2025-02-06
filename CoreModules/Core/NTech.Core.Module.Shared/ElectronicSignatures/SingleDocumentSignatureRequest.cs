using System.Collections.Generic;

namespace NTech.ElectronicSignatures
{
    public class SingleDocumentSignatureRequestUnvalidated
    {
        public string DocumentToSignArchiveKey { get; set; }
        public string DocumentToSignFileName { get; set; }
        public string RedirectAfterFailedUrl { get; set; }
        public string RedirectAfterSuccessUrl { get; set; }
        public string ServerToServerCallbackUrl { get; set; }
        public List<SigningCustomer> SigningCustomers { get; set; }
        public Dictionary<string, string> CustomData { get; set; }

        public class SigningCustomer
        {
            public int? SignerNr { get; set; }
            public string CivicRegNr { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public Dictionary<string, string> CustomData { get; set; }
        }
    }
}
