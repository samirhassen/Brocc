using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace NTechSignicat.Services
{
    public class SignicatMessageEncryptionService
    {
        public SignicatMessageEncryptionService(SignicatSettings settings, IHttpClientFactory httpClientFactory)
        {
            this.settings = settings;
            this.httpClientFactory = httpClientFactory;
        }

        private readonly SignicatSettings settings;
        private readonly IHttpClientFactory httpClientFactory;
        private List<Dictionary<string, string>> cachedSignicatPublicKeys;
        private Dictionary<string, string> cachedClientPrivateKey;

        private T WithRsaKey<T>(IDictionary<string, string> d, Func<RSA, T> f)
        {
            Func<string, byte[]> decodeIfExists = name => d.ContainsKey(name) ? Jose.Base64Url.Decode(d[name]) : null;
            using (var key = RSA.Create())
            {
                var rsaParam = new RSAParameters()
                {
                    Modulus = decodeIfExists("n"),
                    Exponent = decodeIfExists("e"),
                    D = decodeIfExists("d"),
                    DP = decodeIfExists("dp"),
                    DQ = decodeIfExists("dq"),
                    P = decodeIfExists("p"),
                    Q = decodeIfExists("q"),
                    InverseQ = decodeIfExists("qi"),
                };
                key.ImportParameters(rsaParam);
                return f(key);
            }
        }

        private string DecodeIncomingMessage(string encryptedMessage, IDictionary<string, string> signicatPublicKey, IDictionary<string, string> clientPrivateKey)
        {
            var d1 = WithRsaKey(clientPrivateKey, x => Jose.JWT.Decode(encryptedMessage, x, Jose.JweAlgorithm.RSA_OAEP, Jose.JweEncryption.A128CBC_HS256));
            return WithRsaKey(signicatPublicKey, x => Jose.JWT.Decode(d1, x, Jose.JweAlgorithm.RSA_OAEP, Jose.JweEncryption.A256CBC_HS512));
        }

        private async Task<Dictionary<string, string>> GetSignicatPublicKey(string keyUse)
        {
            var keys = await GetSignicatPublicKeys(keyUse);

            if (keys.Count == 0)
                throw new Exception($"Missing signicat key with use {keyUse} from oidc/jwks.json");

            return keys.First();
        }

        private async Task<List<Dictionary<string, string>>> GetSignicatPublicKeys(string keyUse)
        {
            if (cachedSignicatPublicKeys == null)
            {
                var client = httpClientFactory.CreateClient();
                client.BaseAddress = settings.SignicatUrl;
                var result = await client.GetStringAsync("oidc/jwks.json");
                cachedSignicatPublicKeys = JsonConvert.DeserializeAnonymousType(result, new
                {
                    keys = (List<Dictionary<string, string>>)null
                })?.keys;
            }

            var keys = cachedSignicatPublicKeys?.Where(x => x.GetValueOrDefault("use") == keyUse)?.ToList();

            if (keys == null || keys.Count == 0)
                throw new Exception($"Missing signicat key with use {keyUse} from oidc/jwks.json");

            return keys;
        }

        private Dictionary<string, string> GetClientPrivateKey()
        {
            if(cachedClientPrivateKey == null)
                cachedClientPrivateKey = JsonConvert.DeserializeObject<Dictionary<string, string>>(System.IO.File.ReadAllText(settings.AuthenticationMessageEncryptionRsaKeyFile));
            return cachedClientPrivateKey;
        }

        public async Task<string> EncryptOutgoingMessage(Dictionary<string, object> payload)
        {
            Dictionary<string, string> jwk = await GetSignicatPublicKey("enc");
            using (var key = RSA.Create())
            {
                var rsaParam = new RSAParameters()
                {
                    Modulus = Jose.Base64Url.Decode(jwk["n"]),
                    Exponent = Jose.Base64Url.Decode(jwk["e"])
                };
                key.ImportParameters(rsaParam);
                var headers = new Dictionary<string, object>()
                {
                    { "alg", "" },
                    { "enc",  jwk["alg"]},
                    { "typ", "JWE" },
                    { "kid", jwk["kid"]}
                };

                return Jose.JWT.Encode(payload, key, Jose.JweAlgorithm.RSA_OAEP, Jose.JweEncryption.A256CBC_HS512, extraHeaders: headers);
            }
        }

        public async Task<string> DecryptIncomingMessage(string encryptedMessage)
        {
            var signicatSigningKeys = await GetSignicatPublicKeys("sig");
            for(var i=0; i< signicatSigningKeys.Count; i++)
            {
                try
                {
                    var signicatSigningKey = signicatSigningKeys[i];
                    var clientPrivateKey = GetClientPrivateKey();
                    return DecodeIncomingMessage(encryptedMessage, signicatSigningKey, clientPrivateKey);
                }
                catch(Jose.IntegrityException)
                {
                    if(i == (signicatSigningKeys.Count - 1))
                        throw;
                }
            }
            throw new Exception("This cannot happen but the compiler fails to understand this");
        }
    }
}
