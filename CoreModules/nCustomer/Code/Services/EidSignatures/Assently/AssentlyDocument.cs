using Newtonsoft.Json.Linq;
using NTech.Banking.CivicRegNumbers;
using NTech.ElectronicSignatures;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;

namespace nCustomer.Services.EidSignatures.Assently
{
    public class AssentlyDocument
    {
        private readonly JObject rawDocument;

        public AssentlyDocument(JObject rawDocument)
        {
            this.rawDocument = rawDocument;
        }

        public JObject RawDocument => this.rawDocument;

        public class PartySignatureStatus
        {
            public Uri SignatureUrl { get; set; }
            public bool HasSigned { get; set; }
        }
    }
}