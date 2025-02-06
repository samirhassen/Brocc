using Newtonsoft.Json.Linq;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace nCustomer.Services.EidSignatures.Scrive
{
    public abstract class ScriveSignatureClientBase
    {
        private readonly Func<HttpClientSync> httpClientFactory;
        private readonly Credentials credentials;
        protected readonly Uri apiEndpoint;

        private class Credentials
        {
            //Also: apiToken, oauth_consumer_key
            public string ClientCredentialsIdentifier { get; set; }

            //Also: apisecret, half of oauth_signature = <apisecret>&<accesssecret>
            public string ClientCredentialsSecret { get; set; }

            //Also: accesstoken, oauth_token
            public string TokenCredentialsIdentifier { get; set; }

            //Also: accesssecret, half of oauth_signature = <apisecret>&<accesssecret>
            public string TokenCredentialsSecret { get; set; }
        }

        public ScriveSignatureClientBase(NTechSimpleSettings settings)
        {
            this.httpClientFactory = () => new HttpClientSync(); //scriveSignatureBaseUrl
            this.credentials = new Credentials
            {
                ClientCredentialsIdentifier = settings.Req("clientCredentialsIdentifier"),
                ClientCredentialsSecret = settings.Req("clientCredentialsSecret"),
                TokenCredentialsIdentifier = settings.Req("tokenCredentialsIdentifier"),
                TokenCredentialsSecret = settings.Req("tokenCredentialsSecret")
            };
            this.apiEndpoint = new Uri(settings.Req("scriveSignatureBaseUrl"));
        }

        protected MultipartFormDataContent CreateMultipartFormDataContent(
            Dictionary<string, string> nakedStrings = null,
            (byte[] Data, string FileName, string Name, string ContentType)? file = null,
            (JObject Data, string Name)? jsonItem = null)
        {
            var formContent = new MultipartFormDataContent("WebKitFormBoundary9LVkoYVlTmGaF6Tw"); //Boundry seems to be just any random string that separates each content fragment. Using an example from their api explorer but it probably doesnt matter at all.

            if (file.HasValue)
            {
                var fileContent = new ByteArrayContent(file.Value.Data);
                fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(file.Value.ContentType);
                formContent.Add(fileContent, file.Value.Name, file.Value.FileName); //The file name is used for the name so set it accordingly
            }
            if (jsonItem.HasValue)
            {
                var jsonContent = new StringContent(jsonItem.Value.Data.ToString(), Encoding.UTF8, "application/json");
                formContent.Add(jsonContent, jsonItem.Value.Name);
            }
            if (nakedStrings != null)
            {
                foreach (var name in nakedStrings.Keys)
                {
                    formContent.Add(new StringContent(nakedStrings[name]), name);
                }
            }
            return formContent;
        }

        protected JObject PostOrGetSignature(string relativeUrl, HttpContent requestData, HttpMethod method, Action<byte[]> downloadFile = null, bool skipParsingResponse = false)
        {
            var client = httpClientFactory();
            var request = new HttpRequestMessage(method, NTechServiceRegistry.CreateUrl(apiEndpoint, relativeUrl));

            var authorizationHeader = $"oauth_signature_method=\"PLAINTEXT\",oauth_consumer_key=\"{credentials.ClientCredentialsIdentifier}\",oauth_token=\"{credentials.TokenCredentialsIdentifier}\",oauth_signature=\"{credentials.ClientCredentialsSecret}&{credentials.TokenCredentialsSecret}\"";

            //Scrive has a wierd non standards compliant auth header that does not have a scheme at all so we need to disable validaton or this will throw
            request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);

            try
            {
                if (method == HttpMethod.Post && requestData != null)
                {
                    request.Content = requestData;
                }

                var response = client.Send(request);
                if (!skipParsingResponse)
                {
                    var responseContent = response.Content;

                    if (!response.IsSuccessStatusCode)
                    {
                        if ((response.Content.Headers.ContentLength ?? 0) > 0 && response.Content.Headers.ContentType.MediaType.Contains("application/json"))
                        {
                            var jsonBody = response.Content.ReadAsString();
                            if (jsonBody?.Length > 5000)
                                jsonBody = jsonBody.Substring(0, 5000);
                            throw new ScriveSignatureClientException("Error status from scrive with details: " + jsonBody, httpStatusCode: (int)response.StatusCode);
                        }
                        else
                            throw new ScriveSignatureClientException("Error status from scrive without details", httpStatusCode: (int)response.StatusCode);
                    }

                    if (downloadFile != null)
                    {
                        var ms = new MemoryStream();
                        response.Content.CopyTo(ms);
                        downloadFile(ms.ToArray());
                        return null;
                    }
                    else if (response.Content.IsJson())
                    {
                        var rawResponse = response.Content.ReadAsString();
                        return JObject.Parse(rawResponse);
                    }
                    else
                    {
                        throw new ScriveSignatureClientException("Invalid response content type. Expected application/json");
                    }
                }
                else
                {
                    if (response.IsSuccessStatusCode)
                    {
                        return null;
                    }
                    else
                    {
                        throw new ScriveSignatureClientException("Error status from scrive", httpStatusCode: (int)response.StatusCode);
                    }
                }
            }
            catch (ScriveSignatureClientException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ScriveSignatureClientException(ex.Message, innerException: ex);
            }
        }

        public class ScriveSignatureClientException : Exception
        {
            public ScriveSignatureClientException(string message, Exception innerException = null, int? httpStatusCode = null) : base(message, innerException)
            {
                HttpStatusCode = httpStatusCode;
            }

            public int? HttpStatusCode { get; }
        }
    }
}