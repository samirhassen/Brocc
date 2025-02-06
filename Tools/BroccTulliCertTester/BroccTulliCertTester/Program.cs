using Jose;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BroccTulliCertTester
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if(args.Length != 2)
            {
                Console.WriteLine("Usage: BroccTulliCertTester.exe [RS256|RS512] <thumbprint>");
                return;
            }
            JwsAlgorithm algo;
            if (args[0] == "RS256")
                algo = JwsAlgorithm.RS256;
            else if (args[0] == "RS512")
                algo = JwsAlgorithm.RS512;
            else
            {
                Console.WriteLine("Invalid algo");
                return;
            }

            var thumbprint = args[1];
            try
            {
                Console.WriteLine($"Loading cert : {thumbprint}");
                var certificate = LoadClientCertificateUsingThumbPrint(thumbprint);
                Console.WriteLine("Loading private key");
                using (var key = certificate.GetRSAPrivateKey())
                {
                    var payload = new Dictionary<string, object>
                    {
                        ["a"] = "test1",
                        ["b"] = 1,
                        ["c"] = false
                    };
                    Console.WriteLine("--Begin: Cleartext payload--");
                    Console.WriteLine(new JsonNetMapper().Serialize(payload));
                    Console.WriteLine("--End: Cleartext payload--");
                    var signedPayload = CreateJWS(payload, key, algo);
                    Console.WriteLine("--Begin: Signed payload--");
                    Console.WriteLine(signedPayload);
                    Console.WriteLine("--End: Signed payload--");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static X509Certificate2 LoadClientCertificateUsingThumbPrint(string certificateThumbPrint)
        {
            using (var keyStore = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                keyStore.Open(OpenFlags.ReadOnly);
                return keyStore
                    .Certificates
                    .OfType<X509Certificate2>()
                    .First(x => x.Thumbprint.Equals(certificateThumbPrint, StringComparison.OrdinalIgnoreCase));
            }
        }


        private static string CreateJWS(IDictionary<string, object> payload, System.Security.Cryptography.RSA privateKey, JwsAlgorithm algo)
        {
            Console.WriteLine($"Signing payload with: {algo}");
            return Jose.JWT.Encode(payload, privateKey, algo, extraHeaders: new Dictionary<string, object>
                {
                    { "typ", "JWT" }
                }, settings: new JwtSettings
                {
                    JsonMapper = new JsonNetMapper() //The built in default uses the microsoft seralizer that serializes dates as Date(/.../)
                });
        }

        private class JsonNetMapper : IJsonMapper
        {
            public T Parse<T>(string json)
            {
                return JsonConvert.DeserializeObject<T>(json);
            }

            public string Serialize(object obj)
            {
                return JsonConvert.SerializeObject(obj);
            }
        }
    }
}
