using Newtonsoft.Json;
using NTech.Banking.Conversion;
using NTech.Core.Module.Shared.Clients;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static NTech.ElectronicSignatures.CommonElectronicIdSignatureSession;

namespace nCustomer.Services.EidSignatures.Signicat2
{
    public class Signicat2SignatureClient
    {
        private readonly Credentials credentials;
        protected readonly string apiEndpointBaseUrl;
        private class Credentials
        {
            public string ClientId { get; set; }

            public string ClientSecret { get; set; }
        }

        public Signicat2SignatureClient(NTechSimpleSettings settings)
        {
            this.credentials = new Credentials
            {
                ClientId = settings.Req("clientId"),
                ClientSecret = settings.Req("clientSecret"),
            };
            this.apiEndpointBaseUrl = settings.Req("baseUrl").TrimEnd('/');
        }

        private static readonly IServiceClientSyncConverter serviceClientSyncConverter = new ServiceClientSyncConverterLegacy();
        public TResult ToSync<TResult>(Func<Task<TResult>> action) => serviceClientSyncConverter.ToSync(action);

        public async Task<string> GetTokenAsync()
        {
            var clientId = this.credentials.ClientId;
            var clientSecret = this.credentials.ClientSecret;
            var tokenEndpoint = $"{this.apiEndpointBaseUrl}/auth/open/connect/token";

            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
                request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));

                request.Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", "signicat-api"),
                new KeyValuePair<string, string>("client_id", clientId)
            });

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var stringResult = await response.Content.ReadAsStringAsync();
                var token = JsonConvert.DeserializeObject<TokenResult>(stringResult);

                return token.access_token;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error when trying to fetch Signicat2 token: {ex}");
            }
        }

        private class TokenResult
        {
            public string access_token { get; set; }
        }

        public const string PackageTaskName = "packageTask1";
        public static string GetSignTaskName(int signerNr) => $"signTask{signerNr}";
        public static string GetDocumentId(int signerNr) => $"document{signerNr}";

        public async Task<CaseResponse> CreateCaseAsync(string token, string localSessionId, string documentReference, string successCallbackUrl, string failedCallbackUrl, Dictionary<int, SigningCustomer> signingCustomersBySignerNr)
        {
            var hasBrokenCustomer = signingCustomersBySignerNr.Any(x => x.Key != x.Value.SignerNr);
            if (hasBrokenCustomer)
                throw new Exception("signerNr and dictionary key must be the same");

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"{this.apiEndpointBaseUrl}/enterprise/sign/orders");
            request.Headers.Add("Authorization", $"Bearer {token}");

            var caseRequest = new CaseRequest
            {
                clientReference = localSessionId,
                tasks = new List<TaskItem>(),
                daysUntilDeletion = 7,
                packagingTasks = new List<PackagingTask>()
              {
                  new PackagingTask
                  {
                      id = PackageTaskName,
                      sendToArchive = false,
                      documents = signingCustomersBySignerNr.Keys.Select(signerNr => new PackagingTaskDocument
                      {
                          taskId = GetSignTaskName(signerNr),
                          documentIds = new List<string> { GetDocumentId(signerNr) }
                      }).ToList(),
                      method = "pades"
                  }
              }
            };

            string AddLocalSessionIdToRedirectUrl(string urlPattern)
            {
                if (!urlPattern.Contains("{localSessionId}"))
                {
                    throw new Exception("The callback must contain {localSessionId}.");
                }
                return urlPattern.Replace("{localSessionId}", localSessionId);
            }

            foreach (var customer in signingCustomersBySignerNr)
            {
                caseRequest.tasks.Add(new TaskItem
                {
                    id = GetSignTaskName(customer.Key),
                    language = "fi",
                    daysToLive = 7,
                    documents = new List<Document>
                    {
                      new Document
                      {
                          id = GetDocumentId(customer.Key),
                          description = "Document to sign",
                          action = "SIGN",
                          source = "SESSION",
                          documentRef = documentReference,
                          sendResultToArchive = false
                      }
                    },
                    signatureMethods = new List<SignatureMethod>
                    {
                      new SignatureMethod
                      {
                          name = "ftn-sign",
                          type = "AUTHENTICATION_BASED"
                      }
                    },
                    authentication = new Authentication
                    {
                        methods = new List<string> { "ftn" },
                        artifact = false
                    },
                    onTaskPostpone = AddLocalSessionIdToRedirectUrl(successCallbackUrl),
                    onTaskComplete = AddLocalSessionIdToRedirectUrl(successCallbackUrl),
                    onTaskReject = AddLocalSessionIdToRedirectUrl(failedCallbackUrl),
                    subject = new Subject
                    {
                        id = signingCustomersBySignerNr[customer.Key].SignerNr.ToString(),
                        attributes = new List<SubjectAttribute>
                        {
                          new SubjectAttribute
                          {
                              name = "national-id",
                              value = signingCustomersBySignerNr[customer.Key].CivicRegNr,
                              methods = new List<string> { "ftn-sign" }
                          }
                        }
                    }
                });
            }

            var serializedRequest = JsonConvert.SerializeObject(caseRequest);
            var content = new StringContent(serializedRequest, Encoding.UTF8, "application/json");
            
            request.Content = content;

            var response = await client.SendAsync(request);
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var rawResponse = await response.Content.ReadAsStringAsync();
                var parsedError = JsonConvert.DeserializeAnonymousType(rawResponse, new { title = "", violations = new[] { new { message = "", propertyPath = "" } } });
                if (parsedError?.title != null && (parsedError?.violations?.Length ?? 0) > 0)
                {
                    var violations = string.Join(",", parsedError.violations.Select(x => $"{x?.propertyPath}: {x?.message}"));
                    throw new Exception($"{parsedError.title}: {violations}");
                }
            }

            response.EnsureSuccessStatusCode();
            var responseStr = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<CaseResponse>(responseStr);
        }

        public async Task<string> SendDocumentAsync(string token, byte[] fileContent)
        {
            using (var client = new HttpClient())
            {
                var url = $"{this.apiEndpointBaseUrl}/enterprise/sign/documents";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", $"Bearer {token}");
                var fileContentContent = new ByteArrayContent(fileContent);

                fileContentContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
                request.Content = fileContentContent;

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseStr = await response.Content.ReadAsStringAsync();

                var documentId = JsonConvert.DeserializeAnonymousType(responseStr, new { documentId = "" })?.documentId;
                if (string.IsNullOrWhiteSpace(documentId))
                    throw new Exception($"Missing documentId in response from signicat2");

                return documentId;
            }
        }

        public async Task<TaskStatusCode> GetTaskStatusAsync(string token, string orderId, string taskId)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{this.apiEndpointBaseUrl}/enterprise/sign/orders/{orderId}/tasks/{taskId}/status");
            request.Headers.Add("Authorization", $"Bearer {token}");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var taskStatus = JsonConvert.DeserializeAnonymousType(result, new { taskStatus = "" })?.taskStatus;
            var statusCode = Enums.Parse<TaskStatusCode>(taskStatus, ignoreCase: true);
            if (!statusCode.HasValue)
                throw new Exception($"Unexpected task status {taskStatus} for order {orderId} task {taskId}");
            return statusCode.Value;
        }

        public async Task<PackagingStatusCode> GetPackagingTaskStatusAsync(string token, string orderId, string taskId)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{this.apiEndpointBaseUrl}/enterprise/sign/orders/{orderId}/packaging-tasks/{taskId}/status");
            request.Headers.Add("Authorization", $"Bearer {token}");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            var packagingTaskStatus = JsonConvert.DeserializeAnonymousType(result, new { packagingTaskStatus = "" })?.packagingTaskStatus;
            var statusCode = Enums.Parse<PackagingStatusCode>(packagingTaskStatus, ignoreCase: true);
            if (!statusCode.HasValue)
                throw new Exception($"Unexpected packaging status {packagingTaskStatus} for order {orderId} task {taskId}");
            return statusCode.Value;
        }

        public async Task<byte[]> DownloadPackagingTaskResultDocumentAsync(string token, string orderId, string taskId)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{this.apiEndpointBaseUrl}/enterprise/sign/orders/{orderId}/packaging-tasks/{taskId}/result");
            request.Headers.Add("Authorization", $"Bearer {token}");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        public async Task<(OrderStateCode Status, List<int> SignedByNrs)> GetOrderState(string token, string orderId, List<int> signerNrs)
        {
            var signedByNrs = new List<int>();

            var hasFailedSignatures = false;
            foreach (var signerNr in signerNrs)
            {
                var signTaskStatus = await GetTaskStatusAsync(token, orderId, GetSignTaskName(signerNr));
                if (signTaskStatus == TaskStatusCode.Completed)
                    signedByNrs.Add(signerNr);
                else if (signTaskStatus != TaskStatusCode.Created && signTaskStatus != TaskStatusCode.Completed)
                    hasFailedSignatures = true;
            }

            if (hasFailedSignatures || signedByNrs.Count < signerNrs.Count)
                return (Status: hasFailedSignatures ? OrderStateCode.Failed : OrderStateCode.WaitingForSignatures, SignedByNrs: signedByNrs);

            var packageTaskStatus = await GetPackagingTaskStatusAsync(token, orderId, PackageTaskName);

            OrderStateCode finalStatus;
            if (packageTaskStatus == PackagingStatusCode.Created)
                finalStatus = OrderStateCode.WaitingForPackaging;
            else if (packageTaskStatus == PackagingStatusCode.Completed)
                finalStatus = OrderStateCode.CompletedSuccessfully;
            else
                finalStatus = OrderStateCode.Failed;

            return (Status: finalStatus, SignedByNrs: signedByNrs);
        }

    }

    public enum PackagingStatusCode
    {
        Created,
        Completed,
        Failed
    }

    public enum TaskStatusCode
    {
        Created,
        Completed,
        Rejected,
        Expired,
        Deleted
    }

    public class Document
    {
        public string id { get; set; }
        public string description { get; set; }
        public string action { get; set; }
        public string source { get; set; }
        public string documentRef { get; set; }
        public bool sendResultToArchive { get; set; }
    }

    public class SignatureMethod
    {
        public string name { get; set; }
        public string type { get; set; }
    }

    public class Authentication
    {
        public List<string> methods { get; set; }
        public bool artifact { get; set; }
    }

    public class SubjectAttribute
    {
        public string name { get; set; }
        public string value { get; set; }
        public List<string> methods { get; set; }
    }

    public class Subject
    {
        public string id { get; set; }
        public List<SubjectAttribute> attributes { get; set; }
    }

    public class CaseRequest
    {
        public string clientReference { get; set; }
        public List<TaskItem> tasks { get; set; }
        public int daysUntilDeletion { get; set; }
        public List<PackagingTask> packagingTasks { get; set; }
    }

    public class TaskItem
    {
        public string id { get; set; }
        public string language { get; set; }
        public int daysToLive { get; set; }
        public List<Document> documents { get; set; }
        public List<SignatureMethod> signatureMethods { get; set; }
        public Authentication authentication { get; set; }
        public string onTaskPostpone { get; set; }
        public string onTaskComplete { get; set; }
        public string onTaskReject { get; set; }
        public Subject subject { get; set; }
    }

    public class PackagingTask
    {
        public string id { get; set; }
        public bool sendToArchive { get; set; }
        public string method { get; set; }
        public List<PackagingTaskDocument> documents { get; set; }
    }
    public class PackagingTaskDocument
    {
        public string taskId { get; set; }
        public List<string> documentIds { get; set; }
    }

    public class CaseResponse
    {
        public string id { get; set; }
        public string clientReference { get; set; }
        public List<TaskItem> tasks { get; set; }
        public List<Notification> notifications { get; set; }
        public int daysUntilDeletion { get; set; }
        public List<PackagingTask> packagingTasks { get; set; }
        public List<NotificationTemplateParams> notificationTemplateParams { get; set; }

        public class Document
        {
            public string id { get; set; }
            public string description { get; set; }
            public string action { get; set; }
            public string source { get; set; }
            public string documentRef { get; set; }
            public string externalReference { get; set; }
            public string signTextEntry { get; set; }
            public bool sendResultToArchive { get; set; }
        }

        public class SignatureMethod
        {
            public string name { get; set; }
            public string type { get; set; }
            public bool handwritten { get; set; }
        }

        public class Authentication
        {
            public List<string> methods { get; set; }
            public bool artifact { get; set; }
        }

        public class Dialog
        {
            public string title { get; set; }
            public string message { get; set; }
        }

        public class Notification
        {
            public string id { get; set; }
            public string recipient { get; set; }
            public string sender { get; set; }
            public string header { get; set; }
            public string message { get; set; }
            public string type { get; set; }
        }

        public class SubjectAttribute
        {
            public string name { get; set; }
            public string value { get; set; }
            public List<string> methods { get; set; }
        }

        public class Subject
        {
            public string id { get; set; }
            public List<SubjectAttribute> attributes { get; set; }
        }

        public class TaskItem
        {
            public string id { get; set; }
            public string profile { get; set; }
            public string language { get; set; }
            public int daysToLive { get; set; }
            public List<Document> documents { get; set; }
            public List<SignatureMethod> signatureMethods { get; set; }
            public Authentication authentication { get; set; }
            public string onTaskPostpone { get; set; }
            public string onTaskComplete { get; set; }
            public string onTaskReject { get; set; }
            public Dialog dialog { get; set; }
            public List<Notification> notifications { get; set; }
            public string signText { get; set; }
            public Subject subject { get; set; }
            public string signingUrl { get; set; }
        }

        public class DocumentReference
        {
            public string taskId { get; set; }
            public List<string> documentIds { get; set; }
        }

        public class PackagingTask
        {
            public string id { get; set; }
            public bool sendToArchive { get; set; }
            public string method { get; set; }
            public List<Notification> notifications { get; set; }
            public List<DocumentReference> documents { get; set; }
        }

        public class NotificationTemplateParams
        {
            public string id { get; set; }
            public string footer { get; set; }
            public string button { get; set; }
            public string linkText { get; set; }
            public string logoUrl { get; set; }
        }
    }
}


