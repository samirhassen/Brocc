using Duende.IdentityModel.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NTech.ElectronicSignatures;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static nCustomer.Services.EidSignatures.Assently.AssentlyDocument;

namespace nCustomer.Services.EidSignatures.Assently
{
    public abstract class AssentlySignatureClientBase
    {
        private readonly Func<HttpClient> httpClientFactory;
        private readonly Credentials credentials;
        protected readonly Uri apiEndpoint;

        private class Credentials
        {
            public string ClientCredentialsIdentifier { get; set; }

            public string ClientCredentialsSecret { get; set; }
        }

        public AssentlySignatureClientBase(NTechSimpleSettings settings)
        {
            this.httpClientFactory = () => new HttpClient();
            this.credentials = new Credentials
            {
                ClientCredentialsIdentifier = settings.Req("clientCredentialsIdentifier"),
                ClientCredentialsSecret = settings.Req("clientCredentialsSecret"),
            };
            this.apiEndpoint = new Uri(settings.Req("assentlySignatureBaseUrl"));
        }

        protected MultipartFormDataContent CreateMultipartFormDataContent(
            Dictionary<string, string> nakedStrings = null,
            (byte[] Data, string FileName, string Name, string ContentType)? file = null,
            (JObject Data, string Name)? jsonItem = null)
        {
            var formContent = new MultipartFormDataContent();

            if (file.HasValue)
            {
                var fileContent = new ByteArrayContent(file.Value.Data);
                fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(file.Value.ContentType);
                formContent.Add(fileContent, file.Value.Name, file.Value.FileName);
            }
            if (jsonItem.HasValue)
            {
                var jsonContent = new StringContent(jsonItem.Value.Data.ToString(), Encoding.UTF8, "multipart/form-data");
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

        protected async Task<Dictionary<int, PartySignatureStatus>> GetPartySignatureStatusAsync(string caseId, string token = null)
        {
            if (token == null)
            {
                token = await GetCredentialTokenAsync();
            }

            var client = httpClientFactory();
            var partiesEndpoint = $"/apiv3/api/cases/{caseId}/parties";

            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(this.apiEndpoint, partiesEndpoint));

            request.Headers.Add("Authorization", "Bearer " + token);
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);
            var jsonBody = await response.Content.ReadAsStringAsync();

            var responseObjectType = new { parties = new[] { new { signatureUrl = "", signedOn = "", signingOrder = 0 } } };
            var responseObject = JsonConvert.DeserializeAnonymousType(jsonBody, responseObjectType);

            var partyStatusDictionary = new Dictionary<int, PartySignatureStatus>();

            if (responseObject != null && responseObject.parties != null)
            {
                foreach (var party in responseObject.parties)
                {
                    if (party.signatureUrl == null)
                    {
                        throw new AssentlySignatureClientException("Signature URL could not be fetched from Assently.");
                    }

                    var signatureUrl = party.signatureUrl;
                    var signedOn = party.signedOn;
                    var signingOrder = party.signingOrder;

                    var partySignatureStatus = new PartySignatureStatus
                    {
                        SignatureUrl = new Uri(signatureUrl),
                        HasSigned = signedOn != null
                    };

                    partyStatusDictionary.Add(signingOrder, partySignatureStatus);
                }
            }

            return partyStatusDictionary;
        }

        protected async Task<bool> SetCallbackUriAsync(string token, string caseId, Uri callbackUrl)
        {
            var client = httpClientFactory();
            var webhookEndpoint = $"/apiv3/api/cases/{caseId}/webhooks";

            var requestData = new
            {
                events = new[] { "signed" },
                url = callbackUrl.ToString()
            };

            var request = new HttpRequestMessage(HttpMethod.Put, new Uri(this.apiEndpoint, webhookEndpoint))
            {
                Content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "text/json")
            };

            request.Headers.Add("Authorization", "Bearer " + token);

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new AssentlySignatureClientException("Error status from Assently", httpStatusCode: (int)response.StatusCode);
            }

            return response.IsSuccessStatusCode;
        }

        protected async Task<bool> RecallCaseAsync(string caseId)
        {
            var client = httpClientFactory();

            var token = await GetCredentialTokenAsync();

            var request = new HttpRequestMessage(HttpMethod.Put, new Uri(this.apiEndpoint, $"/apiv3/api/cases/{caseId}/recall"));

            request.Headers.Add("Authorization", "Bearer " + token);

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new AssentlySignatureClientException("Case could not be recalled.");
            }

            return response.IsSuccessStatusCode;
        }

        protected async Task<bool> SendCaseAsync(string token, string caseId)
        {
            var client = httpClientFactory();

            var isNotificationsConfigured = await ConfigureNoNotifications(client, token, caseId);
            if (!isNotificationsConfigured)
            {
                throw new AssentlySignatureClientException("Could not configure no notifications");
            }

            var request = new HttpRequestMessage(HttpMethod.Put, new Uri(this.apiEndpoint, $"/apiv3/api/cases/{caseId}/send"));

            request.Headers.Add("Authorization", "Bearer " + token);

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new AssentlySignatureClientException("Could not send document for signing");
            }

            return response.IsSuccessStatusCode;
        }

        protected async Task<string> GetCredentialTokenAsync()
        {            
            var client = new HttpClient();
            var token = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = this.apiEndpoint + "/apiv3/token",
                ClientId = "nTechSystemUser",
                ClientSecret = "nTechSystemUser",
                Scope = "nTech1"
            });

            if (token.IsError || token.AccessToken == null)
            {
                throw new AssentlySignatureClientException("Could not fetch token from Assently.");
            }

            return token.AccessToken;
        }

        protected async Task<string> CreateCaseIdAsync(string token, string caseName)
        {
            var client = httpClientFactory();

            var contentData = new
            {
                name = caseName,
                allowedSignatureTypes = new[] { "electronicId" }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, new Uri(this.apiEndpoint, "/apiv3/api/cases"))
            {
                Content = new StringContent(JsonConvert.SerializeObject(contentData), Encoding.UTF8, "text/json")

            };
            request.Headers.Add("Authorization", "Bearer " + token);
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);
            var jsonBody = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<dynamic>(jsonBody);

            return responseObject.caseId;
        }
        
        protected async Task<(string FileName, byte[] FileData)> GetSignedDocumentDataAsync(string caseId)
        {
            var client = httpClientFactory();
            var token = await GetCredentialTokenAsync();
            var casesEndpoint = $"/apiv3/api/cases/{caseId}";

            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(this.apiEndpoint, casesEndpoint));
            request.Headers.Add("Authorization", "Bearer " + token);
            var response = await client.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            var cases = JsonConvert.DeserializeAnonymousType(json, new { documents = new[] { new { id = string.Empty, type = string.Empty, filename = string.Empty } } });

            if (cases.documents != null)
            {
                var documentId = "";
                var fileName = ""; 
                foreach (var document in cases.documents)
                {
                    if (document.type == "receipt")
                    {
                        documentId = document.id;
                        fileName = document.filename; 
                        break; 
                    }
                }

                if (documentId == null)
                {
                    //Default 
                    documentId = cases.documents[0].id;
                    fileName = cases.documents[0].filename ?? "assently_document"; 
                }

                var documentEndpoint = $"/apiv3/api/cases/{caseId}/documents/{documentId}";
                var docRequest = new HttpRequestMessage(HttpMethod.Get, new Uri(this.apiEndpoint, documentEndpoint));
                docRequest.Headers.Add("Authorization", "Bearer " + token);
                var docResponse = await client.SendAsync(docRequest);

                if (!docResponse.IsSuccessStatusCode)
                {
                    throw new AssentlySignatureClientException("Error status from Assently", httpStatusCode: (int)response.StatusCode);
                }

                var fileData = await docResponse.Content.ReadAsByteArrayAsync();
                return (fileName, fileData);
            }

            return (string.Empty, null);
        }

        protected async Task<JObject> PostNewDocumentAsync(string token, Dictionary<int, CommonElectronicIdSignatureSession.SigningCustomer> signingCustomersBySignerNr, HttpContent requestData, string caseId)
        {
            var client = httpClientFactory();

            var partiesList = new List<object>();

            foreach (var signingCustomerBySignerNr in signingCustomersBySignerNr)
            {
                var signingCustomer = signingCustomersBySignerNr[signingCustomerBySignerNr.Key];
                partiesList.Add(new
                {
                    name = $"{signingCustomer.FirstName} {signingCustomer.LastName}",
                    nationalIdNumber = signingCustomer.CivicRegNr,
                    signingOrder = signingCustomerBySignerNr.Key
                });
            }

            var parties = partiesList.ToArray();
            var partyRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(this.apiEndpoint, $"/apiv3/api/cases/{caseId}/parties"))
            {
                Content = new StringContent(JsonConvert.SerializeObject(new { parties }), Encoding.UTF8, "text/json")

            };

            partyRequest.Headers.Add("Authorization", "Bearer " + token);
            var partyResponse = await client.SendAsync(partyRequest);

            var docRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(this.apiEndpoint, $"/apiv3/api/cases/{caseId}/documents"))
            {
                Content = requestData
            };
            docRequest.Headers.Add("Authorization", "Bearer " + token);
            docRequest.Headers.Add("Accept", "application/json");
            var docResponse = await client.SendAsync(docRequest);
            var docJson = await docResponse.Content.ReadAsStringAsync();

            return JObject.Parse(docJson);
        }

        protected async Task<bool> ConfigureNoNotifications(HttpClient client, string token, string caseId)
        {
            //Assently requires configuration of notifications to avoid defaulting to email notifications
            //The whole object needs to be sent for the "no notifications" configuration to work 
            var notificationObject = new
            {
                cancelUrl = (string)null,
                continueUrl = "",
                continueName = "",
                continueAuto = false,
                sendSignRequestEmailToParties = false,
                sendFinishEmailToCreator = false,
                sendFinishEmailToParties = false,
                sendRecallEmailToParties = false,
                requestMessage = (string)null,
                requestMessageSms = (string)null,
                finishedMessage = (string)null,
                finishedMessageSms = (string)null,
                sendRejectNotification = false,
                expireAfterDays = 0,
                expireOn = (DateTime?)null,
                remindAfterDays = 0,
                notificationMethods = new[] { "none" }
            };

            var noNotificationRequest = new HttpRequestMessage(HttpMethod.Put, new Uri(this.apiEndpoint, $"/apiv3/api/cases/{caseId}/notifications"))
            {
                Content = new StringContent(JsonConvert.SerializeObject(notificationObject), Encoding.UTF8, "text/json")

            };
            noNotificationRequest.Headers.Add("Authorization", "Bearer " + token);
            var notificationsResponse = await client.SendAsync(noNotificationRequest);

            return notificationsResponse.IsSuccessStatusCode;
        }

        public class AssentlySignatureClientException : Exception
        {
            public AssentlySignatureClientException(string message, Exception innerException = null, int? httpStatusCode = null) : base(message, innerException)
            {
                HttpStatusCode = httpStatusCode;
            }

            public int? HttpStatusCode { get; }
        }
    }
}