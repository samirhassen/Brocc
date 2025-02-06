using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace nCustomer.Code.Services.EidAuthentication.EncryptionHelpers
{
    public class JweEncryptionService
    {
        private readonly NTechSimpleSettings settings;
        private Dictionary<string, string> CachedEncryptionKeys;
        public JweEncryptionService(NTechSimpleSettings settings)
        {
            this.settings = settings;
        }

        public EncryptionKeys.PublicKeys GetEncryptionPublicKey()
        {
            if (CachedEncryptionKeys == null)
            {
                var encryptionKeysPath = settings.Opt("authenticationMessageEncryptionRsaKeyFile");
                CachedEncryptionKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(System.IO.File.ReadAllText(encryptionKeysPath));
            }

            ThrowIfMissingEncryptionKeys();

            return new EncryptionKeys.PublicKeys
            {
                alg = "RSA-OAEP",
                e = CachedEncryptionKeys["e"],
                kty = CachedEncryptionKeys["kty"],
                n = CachedEncryptionKeys["n"],
                use = "enc"
            };
        }

        public EncryptionKeys.PrivateKeys GetEncryptionPrivateKey()
        {
            if (CachedEncryptionKeys == null)
            {
                var encryptionKeysPath = settings.Opt("authenticationMessageEncryptionRsaKeyFile");
                CachedEncryptionKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(System.IO.File.ReadAllText(encryptionKeysPath));
            }

            ThrowIfMissingEncryptionKeys();

            return new EncryptionKeys.PrivateKeys
            {
                alg = "RSA-OAEP",
                d = CachedEncryptionKeys["d"],
                dp = CachedEncryptionKeys["dp"],
                dq = CachedEncryptionKeys["dq"],
                e = CachedEncryptionKeys["e"],
                kty = CachedEncryptionKeys["kty"],
                n = CachedEncryptionKeys["n"],
                p = CachedEncryptionKeys["p"],
                q = CachedEncryptionKeys["q"],
                qi = CachedEncryptionKeys["qi"],
                use = "enc"
            };
        }

        public string GetDecryptedPayload(string payload)
        {
            using (RSA rsa = RSA.Create())
            {
                var jwePrivateKey = GetEncryptionPrivateKey();
                var rsaParameters = GetRsaParameters(jwePrivateKey);
                rsa.ImportParameters(rsaParameters);

                return Jose.JWT.Decode(payload, rsa);
            }
        }

        private void ThrowIfMissingEncryptionKeys()
        {
            var requiredKeys = new string[] { "d", "dp", "dq", "e", "kty", "n", "p", "q", "qi" };
            if (!requiredKeys.All(key => CachedEncryptionKeys.ContainsKey(key)))
            {
                NLog.Error("Error when fetching encryption keys: encryption settings does not contain all keys.");
                throw new Exception("Encryption settings have missing keys.");
            }
        }

        public RSAParameters GetRsaParameters(EncryptionKeys.PrivateKeys jwePrivateKey)
        {
            byte[] decodeIfExists(string val) => val != null ? Jose.Base64Url.Decode(val) : null;
            return new RSAParameters
            {
                Modulus = decodeIfExists(jwePrivateKey.n),
                Exponent = decodeIfExists(jwePrivateKey.e),
                D = decodeIfExists(jwePrivateKey.d),
                DP = decodeIfExists(jwePrivateKey.dp),
                DQ = decodeIfExists(jwePrivateKey.dq),
                P = decodeIfExists(jwePrivateKey.p),
                Q = decodeIfExists(jwePrivateKey.q),
                InverseQ = decodeIfExists(jwePrivateKey.qi)
            };
        }

        public class EncryptionKeys
        {
            public class PublicKeys
            {
                public string alg { get; set; }
                public string e { get; set; }
                public string kty { get; set; }
                public string n { get; set; }
                public string use { get; set; }
            }

            public class PrivateKeys
            {
                public string d { get; set; }
                public string dp { get; set; }
                public string dq { get; set; }
                public string p { get; set; }
                public string q { get; set; }
                public string qi { get; set; }
                public string alg { get; set; }
                public string e { get; set; }
                public string kty { get; set; }
                public string n { get; set; }
                public string use { get; set; }
            }
        }
    }
}