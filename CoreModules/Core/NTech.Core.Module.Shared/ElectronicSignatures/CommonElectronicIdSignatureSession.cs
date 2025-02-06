using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.ElectronicSignatures
{
    public class CommonElectronicIdSignatureSession
    {
        public string Id { get; set; }
        public string ProviderSessionId { get; set; }
        public string SignatureProviderName { get; set; }
        public string RedirectAfterFailedUrl { get; set; }
        public string RedirectAfterSuccessUrl { get; set; }
        public string ServerToServerCallbackUrl { get; set; }
        public Dictionary<int, SigningCustomer> SigningCustomersBySignerNr { get; set; }
        public Dictionary<string, string> CustomData { get; set; }
        public string GetCustomDataOpt(string name) => (CustomData?.ContainsKey(name) ?? false) ? CustomData[name] : null;
        public PdfModel UnsignedPdf { get; set; }
        public PdfModel SignedPdf { get; set; }
        public DateTime? ClosedDate { get; set; }
        public string ClosedMessage { get; set; }

        public class PdfModel
        {
            public string ArchiveKey { get; set; }
            public string FileName { get; set; }
        }

        public class SigningCustomer
        {
            public int SignerNr { get; set; }
            public string CivicRegNr { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public DateTime? SignedDateUtc { get; set; }
            public string SignatureUrl { get; set; }
            public Dictionary<string, string> CustomData { get; set; }
            public string GetCustomDataOpt(string name) => (CustomData?.ContainsKey(name) ?? false) ? CustomData[name] : null;
            public void SetCustomData(string name, string value)
            {
                if (CustomData == null)
                    CustomData = new Dictionary<string, string>();
                CustomData[name] = value;
            }
        }

        public Dictionary<int, string> GetActiveSignatureUrlBySignerNr()
        {
            if (ClosedDate.HasValue || SignedPdf != null)
                return new Dictionary<int, string>();

            return SigningCustomersBySignerNr
                .Values
                .Where(x => !x.SignedDateUtc.HasValue && x.SignatureUrl != null)
                .ToDictionary(x => x.SignerNr, x => x.SignatureUrl);
        }

        public List<int> GetSignedByApplicantNrs() =>
            SigningCustomersBySignerNr.Values.Where(x => x.SignedDateUtc.HasValue).Select(x => x.SignerNr).ToList();

        public bool HaveAllSigned() =>
            GetSignedByApplicantNrs().Count() == SigningCustomersBySignerNr.Count;

        public bool HaveAnySigned() =>
            GetSignedByApplicantNrs().Any();

        public bool IsFailed() =>
            ClosedDate.HasValue && SignedPdf == null;

        public void SetCustomData(string name, string value)
        {
            if (CustomData == null)
                CustomData = new Dictionary<string, string>();
            CustomData[name] = value;
        }
    }
}