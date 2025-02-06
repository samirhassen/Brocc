using nCredit.DbModel.Repository;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using NTech.Core.Credit.Shared.Database;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.PositiveCreditRegisterExportService;
using nCredit;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using NTech.Core.Module.Shared.Clients;
using System.IO;
using Newtonsoft.Json.Linq;
using NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models;
using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models.BaseLoanExportRequestModel;

namespace NTech.Core.Credit.Shared.Services.PositiveCreditRegister.ApiClient
{
    internal class RealPcrApiClient : PcrApiClient
    {
        public RealPcrApiClient(ICreditEnvSettings envSettings, ICoreClock clock, IServiceClientSyncConverter syncConverter) : base(envSettings, clock, syncConverter)
        {
        }

        public override (HttpResponseMessage responseMessage, List<string> Warnings) SendBatch(object fields, BatchType batchType, string requestUrl, ICreditContextExtended context, CoreSystemItemRepository repo)
        {
            if (batchType == BatchType.CheckBatchStatus)
                throw new Exception("Use the CheckBatchStatus method instead");

            var warnings = new List<string>();

            var shortGuid = Guid.NewGuid().ToString("N").Substring(0, 4);
            var batchReference = $"Batch_{batchType}_{clock.Now:yyyyMMddHHmmss}_{shortGuid}";

            var result = SendHttpRequestToPcrApi(requestUrl, batchReference, fields, x =>
            {
                ObserveSendBatch?.Invoke(x);
                httpLogger.LogHttpRequestSent(x, warnings, batchReference);
            });

            SetLog(batchType, batchReference, context, repo);

            httpLogger.LogHttpResponsedReceived(result, warnings, batchReference);

            return (result, warnings);
        }

        public override (List<string> Warnings, PcrBatchCheckResult BatchStatus) CheckBatchStatus(string batchReference, ICreditContextExtended context, CoreSystemItemRepository repo)
        {
            var fields = CreateCheckBatchStatusFields(batchReference);

            var warnings = new List<string>();

            var result = SendHttpRequestToPcrApi(Settings.CheckBatchStatusEndpointUrl, null, fields, x =>
            {
                httpLogger.LogHttpRequestSent(x, warnings, batchReference);
            });

            var responseContent = syncConverter.ToSync(() => result.Content.ReadAsStringAsync());
            var responseJson = JObject.Parse(responseContent);
            var checkBatchStatusStatusCode = responseJson["batchStatus"].ToString();
            var batchRefProperty = fields.GetType().GetProperty("batchReference");
            var batchRef = batchRefProperty.GetValue(fields, null);

            httpLogger.LogHttpResponsedReceived(result, warnings, batchReference);

            var statusResult = $"{batchRef} [status:] {checkBatchStatusStatusCode}"; //Right now we only log, possibly todo alert at failed

            SetLog(BatchType.CheckBatchStatus, statusResult, context, repo);

            return (warnings, 
                BatchStatus: new PcrBatchCheckResult
                {
                    BatchReference = batchReference,
                    IsFinishedSuccess = checkBatchStatusStatusCode == "FinishedSuccess",
                    BatchStatusCode = checkBatchStatusStatusCode
                });
        }

        private HttpResponseMessage SendHttpRequestToPcrApi(string requestUrl, string headerBatchReference, object fields, Action<HttpRequestMessage> observeRequest)
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual
            };

            var cert = LoadClientCertificateUsingThumbPrint(Settings.CertificateThumbPrint);
            if (cert == null)
            {
                throw new Exception("Could not find certificate with that thumbprint.");
            };

            handler.ClientCertificates.Add(cert);
            var client = new HttpClient(handler);

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            var jsonSettings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new StringEnumConverter() },
                NullValueHandling = NullValueHandling.Ignore
            };

            if (headerBatchReference != null)
            {
                request.Headers.Add("BatchReference", headerBatchReference);
            }

            request.Content = new StringContent(JsonConvert.SerializeObject(fields, jsonSettings), Encoding.UTF8, "application/json");

            observeRequest?.Invoke(request);

            return syncConverter.ToSync(() => client.SendAsync(request));
        }

        private (bool isSuccess, string failedMessage) TryLogResponseToFile(HttpResponseMessage response, string folderName = null)
        {
            try
            {
                string responseLogFilePathPre = Path.Combine(Settings.LogFilePath, !string.IsNullOrEmpty(folderName) ? folderName : "responses");

                if (!Directory.Exists(responseLogFilePathPre))
                {
                    Directory.CreateDirectory(responseLogFilePathPre);
                }

                string filename = $"response_{clock.Now:yyyyMMddHHmmss}.txt";
                string responseLogFilePath = Path.Combine(responseLogFilePathPre, filename);

                int postfix = 0;
                while (File.Exists(responseLogFilePath))
                {
                    postfix++;
                    string newFilename = $"response_{clock.Now:yyyyMMddHHmmss}_{postfix}.txt";
                    responseLogFilePath = Path.Combine(responseLogFilePathPre, newFilename);
                }

                string responseLog = $"Response Status Code: {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}";
                responseLog += $"Response Headers:{Environment.NewLine}";

                foreach (var header in response.Headers)
                {
                    responseLog += $"{header.Key}: {string.Join(", ", header.Value)}{Environment.NewLine}";
                }

                responseLog += Environment.NewLine;
                responseLog += "Response Content:" + Environment.NewLine;
                responseLog += syncConverter.ToSync(() => response.Content.ReadAsStringAsync());

                File.WriteAllText(responseLogFilePath, responseLog);

                return (isSuccess: true, null);
            }
            catch (Exception ex)
            {
                return (isSuccess: false, failedMessage: ex.Message);
            }
        }

        public override string FetchRawGetLoanResponse(string creditNr)
        {
            var warnings = new List<string>();
            var errors = new List<string>();

            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual
            };

            var cert = LoadClientCertificateUsingThumbPrint(Settings.CertificateThumbPrint);
            if (cert == null)
            {
                throw new Exception("Could not find certificate with that thumbprint.");
            };

            handler.ClientCertificates.Add(cert);
            var client = new HttpClient(handler);

            var request = new HttpRequestMessage(HttpMethod.Post, Settings.GetLoanEndpointUrl);
            var jsonSettings = new JsonSerializerSettings { Converters = new List<JsonConverter> { new StringEnumConverter() } };

            var fields = new GetLoanRequestModel
            {
                TargetEnvironment = Settings.IsTargetProduction ? TargetEnvironment.Production : TargetEnvironment.Test,
                Owner = new Owner
                {
                    IdCodeType = IdCodeType.BusinessId,
                    IdCode = Settings.OwnerIdCode
                },
                LoanNumber = new LoanNumber
                {
                    Type = LoanNumberType.Other,
                    Number = creditNr,
                }
            };

            request.Content = new StringContent(JsonConvert.SerializeObject(fields, jsonSettings), Encoding.UTF8, "application/json");
            var result = syncConverter.ToSync(() => client.SendAsync(request));
            string responseContent = syncConverter.ToSync(() => result.Content.ReadAsStringAsync());

            return responseContent;
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
    }
}
