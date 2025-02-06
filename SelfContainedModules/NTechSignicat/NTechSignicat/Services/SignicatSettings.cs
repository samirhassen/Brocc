using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NTech.Banking.CivicRegNumbers;
using NTech.Services.Infrastructure;
using NTech.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NTechSignicat.Services
{
    public class SignicatSettings
    {
        private readonly NTechSimpleSettings settings;
        private readonly INEnv env;

        public SignicatSettings(INEnv env)
        {
            var settingsFile = env.StaticResourceFile("ntech.signicat.settingsfile", "signicat-settings.txt", true);

            settings = NTechSimpleSettings.ParseSimpleSettingsFile(settingsFile.FullName, forceFileExistance: true);
            this.env = env;
        }

        public bool UseLocalMockForLogin
        {
            get
            {
                var result = settings.OptBool("useLocalMock") || settings.OptBool("useLocalMockForLogin");
                if (result && env.IsProduction)
                    throw new Exception("useLocalMock or useLocalMockForLogin is not allowed in production");
                return result;
            }
        }

        public bool UseLocalMockForSignatures
        {
            get
            {
                var result = settings.OptBool("useLocalMock") || settings.OptBool("useLocalMockForSignatures");
                if (result && env.IsProduction)
                    throw new Exception("useLocalMock or useLocalMockForSignatures is not allowed in production");
                return result;
            }
        }

        public bool AlwaysFailVerifyIsSignedByExactlyThesePersonsInMock
        {
            get
            {
                return settings.OptBool("alwaysFailVerifyIsSignedByExactlyThesePersonsInMock");
            }
        }

        public string ClientId
        {
            get
            {
                return settings.Req("clientId");
            }
        }

        public string ClientSecret
        {
            get
            {
                return settings.Req("clientSecret");
            }
        }

        public Dictionary<int, string> ReplacementCivicRegNrs
        {
            get
            {
                var result = settings.Opt("replacementCivicRegNrs");
                if (result != null && env.IsProduction)
                    throw new Exception("replacementCivicRegNrs is not allowed in production");

                return result == null ? null : JsonConvert.DeserializeObject<List<string>>(result).Select((x, i) => new { x, i }).ToDictionary(x => x.i + 1, x => x.x);
            }
        }

        public string GetLoginMethod(SignicatLoginMethodCode signicatLoginMethod)
        {
            switch (signicatLoginMethod)
            {
                case SignicatLoginMethodCode.SwedishBankId:
                    return settings.Opt("loginNameSwedishBankId") ?? "urn:signicat:oidc:method:sbid";

                case SignicatLoginMethodCode.FinnishTrustNetwork:
                    return settings.Opt("loginNameFinnishTrustNetwork") ?? "urn:signicat:oidc:portal:ftn";

                default:
                    throw new NotImplementedException();
            }
        }

        public void WithTestReplacementCivicRegNr(ICivicRegNumber civicRegNumber, int applicantNr, Action<ICivicRegNumber> replace, bool isLogin)
        {
            if (env.IsProduction)
                return;

            if (isLogin && UseLocalMockForLogin)
                return;

            if (!isLogin && UseLocalMockForSignatures)
                return;

            //Test only feature to allow using the various banks test persons e-ids while still having unique civicregnr in the actual application
            var r = ReplacementCivicRegNrs;
            if (r == null)
                return;

            if (!r.ContainsKey(applicantNr))
                return;

            replace(new CivicRegNumberParser(civicRegNumber.Country).Parse(r[applicantNr]));
        }

        public Uri SignicatUrl
        {
            get
            {
                return new Uri(settings.Req("signicatUrl"));
            }
        }

        public Uri SelfExternalUrl
        {
            get
            {
                var s = settings.Opt("selfExternalUrl");
                if (s == null)
                    return env.ServiceRegistry.External.ServiceRootUri("NTechSignicat");
                else
                    return new Uri(s);
            }
        }

        public string SignatureUsername
        {
            get
            {
                return settings.Req("signatureUsername");
            }
        }

        public string SignaturePassword
        {
            get
            {
                return settings.Req("signaturePassword");
            }
        }

        public string SignaturePackagingMethod
        {
            get
            {
                return settings.Req("signaturePackagingMethod");
            }
        }

        public string ServiceName
        {
            get
            {
                return settings.Req("serviceName");
            }
        }

        public string SignatureTestPdfPath
        {
            get
            {
                return settings.Opt("signatureTestPdfPath");
            }
        }

        public string SignatureClientCertificateThumbPrint
        {
            get
            {
                return settings.Opt("signatureClientCertificateThumbPrint");
            }
        }

        public string SignatureClientCertificateFile
        {
            get
            {
                return settings.Opt("signatureClientCertificateFile");
            }
        }

        public bool HasSignatureClientCertificate
        {
            get
            {
                return SignatureClientCertificateFile != null || SignatureClientCertificateThumbPrint != null;
            }
        }

        public string SignatureClientCertificateFilePassword
        {
            get
            {
                return settings.Opt("signatureClientCertificateFilePassword");
            }
        }

        public string SqliteDocumentDbFile
        {
            get
            {
                return settings.Req("sqliteDocumentDbFile");
            }
        }

        public string AuthenticationMessageEncryptionRsaKeyFile
        {
            get
            {
                /*
                 Should be the client public and private RSA keys. The public key also needs to be sent to signicat so they can encrypt the response.

                Howto generate a certificate pair:
                1. install python >= 3.5
                2. install pip
                >> pip install jwcrypto
                >> python
                >> from jwcrypto import jwk, jwe, jws
                >> jwk = jwk.JWK.generate(kty='RSA', size=4096)
                >> jwk.export()  #private key and private key
                >> jwk.export(private_key=False)  #public key only (give this to signicat)
                 */
                return settings.Opt("authenticationMessageEncryptionRsaKeyFile");
            }
        }

        public bool IsAuthenticationMessageEncryptionUsed()
        {
            return AuthenticationMessageEncryptionRsaKeyFile != null;
        }
    }
}