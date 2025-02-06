using nCredit;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace NTech.Core.Credit.Shared.Services.PositiveCreditRegister
{
    public class PcrLoggingService
    {
        protected readonly ICreditEnvSettings envSettings;
        protected readonly ICoreClock clock;
        protected readonly IServiceClientSyncConverter syncConverter;

        protected PositiveCreditRegisterSettingsModel Settings => envSettings.PositiveCreditRegisterSettings;

        public PcrLoggingService(ICreditEnvSettings envSettings, ICoreClock clock, IServiceClientSyncConverter syncConverter)
        {
            this.envSettings = envSettings;
            this.clock = clock;
            this.syncConverter = syncConverter;
        }

        public GetPcrBatchLogsResponse GetBatchLogs(GetPcrBatchLogsRequest request)
        {
            var response = new GetPcrBatchLogsResponse 
            {
                LogFiles = new List<GetPcrLogsResponseItem>()
            };
            if (string.IsNullOrWhiteSpace(Settings.LogFilePath) || !Directory.Exists(BatchLogsFolder))
                return response;
            
            foreach(var logFile in new DirectoryInfo(BatchLogsFolder).GetFiles($"{request.LogCorrelationId}*.txt"))
            {
                response.LogFiles.Add(new GetPcrLogsResponseItem
                {
                    IsRequestLog = logFile.Name.Contains("_request_"),
                    IsResponseLog = logFile.Name.Contains("_response_"),
                    LogDate = logFile.CreationTime,
                    LogFileName = logFile.Name
                });
            }

            response.LogFiles = response.LogFiles.OrderByDescending(x => x.LogDate).ThenBy(x => x.LogFileName).ToList();

            return response;
        }

        public string GetLogfileContent(string logCorrelationId, string logFilename)
        {
            if (string.IsNullOrWhiteSpace(Settings.LogFilePath) || string.IsNullOrWhiteSpace(logCorrelationId) || string.IsNullOrWhiteSpace(logFilename) || !Directory.Exists(BatchLogsFolder))
                return null;

            var filename = Path.Combine(BatchLogsFolder, logFilename);
            if(!filename.Contains(logCorrelationId) || !File.Exists(filename))
                return null;

            return File.ReadAllText(filename);
        }

        internal void LogHttpRequestSent(HttpRequestMessage request, List<string> warnings, string logCorrelationId)
        {
            if (!Settings.IsLogRequestToFileEnabled)
                return;

            if (string.IsNullOrEmpty(Settings.LogFilePath))
            {
                warnings.Add("IsLogRequestToFileEnabled but no LogFilePath is configured.");
            }
            else
            {
                var logRequest = TryLogRequestToFile(request, logCorrelationId);
                if (!logRequest.isSuccess)
                {
                    warnings.Add(logRequest.failedMessage ?? "Could not log request to file.");
                }
            }            
        }

        internal void LogHttpResponsedReceived(HttpResponseMessage result, List<string> warnings, string logCorrelationId)
        {
            if (!Settings.IsLogResponseToFileEnabled)
                return;
            
            if (string.IsNullOrEmpty(Settings.LogFilePath))
            {
                warnings.Add("IsLogResponseToFileEnabled but no LogFilePath is configured.");
            }
            else
            {
                var logResponse = TryLogResponseToFile(result, logCorrelationId);
                if (!logResponse.isSuccess)
                {
                    warnings.Add(logResponse.failedMessage ?? "Could not log response to file.");
                }
            }            
        }

        private string BatchLogsFolder => Path.Combine(Settings.LogFilePath, "batchLogs");

        private (bool isSuccess, string failedMessage) TryLogRequestToFile(HttpRequestMessage request, string logCorrelationId)
        {
            try
            {
                Directory.CreateDirectory(BatchLogsFolder);
                string filename = $"{logCorrelationId}_request_{clock.Now:yyyyMMddHHmmss}_{Guid.NewGuid()}.txt";
                string requestLogFilePath = Path.Combine(BatchLogsFolder, filename);

                string requestLog = $"Request URL: {request.RequestUri}{Environment.NewLine}";
                requestLog += $"Request Method: {request.Method}{Environment.NewLine}";

                requestLog += $"Request Headers:{Environment.NewLine}";

                foreach (var header in request.Headers)
                {
                    requestLog += $"{header.Key}: {string.Join(", ", header.Value)}{Environment.NewLine}";
                }

                requestLog += $"Content Headers:{Environment.NewLine}";

                foreach (var contentHeader in request.Content.Headers)
                {
                    requestLog += $"{contentHeader.Key}: {string.Join(", ", contentHeader.Value)}{Environment.NewLine}";
                }

                requestLog += Environment.NewLine;
                requestLog += "Request Content:" + Environment.NewLine;
                requestLog += syncConverter.ToSync(() => request.Content.ReadAsStringAsync());

                string folderPath = Path.GetDirectoryName(requestLogFilePath);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                File.WriteAllText(requestLogFilePath, requestLog);

                return (isSuccess: true, null);
            }
            catch (Exception ex)
            {
                return (isSuccess: false, failedMessage: ex.Message);
            }
        }


        private (bool isSuccess, string failedMessage) TryLogResponseToFile(HttpResponseMessage response, string logCorrelationId)
        {
            try
            {
                Directory.CreateDirectory(BatchLogsFolder);

                string filename = $"{logCorrelationId}_response_{clock.Now:yyyyMMddHHmmss}_{Guid.NewGuid()}.txt";
                string responseLogFilePath = Path.Combine(BatchLogsFolder, filename);

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
    }

    public class GetPcrBatchLogsRequest
    {
        [Required]
        public string LogCorrelationId { get; set; }
    }

    public class GetPcrBatchLogsResponse
    {
        public List<GetPcrLogsResponseItem> LogFiles { get; set; }
    }

    public class GetPcrLogsResponseItem
    {
        public bool IsRequestLog { get; set; }
        public bool IsResponseLog { get; set; }
        public DateTime LogDate { get; set; }
        public string LogFileName { get; set; }        
    }
}
