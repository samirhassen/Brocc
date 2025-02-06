using Microsoft.Extensions.Logging;
using NTech.Banking.CivicRegNumbers;
using NTech.Services.Infrastructure;
using NTechSignicat.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NTechSignicat.Services
{
    public class SignicatSignatureService : SignicatSignatureServiceBase
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly SignicatSettings settings;
        private readonly WcfLoggingMessageInspectorAndBehaviour wcfLoggingMessageInspectorAndBehaviour;
        private readonly ILogger<SignicatSignatureService> logger;
        private readonly INEnv env;

        public SignicatSignatureService(IHttpClientFactory httpClientFactory, SignicatSettings settings, WcfLoggingMessageInspectorAndBehaviour wcfLoggingMessageInspectorAndBehaviour, IDocumentDatabaseService documentDatabaseService, IDocumentService documentService, ILogger<SignicatSignatureService> logger, INEnv env) : base(documentDatabaseService, documentService, env)
        {
            this.httpClientFactory = httpClientFactory;
            this.settings = settings;
            this.wcfLoggingMessageInspectorAndBehaviour = wcfLoggingMessageInspectorAndBehaviour;
            this.logger = logger;
            this.env = env;
        }

        private HttpClient CreateSdsHttpClient(string method)
        {
            var handler = new System.Net.Http.HttpClientHandler();

            if (settings.HasSignatureClientCertificate)
            {
                handler.ClientCertificateOptions = System.Net.Http.ClientCertificateOption.Manual;
                var clientCert = SignicatSignatureService.LoadClientCertificate(settings);
                logger.LogTrace($"{method}: Using client certificate {clientCert.Thumbprint}");
                handler.ClientCertificates.Add(clientCert);
            }
            else
            {
                logger.LogTrace($"{method}: Not using a certificate");
            }

            return new HttpClient(handler);
        }

        private async Task<string> UploadFileToSds(byte[] fileBytes, string contentType)
        {
            var url = NTechServiceRegistry.CreateUrl(settings.SignicatUrl, $"doc/{settings.ServiceName}/sds");
            var request = new HttpRequestMessage(HttpMethod.Post, url.ToString());
            request.Headers.Add("Authorization", $"Basic {System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{settings.SignatureUsername}:{settings.SignaturePassword}"))}");
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
            request.Content = fileContent;

            var client = CreateSdsHttpClient("UploadFileToSds");
            var result = await client.SendAsync(request);
            if (result.StatusCode == System.Net.HttpStatusCode.Created)
            {
                return await result.Content.ReadAsStringAsync();
            }
            else
            {
                if (result.Content.Headers.ContentLength > 0)
                {
                    var resultString = await result.Content.ReadAsStringAsync();
                    throw new Exception($"Error: {resultString} - {result.StatusCode} - {result.ReasonPhrase}");
                }
                else
                    throw new Exception($"Error: {result.StatusCode} - {result.ReasonPhrase}");
            }
        }

        public static System.Security.Cryptography.X509Certificates.X509Certificate2 LoadClientCertificate(SignicatSettings signicatSettings)
        {
            if (signicatSettings.SignatureClientCertificateThumbPrint != null)
                return LoadClientCertificateUsingThumbPrint(signicatSettings.SignatureClientCertificateThumbPrint);
            else
                return LoadClientCertificateFromFile(signicatSettings.SignatureClientCertificateFile, signicatSettings.SignatureClientCertificateFilePassword);
        }

        private static System.Security.Cryptography.X509Certificates.X509Certificate2 LoadClientCertificateUsingThumbPrint(string certificateThumbPrint)
        {
            using (var keyStore = new System.Security.Cryptography.X509Certificates.X509Store(System.Security.Cryptography.X509Certificates.StoreName.My, System.Security.Cryptography.X509Certificates.StoreLocation.LocalMachine))
            {
                keyStore.Open(System.Security.Cryptography.X509Certificates.OpenFlags.ReadOnly);
                return keyStore
                    .Certificates
                    .OfType<System.Security.Cryptography.X509Certificates.X509Certificate2>()
                    .First(x => x.Thumbprint.Equals(certificateThumbPrint, StringComparison.OrdinalIgnoreCase));
            }
        }

        private static System.Security.Cryptography.X509Certificates.X509Certificate2 LoadClientCertificateFromFile(string certificateFilename, string certificatePassword = null)
        {
            if (certificatePassword != null)
                return new System.Security.Cryptography.X509Certificates.X509Certificate2(System.IO.File.ReadAllBytes(certificateFilename), certificatePassword);
            else
                return new System.Security.Cryptography.X509Certificates.X509Certificate2(System.IO.File.ReadAllBytes(certificateFilename));
        }

        public const string SignicatSdsServiceHttpClientName = "SdsDocumentService";

        private async Task<byte[]> DownloadFileFromSdsByUrl(string url, string expectedContentType)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url.ToString());
            request.Headers.Add("Authorization", $"Basic {System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{settings.SignatureUsername}:{settings.SignaturePassword}"))}");
            var client = CreateSdsHttpClient("UploadFileToSds");

            var result = await client.SendAsync(request);
            if (result.StatusCode == System.Net.HttpStatusCode.OK && result.Content.Headers.ContentType.MediaType.Contains(expectedContentType))
            {
                return await result.Content.ReadAsByteArrayAsync();
            }
            else
            {
                if (result.Content.Headers.ContentLength > 0 && result.Content.Headers.ContentLength < 2000)
                {
                    var resultString = await result.Content.ReadAsStringAsync();
                    throw new Exception($"Error: {resultString} - {result.StatusCode} - {result.ReasonPhrase}");
                }
                else
                    throw new Exception($"Error: {result.StatusCode} - {result.ReasonPhrase}");
            }
        }

        private async Task<byte[]> DownloadFileFromSds(string code, string expectedContentType)
        {
            var url = NTechServiceRegistry.CreateUrl(settings.SignicatUrl, $"doc/{settings.ServiceName}/sds/{code}");
            return await DownloadFileFromSdsByUrl(url.ToString(), expectedContentType);
        }

        private void SetSaneWcfSizeLimitDefaults<T>(T b) where T : HttpBindingBase
        {
            b.MaxReceivedMessageSize = 20000000L;
            b.MaxBufferSize = 20000000;
            if (b.ReaderQuotas == null)
            {
                b.ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas();
            }
            b.ReaderQuotas.MaxDepth = 32;
            b.ReaderQuotas.MaxArrayLength = 200000000;
            b.ReaderQuotas.MaxStringContentLength = 200000000;
        }

        private System.Security.Cryptography.X509Certificates.X509Certificate2 LoadClientCertificateWithLogging(string method)
        {
            if (settings.HasSignatureClientCertificate)
            {
                var clientCert = SignicatSignatureService.LoadClientCertificate(settings);
                logger.LogTrace($"{method}: Using client certificate {clientCert.Thumbprint}");
                return clientCert;
            }
            else
            {
                logger.LogTrace($"{method}: Not using a certificate");
                return null;
            }
        }

        private SignicatDocumentServiceV3.DocumentEndPointClient CreateDocumentServiceClient(string method)
        {
            var clientCert = LoadClientCertificateWithLogging(method);
            var binding = CreateHttpsBinding(clientCert != null);
            SetSaneWcfSizeLimitDefaults(binding);
            var address = new EndpointAddress(NTechServiceRegistry.CreateUrl(settings.SignicatUrl, "ws/documentservice-v3").ToString());
            var c = new SignicatDocumentServiceV3.DocumentEndPointClient(binding, address);
            c.Endpoint.EndpointBehaviors.Add(wcfLoggingMessageInspectorAndBehaviour);
            SetupClientCertificate(c, clientCert);
            return c;
        }

        private BasicHttpsBinding CreateHttpsBinding(bool usesClientCertificate)
        {
            var binding = new BasicHttpsBinding();
            binding.Security.Mode = BasicHttpsSecurityMode.Transport;
            if (usesClientCertificate)
            {
                binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
            }
            return binding;
        }

        private void SetupClientCertificate<T>(ClientBase<T> c, System.Security.Cryptography.X509Certificates.X509Certificate2 clientCert) where T : class
        {
            if (clientCert == null)
                return;
            c.ClientCredentials.ClientCertificate.Certificate = clientCert;
        }

        private SignicatPackagingServiceV4.PackagingEndPointClient CreatePackagingServiceClient(string method)
        {
            var clientCert = LoadClientCertificateWithLogging(method);
            var certificateThumbPrint = settings.SignatureClientCertificateThumbPrint;
            var binding = CreateHttpsBinding(clientCert != null);
            SetSaneWcfSizeLimitDefaults(binding);
            var address = new EndpointAddress(NTechServiceRegistry.CreateUrl(settings.SignicatUrl, "ws/packagingservice-v4").ToString());
            var c = new SignicatPackagingServiceV4.PackagingEndPointClient(binding, address);
            c.Endpoint.EndpointBehaviors.Add(wcfLoggingMessageInspectorAndBehaviour);
            SetupClientCertificate(c, clientCert);
            return c;
        }

        protected override async Task<byte[]> PackageAsPdf(string requestId, List<string> documentUris)
        {
            var p = CreatePackagingServiceClient("PackageAsPdf");
            var pr = await p.createpackageAsync(new SignicatPackagingServiceV4.createpackagerequest
            {
                service = this.settings.ServiceName,
                password = this.settings.SignaturePassword,
                packagingmethod = this.settings.SignaturePackagingMethod,
                validationpolicy = "ltvsdo-validator",
                Items = documentUris.Select(x => new SignicatPackagingServiceV4.documentid
                {
                    uridocumentid = x
                }).ToArray(),
                sendresulttoarchive = false
            });

            var errorMessage = pr.createpackageresponse.error?.message;
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new Exception($"PackageAsPdf failed: {pr.createpackageresponse.error.code} - {pr.createpackageresponse.error.message}");
            }

            var signedDocumentBytes = await DownloadFileFromSds(pr.createpackageresponse.id, "application/x-pades");
            return signedDocumentBytes;
        }

        private static bool UsesAuthenticationBasedSignatures(SignicatLoginMethodCode loginMethodCode)
        {
            return loginMethodCode == SignicatLoginMethodCode.FinnishTrustNetwork;
        }

        protected override async Task<CreateSignatureResponse> CreateSignatureRequest(
          Dictionary<int, SignatureRequestCustomer> signingCustomersByApplicantNr,
          List<SignaturePdf> pdfs)
        {
            Func<string, string> createRedirectUrl = x => NTechServiceRegistry.CreateUrl(this.settings.SelfExternalUrl, "signature-redirect",
                                    Tuple.Create("request_id", "${requestId}"), Tuple.Create("taskId", "${taskId}"), Tuple.Create("status", x)).ToString();

            var uploadedDocuments = new List<CreateSignatureResponse.Document>();
            foreach (var pdf in pdfs)
            {
                var documentSdsCode = await this.UploadFileToSds(pdf.PdfBytes, "application/pdf");
                uploadedDocuments.Add(new CreateSignatureResponse.Document
                {
                    SdsCode = documentSdsCode,
                    Pdf = pdf
                });
            }

            var requestDocuments = new List<SignicatDocumentServiceV3.document>();
            foreach (var x in uploadedDocuments)
            {
                requestDocuments.Add(new SignicatDocumentServiceV3.sdsdocument
                {
                    id = x.Pdf.DocumentId,
                    refsdsid = x.SdsCode,
                    sendtoarchiveSpecified = true,
                    sendtoarchive = false,
                    description = x.Pdf.PdfDisplayFileName
                });
            }

            var request = new SignicatDocumentServiceV3.request
            {
                language = signingCustomersByApplicantNr.First().Value.UserLanguage,
                document = requestDocuments.ToArray()
            };
            var tasks = new List<SignicatDocumentServiceV3.task>();
            var subjects = new List<SignicatDocumentServiceV3.subject>();

            var sessionCustomerByApplicantNr = new Dictionary<int, SignatureSession.SigningCustomer>();

            foreach (var applicantNr in signingCustomersByApplicantNr.Keys.OrderBy(x => x))
            {
                var c = signingCustomersByApplicantNr[applicantNr];
                var taskId = c.ApplicantNr.ToString();
                sessionCustomerByApplicantNr[applicantNr] = new SignatureSession.SigningCustomer
                {
                    ApplicantNr = c.ApplicantNr,
                    SignicatTaskId = taskId,
                    UserLanguage = c.UserLanguage,
                    CivicRegNr = c.CivicRegNr.NormalizedValue,
                    CivicRegNrCountry = c.CivicRegNr.Country
                };
                var subject = new SignicatDocumentServiceV3.subject
                {
                    id = $"subject{applicantNr}",
                    nationalid = c.CivicRegNr.NormalizedValue,
                    firstname = c.FirstName,
                    lastname = c.LastName
                };

                settings.WithTestReplacementCivicRegNr(c.CivicRegNr, applicantNr, x =>
                {
                    subject.nationalid = x.NormalizedValue;
                    sessionCustomerByApplicantNr[applicantNr].UsesTestReplacementCivicRegNrs = true;
                }, false);

                subjects.Add(subject);

                tasks.Add(new SignicatDocumentServiceV3.task
                {
                    id = taskId,
                    bundle = false,
                    bundleSpecified = true,
                    subjectref = subject.id,
                    documentaction = request.document.Select(x => new SignicatDocumentServiceV3.documentaction
                    {
                        type = SignicatDocumentServiceV3.documentactiontype.sign,
                        documentref = x.id
                    }).ToArray(),
                    configuration = "default",
                    authenticationbasedsignature = UsesAuthenticationBasedSignatures(c.SignicatLoginMethod) ? new[]
                                {
                                    new SignicatDocumentServiceV3.authenticationbasedsignature
                                    {
                                        method = new []
                                        {
                                            new SignicatDocumentServiceV3.method
                                            {
                                                Value = GetSignatureMethod(c.SignicatLoginMethod)
                                            }
                                        }
                                    }
                                } : null,
                    signature = UsesAuthenticationBasedSignatures(c.SignicatLoginMethod) ? null : new[]
                    {
                        new SignicatDocumentServiceV3.signature
                        {
                           method = new []
                                {
                                    new SignicatDocumentServiceV3.method
                                    {
                                        Value = GetSignatureMethod(c.SignicatLoginMethod)
                                    }
                                }
                        }
                    },
                    ontaskcancel = createRedirectUrl("taskcanceled"),
                    ontaskpostpone = createRedirectUrl("taskpostponed"),
                    ontaskcomplete = createRedirectUrl("taskcomplete")
                });
            }
            request.subject = subjects.ToArray();
            request.task = tasks.ToArray();

            //Full example: https://developer.signicat.com/documentation/signing/get-started-with-signing/full-flow-example/
            var client = CreateDocumentServiceClient("CreateSignatureRequest");

            var result = await client.createRequestAsync(new SignicatDocumentServiceV3.createrequestrequest
            {
                service = settings.ServiceName,
                password = settings.SignaturePassword,
                request = new SignicatDocumentServiceV3.request[] { request }
            });

            var requestId = result.createrequestresponse.requestid.First();

            var response = new CreateSignatureResponse
            {
                RequestId = result.createrequestresponse.requestid.First(),
                SigningCustomersByApplicantNr = sessionCustomerByApplicantNr,
                Documents = uploadedDocuments
            };

            foreach (var c in sessionCustomerByApplicantNr)
            {
                c.Value.SignicatSignatureUrl = NTechServiceRegistry.CreateUrl(this.settings.SignicatUrl, $"std/docaction/{this.settings.ServiceName}",
                    Tuple.Create("request_id", requestId), Tuple.Create("task_id", c.Value.ApplicantNr.ToString())).ToString();
            }

            return response;
        }

        protected override async Task<Dictionary<string, DocumentSignatureResult>> GetSignatureStatusAndSignedDocumentUris(SignatureSession session)
        {
            var client = CreateDocumentServiceClient("GetSignatureStatusAndSignedDocumentUris");
            var result = await client.getStatusAsync(new SignicatDocumentServiceV3.getstatusrequest
            {
                service = settings.ServiceName,
                password = settings.SignaturePassword,
                requestid = new string[] { session.SignicatRequestId }
            });
            return result
                .getstatusresponse1
                .GroupBy(x => x.taskid)
                .ToDictionary(x => x.Key, x =>
                {
                    var taskResult = x.Single();
                    var taskStatus = taskResult?.taskstatus;

                    var documents = new Dictionary<string, string>();
                    if(taskResult.documentstatus != null)
                    {
                        foreach(var documentStatus in taskResult.documentstatus)
                        {
                            if(documentStatus != null && documentStatus.id != null && documentStatus.resulturi != null)
                                documents[documentStatus.id] = documentStatus.resulturi;
                        }
                    }

                    return new DocumentSignatureResult
                    {
                        TaskStatus = taskStatus,
                        SignedDocumentUriByDocumentId = documents
                    };
                });
        }

        private List<ICivicRegNumber> GetExpectedCivicRegNumbers(SignatureSession session)
        {
            var actualExpectedCivicRegNumbers = session.SigningCustomersByApplicantNr.Values.Select(x => SignicatLoginMethodValidator.ParseCivicRegNr(x.CivicRegNrCountry, x.CivicRegNr)).ToList();
            if (env.IsProduction)
                return actualExpectedCivicRegNumbers;
            if (!session.SigningCustomersByApplicantNr.Any(x => x.Value.UsesTestReplacementCivicRegNrs))
                return actualExpectedCivicRegNumbers;

            var replacedNumbers = new List<ICivicRegNumber>();
            foreach (var c in session.SigningCustomersByApplicantNr.Select(x => new { applicantNr = x.Key, civicRegNr = CivicRegNumberParser.Parse(x.Value.CivicRegNrCountry, x.Value.CivicRegNr) }))
            {
                var civicRegNr = c.civicRegNr;
                settings.WithTestReplacementCivicRegNr(civicRegNr, c.applicantNr, x => civicRegNr = x, false);
                replacedNumbers.Add(civicRegNr);
            }
            return replacedNumbers.Distinct().ToList();
        }

        protected override async Task<(bool isOk, List<ICivicRegNumber> signedByCivicRegNrs)> VerifyIsSignedByAtLeastThesePersons(SignatureSession session, List<string> resultUris)
        {
            var expectedCivicRegNumbers = GetExpectedCivicRegNumbers(session);

            session.RawSignaturePackages = session.RawSignaturePackages ?? new List<string>();
            var allSignerCivicRegNrs = new List<ICivicRegNumber>();
            foreach (var resultUri in resultUris)
            {
                var resultDocument = XDocument.Load(new MemoryStream(await DownloadFileFromSdsByUrl(resultUri, "application/x-ltv-sdo")));
                if (env.LogFolder != null && (env.IsVerboseLoggingEnabled || env.OptionalSetting("ntech.signicat.logsdo") == "true"))
                {
                    var p = Path.Combine(env.LogFolder.FullName, "Signicat");
                    Directory.CreateDirectory(p);
                    var f = Path.Combine(p, $"SDO-{session.Id}-{session.SignicatRequestId}-{Guid.NewGuid().ToString()}.xml");
                    resultDocument.Save(f);
                }
                session.RawSignaturePackages.Add(resultDocument.ToString());
                var signerCivicRegNrs = resultDocument.Descendants().Where(x => x.Name.LocalName == "SignerDescription").Select(x => new
                {
                    SignerNationalId = x.Descendants().Single(y => y.Name.LocalName == "SignerNationalId").Value,
                    SignerNationality = x.Descendants().Single(y => y.Name.LocalName == "SignerNationality").Value,
                }).Select(x => SignicatLoginMethodValidator.ParseCivicRegNr(x.SignerNationality, x.SignerNationalId)).Distinct().ToList();

                allSignerCivicRegNrs.AddRange(signerCivicRegNrs);
            }

            var e = expectedCivicRegNumbers.Select(x => x.NormalizedValue).ToHashSet();
            var a = allSignerCivicRegNrs.Select(x => x.NormalizedValue).ToHashSet();

            return (e.IsSubsetOf(a), allSignerCivicRegNrs.Distinct().ToList());
        }
    }
}