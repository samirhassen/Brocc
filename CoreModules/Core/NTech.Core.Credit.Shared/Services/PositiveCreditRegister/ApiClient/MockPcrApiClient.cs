using nCredit.DbModel.Repository;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using NTech.Core.Credit.Shared.Database;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Text;
using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.PositiveCreditRegisterExportService;
using nCredit;
using NTech.Core.Module.Shared.Clients;

namespace NTech.Core.Credit.Shared.Services.PositiveCreditRegister.ApiClient
{
    internal class MockPcrApiClient : PcrApiClient
    {
        public MockPcrApiClient(ICreditEnvSettings envSettings, ICoreClock clock, IServiceClientSyncConverter syncConverter) : base(envSettings, clock, syncConverter)
        {            
        }

        public override (HttpResponseMessage responseMessage, List<string> Warnings) SendBatch(object fields, BatchType batchType, string requestUrl, ICreditContextExtended context, CoreSystemItemRepository repo)
        {
            if (envSettings.IsProduction)
                throw new Exception("Mock not allowed in production");

            if (batchType == BatchType.CheckBatchStatus)
                throw new Exception("Use the CheckBatchStatus method instead");


            var warnings = new List<string>();

            var handler = new HttpClientHandler();
            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            var jsonSettings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new StringEnumConverter() }
            };

            var batchReference = $"Batch_{batchType}_{clock.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 4)}";
            request.Headers.Add("BatchReference", batchReference);
            SetLog(batchType, batchReference, context, repo);

            request.Content = new StringContent(JsonConvert.SerializeObject(fields, jsonSettings), Encoding.UTF8, "application/json");

            httpLogger.LogHttpRequestSent(request, warnings, batchReference);

            ObserveSendBatch?.Invoke(request);

            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Accepted
            };

            return (responseMessage, warnings);
        }

        public override (List<string> Warnings, PcrBatchCheckResult BatchStatus) CheckBatchStatus(string batchReference, ICreditContextExtended context, CoreSystemItemRepository repo)
        {
            var fields = CreateCheckBatchStatusFields(batchReference);
            var result = SendBatch(fields, BatchType.CheckBatchStatus, Settings.CheckBatchStatusEndpointUrl, context, repo);
            var mockCode = Settings.MockPcrBatchStatusFailureCode;
            var batchResult = new PcrBatchCheckResult
            {
                BatchReference = batchReference,
                IsFinishedSuccess = mockCode == null,
                BatchStatusCode = mockCode ?? "FinishedSuccess"
            };

            return (result.Warnings, BatchStatus: batchResult);
        }

        public override string FetchRawGetLoanResponse(string creditNr)
        {
            throw new NotImplementedException();
        }
    }
}
