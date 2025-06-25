using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Jose;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NTech.Core.Module;
using static NTech.Core.Savings.Shared.Services.FinnishCustomsAccounts.FinnishCustomsAccountsWebservice;

namespace NTech.Core.Savings.Shared.Services.FinnishCustomsAccounts
{
    public interface IFinnishCustomsAccountsWebservice
    {
        bool TryReportUpdate(JObject clearTextUpdate, LoggingContextModel loggingContext);
    }

    public class FinnishCustomsAccountsWebservice : IFinnishCustomsAccountsWebservice
    {
        private readonly Lazy<NTechSimpleSettingsCore> finnishCustomsSettings;
        private readonly bool isTest;
        private readonly IFinnishCustomsMigrationManager migrationManager;

        public FinnishCustomsAccountsWebservice(Lazy<NTechSimpleSettingsCore> finnishCustomsSettings, bool isTest, IFinnishCustomsMigrationManager migrationManager)
        {
            this.finnishCustomsSettings = finnishCustomsSettings;
            this.isTest = isTest;
            this.migrationManager = migrationManager;
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

        private static void SetBearerToken(HttpClient client, string token) =>
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        private T WithHttpClient<T>(X509Certificate2 clientCertificate, Func<HttpClient, T> withClient)
        {
            return migrationManager.WithHttpClientUsingClientCertificate(clientCertificate, client =>
            {
                client.Timeout = TimeSpan.FromMinutes(5);
                if (isTest && finnishCustomsSettings.Value.OptBool("delivery.localtest.forceError"))
                {
                    client.DefaultRequestHeaders.Add("X-Ntech-ErrorTest", "1");
                }
                return withClient(client);
            });
        }

        public bool TryReportUpdate(JObject clearTextUpdate, LoggingContextModel loggingContext)
        {
            loggingContext.ClearTextRequest = clearTextUpdate.ToString(Formatting.Indented);

            var clientCertificateThumbprint = finnishCustomsSettings.Value.Req("delivery.clientCertificateThumbprint");
            var clientCertificate = LoadClientCertificateUsingThumbPrint(clientCertificateThumbprint);

            WithRsaKey(clientCertificate, rsaKey =>
            {
                var jwsAlgorithm = GetJwsAlgorithmSetting();
                loggingContext.AuthorizationHeader = CreateJWS(new Dictionary<string, object>
                {
                    { "sub", finnishCustomsSettings.Value.Req("senderBusinessId") },
                    { "aud", "accountRegister" }
                }, rsaKey, jwsAlgorithm);

                var requestObject = new JObject();
                requestObject.Add("sub", finnishCustomsSettings.Value.Req("senderBusinessId"));
                requestObject.Add("aud", "accountRegister");
                requestObject.Add("reportUpdate", clearTextUpdate);
                var requestJwsPayload = JsonConvert.DeserializeObject<IDictionary<string, object>>(requestObject.ToString(Formatting.None), new DictionaryConverter());
                loggingContext.EncodedRequest = CreateJWS(requestJwsPayload, rsaKey, jwsAlgorithm);
            });

            var rawContentBytes = Encoding.UTF8.GetBytes(loggingContext.EncodedRequest);

            var result = WithHttpClient(clientCertificate, c =>
            {
                var url = finnishCustomsSettings.Value.Req("delivery.url");
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                SetBearerToken(c, loggingContext.AuthorizationHeader);
                var fileContent = new ByteArrayContent(rawContentBytes);
                //NOTE: Override this when testing locally since the response cannot be parsed otherwise
                fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(finnishCustomsSettings.Value.OptBool("delivery.useJwtConentType") ? "application/jwt" : "application/json;charset=utf-8");
                request.Content = fileContent;
                return c.SendAsync(request).Result;
            });

            if (result.Headers.TryGetValues("X-Correlation-ID", out var correlationIds))
            {
                loggingContext.CustomsCorrelationId = correlationIds?.FirstOrDefault();
            }

            if (loggingContext.CustomsCorrelationId == null)
            {
                if (result.Content.Headers.TryGetValues("X-Correlation-ID", out var contentCorrelationIds))
                {
                    loggingContext.CustomsCorrelationId = contentCorrelationIds?.FirstOrDefault();
                }
            }

            if (result.Content.Headers.ContentLength > 0 && result.Content.Headers.ContentType.MediaType.Contains("application/json"))
            {
                loggingContext.JsonResponse = result.Content.ReadAsStringAsync().Result;
            }

            if (result.IsSuccessStatusCode)
            {
                return true;
            }

            var errorMessage = (loggingContext.JsonResponse == null ? null : JsonConvert.DeserializeAnonymousType(loggingContext.JsonResponse, new { message = "" })?.message) ?? "no error message provided";
            loggingContext.CustomsErrorMessage = errorMessage;
            loggingContext.HttpErrorCode = (int)result.StatusCode;
            return false;
        }

        private void WithRsaKey(X509Certificate2 certificateWithPrivateKey, Action<System.Security.Cryptography.RSA> withKey)
        {
            using (var key = certificateWithPrivateKey.GetRSAPrivateKey())
            {
                withKey(key);
            }
        }

        private static string CreateJWS(IDictionary<string, object> payload, System.Security.Cryptography.RSA privateKey, JwsAlgorithm jwsAlgorith)
        {
            return Jose.JWT.Encode(payload, privateKey, jwsAlgorith, extraHeaders: new Dictionary<string, object> //configurable
                {
                    { "typ", "JWT" }
                }, settings: new JwtSettings
                {
                    JsonMapper = new JsonNetMapper() //The built in default uses the microsoft seralizer that serializes dates as Date(/.../)
                });
        }

        private JwsAlgorithm GetJwsAlgorithmSetting()
        {
            string jwsAlgorithmString = finnishCustomsSettings.Value.Opt("delivery.jwsAlgorithm")?.Trim();

            if (string.IsNullOrEmpty(jwsAlgorithmString))
            {
                return JwsAlgorithm.RS256; //Default
            }
            else if (Enum.TryParse<JwsAlgorithm>(jwsAlgorithmString, out JwsAlgorithm jwsAlgorithm))
            {
                return jwsAlgorithm;
            }
            else
            {
                throw new Exception($"Finnish customs error: delivery.jwsAlgorithm setting '{jwsAlgorithmString}' is not valid. Valid values include 'RS256' and 'RS512'."); 
            }
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

        public class LoggingContextModel
        {
            public string CustomsCorrelationId { get; set; }
            public string ClearTextRequest { get; set; }
            public string SignedRequest { get; set; }
            public string AuthorizationHeader { get; set; }
            public string EncodedRequest { get; set; }
            public string JsonResponse { get; set; }
            public int? HttpErrorCode { get; set; }
            public string CustomsErrorMessage { get; set; }
            public string ExceptionMessage { get; set; }
        }
    }

    //From: https://stackoverflow.com/questions/5546142/how-do-i-use-json-net-to-deserialize-into-nested-recursive-dictionary-and-list
    public class DictionaryConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            this.WriteValue(writer, value);
        }

        private void WriteValue(JsonWriter writer, object value)
        {
            var t = JToken.FromObject(value);
            switch (t.Type)
            {
                case JTokenType.Object:
                    this.WriteObject(writer, value);
                    break;

                case JTokenType.Array:
                    this.WriteArray(writer, value);
                    break;

                case JTokenType.Date:
                    writer.WriteValue(value);
                    break;

                default:
                    writer.WriteValue(value);
                    break;
            }
        }

        private void WriteObject(JsonWriter writer, object value)
        {
            writer.WriteStartObject();
            var obj = value as IDictionary<string, object>;
            foreach (var kvp in obj)
            {
                writer.WritePropertyName(kvp.Key);
                this.WriteValue(writer, kvp.Value);
            }
            writer.WriteEndObject();
        }

        private void WriteArray(JsonWriter writer, object value)
        {
            writer.WriteStartArray();
            var array = value as IEnumerable<object>;
            foreach (var o in array)
            {
                this.WriteValue(writer, o);
            }
            writer.WriteEndArray();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return ReadValue(reader);
        }

        private object ReadValue(JsonReader reader)
        {
            while (reader.TokenType == JsonToken.Comment)
            {
                if (!reader.Read()) throw new JsonSerializationException("Unexpected Token when converting IDictionary<string, object>");
            }

            switch (reader.TokenType)
            {
                case JsonToken.StartObject:
                    return ReadObject(reader);

                case JsonToken.StartArray:
                    return this.ReadArray(reader);

                case JsonToken.Integer:
                case JsonToken.Float:
                case JsonToken.String:
                case JsonToken.Boolean:
                case JsonToken.Undefined:
                case JsonToken.Null:
                case JsonToken.Date:
                case JsonToken.Bytes:
                    return reader.Value;

                default:
                    throw new JsonSerializationException
                        (string.Format("Unexpected token when converting IDictionary<string, object>: {0}", reader.TokenType));
            }
        }

        private object ReadArray(JsonReader reader)
        {
            IList<object> list = new List<object>();

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.Comment:
                        break;

                    default:
                        var v = ReadValue(reader);

                        list.Add(v);
                        break;

                    case JsonToken.EndArray:
                        return list;
                }
            }

            throw new JsonSerializationException("Unexpected end when reading IDictionary<string, object>");
        }

        private object ReadObject(JsonReader reader)
        {
            var obj = new Dictionary<string, object>();

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:
                        var propertyName = reader.Value.ToString();

                        if (!reader.Read())
                        {
                            throw new JsonSerializationException("Unexpected end when reading IDictionary<string, object>");
                        }

                        var v = ReadValue(reader);

                        obj[propertyName] = v;
                        break;

                    case JsonToken.Comment:
                        break;

                    case JsonToken.EndObject:
                        return obj;
                }
            }

            throw new JsonSerializationException("Unexpected end when reading IDictionary<string, object>");
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(IDictionary<string, object>).IsAssignableFrom(objectType);
        }
    }
}