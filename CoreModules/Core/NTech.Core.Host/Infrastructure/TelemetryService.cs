using Azure.Storage.Blobs.Models;
using Newtonsoft.Json;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NTech.Core.Module.Infrastrucutre
{
    public class TelemetryService
    {
        private readonly NEnv env;
        private readonly IClientConfigurationCore clientConfiguration;

        public TelemetryService(NEnv env, IClientConfigurationCore clientConfiguration)
        {
            this.env = env;
            this.clientConfiguration = clientConfiguration;
        }

        public async Task LogTelemetryDataAsync(LogTelemetryDataRequest request)
        {
            await SendTelemetryDataAsync(request.DatasetName, JsonConvert.DeserializeObject(request.BatchAsJson));
        }

        private async Task SendTelemetryDataAsync<T>(string datasetName, T data)
        {
            var now = DateTimeOffset.Now;
            var guid = Guid.NewGuid().ToString();
            var environmentName = TelemetryEnvironmentName;

            var tempFileData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
            {
                date = now,
                datasetName = datasetName,
                environmentName = TelemetryEnvironmentName,
                data = data
            }));

            var actualFileName = $"{datasetName}-Telemetry-{now.ToString("yyyyMMddHHmmss")}-{guid}.txt";

            if (IsTelemetryLocalLoggingEnabled)
            {
                Action logToDisk = () =>
                {
                    var logFolder = env.LogFolder;
                    if (logFolder == null)
                    {
                        return;
                    }

                    var folder = Path.Combine(logFolder.FullName, @"Telemetry\Aggregate\" + datasetName);
                    Directory.CreateDirectory(folder);

                    System.IO.File.WriteAllBytes(Path.Combine(folder, actualFileName), tempFileData);
                };
                logToDisk();
            }
            if (IsTelemetryRemoteLoggingEnabled)
            {
                var container = new Azure.Storage.Blobs.BlobContainerClient(TelemetryAzureSasUrl);
                var blobClient = container.GetBlobClient($"{datasetName}/{actualFileName}");
                var options = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders 
                    {
                        ContentType = "text/plain"
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        { "ntechenvironment", environmentName },
                        { "ntechdate",  DateTimeOffset.Now.ToString("o") },
                        { "ntechtype", datasetName }
                    }
                };
                await blobClient.UploadAsync(BinaryData.FromBytes(tempFileData), options);
            }
        }

        private Uri GetOptionalTelemetryAzureSasUrl()
        {
            var fi =  env.StaticResourceFile("ntech.telemetry.azuresasurlfile", "telemetrySasToken.txt", false);
            if (!fi.Exists)
                return null;
            return new Uri(File.ReadAllText(fi.FullName));
        }

        private Uri TelemetryAzureSasUrl
        {
            get
            {
                var uri = GetOptionalTelemetryAzureSasUrl();
                return uri;
            }
        }

        private string TelemetryEnvironmentName
        {
            get
            {
                var s = env.OptionalSetting("ntech.telemetry.environmentname");
                if (s != null)
                    return s;
                else if (env.IsProduction)
                    return "prod";
                else
                    return "dev";
            }
        }

        private bool IsStandard => clientConfiguration.IsFeatureEnabled("ntech.feature.unsecuredloans.standard") || clientConfiguration.IsFeatureEnabled("ntech.feature.mortgageloans.standard");

        private bool IsTelemetryRemoteLoggingEnabled
        {
            get
            {
                var m = TelemetryLoggingMode ?? "";
                if (string.IsNullOrWhiteSpace(m) && IsStandard && TelemetryAzureSasUrl != null)
                {
                    //For standard we interpret the presence of the Telemetry token as opting into remote to make the setup easier.
                    //They can still disable by setting it to something like local
                    m = "remote";
                }
                return m == "remote" || m == "localandremote";
            }
        }

        private bool IsTelemetryLocalLoggingEnabled
        {
            get
            {
                var m = TelemetryLoggingMode ?? "";
                return m == "local" || m == "localandremote";
            }
        }

        private string TelemetryLoggingMode
        {
            get
            {
                return env.OptionalSetting("ntech.telemetry.loggingmode");
            }
        }

    }

    public class LogTelemetryDataRequest
    {
        [Required]

        public string DatasetName { get; set; }
        [Required]
        public string BatchAsJson { get; set; }
    }
}
