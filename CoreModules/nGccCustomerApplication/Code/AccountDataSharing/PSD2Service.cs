using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Web;
using System.Collections.Concurrent;
using NTech.Services.Infrastructure;

namespace nGccCustomerApplication.Controllers
{
    public class PSD2Service
    {
        private static readonly ConcurrentDictionary<string, ReturnTokens> activeSessions = new ConcurrentDictionary<string, ReturnTokens>();

        public PSD2Service()
        {
            this.settings = new Lazy<PSD2Settings>(() =>
            {
                if (!IsThereAPsd2SettingFileThatExists)
                    return null;

                string Req(string name) => GetPsd2Setting(name, true);

                return new PSD2Settings
                {
                    AppId = Req("appId"),
                    AppKey = Req("appKey"),
                    ServiceName = Req("serviceName"),
                    ServiceLenderName = Req("serviceLenderName"),
                    BasisCompanyName = Req("basisCompanyName"),
                    BasisPurposeCode = Req("basisPurposeCode"),
                    BasisLocale = Req("basisLocale"),
                    RawLogFolder = GetPsd2Setting("rawLogFolder", isRequired: false)
                };
            });
        }

        private PSD2Settings Settings => settings.Value;

        private ReturnTokens ActiveSession(string sessionKey)
        {
            if (!activeSessions.TryGetValue(sessionKey, out var tokens))
                throw new Exception($"Missing session {sessionKey}");
            return tokens;
        }

        public Uri GetRedirectCustomerUrl(string sessionKey) => NTechServiceRegistry.CreateUrl(GetRedirectBaseUrl(), $"psd2-client/session/{ActiveSession(sessionKey).Nonce}");
        private Uri GetCreateSessionUrl() => NTechServiceRegistry.CreateUrl(GetBankAccountAggregationsBaseUrl(), "/");

        public async Task<Uri> StartSession(string sessionKey, Uri successUrl, Uri errorUrl, Uri calculationCallbackUrl)
        {
            var bankAccountData = new BankAccountModel
            {
                Service = new Service
                {
                    Name = Settings.ServiceName,
                    LenderName = Settings.ServiceLenderName,
                    NoRawData = false
                },
                Integration = new Integration
                {
                    AccountDataCallback = null,
                    CalculationResultCallback = calculationCallbackUrl.ToString(),
                    EndUserRedirectSuccess = successUrl.ToString(),
                    EndUserRedirectError = errorUrl.ToString(),
                },
                Basis = new Basis
                {
                    CompanyName = Settings.BasisCompanyName,
                    PurposeCode = Settings.BasisPurposeCode,
                    Locale = Settings.BasisLocale,
                    RequestId = sessionKey
                }
            };
            var client = new HttpClient();
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = GetCreateSessionUrl(),
                Headers = {
                { "app-id", Settings.AppId },
                { "app-key", Settings.AppKey },
                { HttpRequestHeader.Accept.ToString(), "application/json" },
                { HttpRequestHeader.ContentType.ToString(), "application/json" }
            },
                Content = new StringContent(JsonConvert.SerializeObject(bankAccountData), Encoding.UTF8, "application/json")
            };
            var response = await client.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            var returnTokens = JsonConvert.DeserializeObject<ReturnTokens>(responseContent);

            activeSessions[sessionKey] = returnTokens;

            return NTechServiceRegistry.CreateUrl(GetRedirectBaseUrl(), $"psd2-client/session/{ActiveSession(sessionKey).Nonce}");
        }

        private async Task<byte[]> ReadStreamToEndAsync(Stream stream)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = (await stream.ReadAsync(buffer, 0, buffer.Length))) > 0)
                {
                    await ms.WriteAsync(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        public async Task<(string RuleEngineData, byte[] DataPdf, byte[] RulePdf)> HandleCalculationCallback(string sessionKey, HttpRequestBase request, bool downloadDataPdf, bool downloadRulePdf, string sourceContext)
        {
            byte[] bodyContent = null;
            if(request.ContentLength > 0)
            {
                bodyContent = await ReadStreamToEndAsync(request.InputStream);
            }

            var session = ActiveSession(sessionKey);

            LogEvent(sessionKey, sourceContext, 
                $"calculation callback received:" + Environment.NewLine + string.Join(Environment.NewLine, request.Headers.AllKeys.Select(x => $"{x}={request.Headers[x]}")));
            if (bodyContent != null)
                LogRawData(sessionKey, sourceContext, bodyContent, "calculation-callback");

            if (request.ContentType?.ToLower()?.Contains("json") != true)
                throw new Exception($"Non json calculation callback received for sat psd2 session {sessionKey}");

            var body = Encoding.UTF8.GetString(bodyContent);

            byte[] dataPdf = null;
            byte[] rulePdf = null;
            if (downloadDataPdf) dataPdf = await DownloadDataPdf(body, sessionKey, sourceContext);
            if (downloadRulePdf) rulePdf = await DownloadRulePdf(body, sessionKey, sourceContext);

            return (RuleEngineData: body, DataPdf: dataPdf, RulePdf: rulePdf);
        }

        public void LogEvent(string sessionToken, string sourceContext, string logText)
        {
            if (settings.Value.RawLogFolder == null)
                return;
            var fileName = Path.Combine(settings.Value.RawLogFolder, $"{sessionToken}-{sourceContext}-events.txt");
            File.AppendAllText(fileName, $"Event: {DateTimeOffset.Now.ToString("o")}" + Environment.NewLine + logText + Environment.NewLine + Environment.NewLine);
        }

        public void LogRawData(string sessionToken, string sourceContext, byte[] data, string dataSuffix)
        {
            if (settings.Value.RawLogFolder == null)
                return;
            
            var fileName = Path.Combine(settings.Value.RawLogFolder, $"{sessionToken}-{sourceContext}-{dataSuffix}.dat");
            File.WriteAllBytes(fileName, data);
            
        }

        private string ExtractRawDataAndRenameToAccountData(string callbackBodyContent)
        {
            /*
             * So for some utterly insane reason SAT cant parse this themselves even though they created and returned it so you need to extract ruleResponse.rawData when creating the rule pdf
             Basically we change { ruleResponse: { rawData: <X>, anyOther: ..., ... }, ...} to { accountData: <X> }
             */
            var parsedContent = JObject.Parse(callbackBodyContent);

            JObject ruleBody = (JObject)parsedContent.SelectToken("ruleResponse");
            foreach (var child in ruleBody.Children().OfType<JProperty>().ToList())
            {
                if (child.Name == "rawData")
                {
                    ruleBody.Add(new JProperty("accountData", child.Value));
                    child.Remove();
                }
                else
                    child.Remove();

                if (child.Name == "processingDate" || child.Name == "processingdate")
                    child.Remove();
            }

            /*
             From SAT ... it seems we need to append this for rule pdf creation to work. SAT response below:

            This is due to an undocumented requirement
            so Naktergal could not know this (I dint know this).
            You need to append "processingdate":
            "YYYY-MM-DD" after the accountdata
            element for the pdf creation to complete.

            - in their actual example the spelling is processingDate.
            - also removing it if it exists since they may add code to autoinclude this so everyone else doesnt also get screwed by this.
             
             */
            ruleBody.Add(new JProperty("processingDate", DateTimeOffset.UtcNow.ToString("yyyy-MM-dd")));

            return parsedContent.SelectToken("ruleResponse").ToString();
        }

        private Task<byte[]> DownloadDataPdf(string callbackBodyContent, string sessionKey, string sourceContext) => DownloadPdf(callbackBodyContent, sessionKey, false, sourceContext);
        private Task<byte[]> DownloadRulePdf(string callbackBodyContent, string sessionKey, string sourceContext) => DownloadPdf(callbackBodyContent, sessionKey, true, sourceContext);
        private async Task<byte[]> DownloadPdf(string callbackBodyContent, string sessionKey, bool isRulePdf, string sourceContext)
        {
            var session = ActiveSession(sessionKey);

            var client = new HttpClient();            
            callbackBodyContent = ExtractRawDataAndRenameToAccountData(callbackBodyContent);            
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = NTechServiceRegistry.CreateUrl(GetBankAccountAggregationsBaseUrl(), $"{(isRulePdf ? "createRulePDF" : "createAccountDataPDF")}/{session.SessionToken}"),
                Headers = {
                    { "app-id", Settings.AppId },
                    { "app-key", Settings.AppKey },
                    { HttpRequestHeader.Accept.ToString(), "application/pdf" }
                    },
                Content = new StringContent(callbackBodyContent, Encoding.UTF8, "application/json")
            };
            var response = await client.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
            Stream stream = await response.Content.ReadAsStreamAsync();
            return await ReadStreamToEndAsync(stream);
        }

        private enum SatEnvironmentCode
        {
            Prod,
            Test
        }

        private SatEnvironmentCode SatEnvironment
        {
            get
            {
                var satEnvironmentCode = (GetPsd2Setting("satEnvironmentCode", true) ?? "").Trim().ToLowerInvariant();
                if (satEnvironmentCode == "prod")
                    return SatEnvironmentCode.Prod;
                else if (satEnvironmentCode == "test")
                    return SatEnvironmentCode.Test;
                else
                    throw new Exception("satEnvironmentCode must be prod or test");
            }
        }

        private Uri GetBankAccountAggregationsBaseUrl()
        {
            switch (SatEnvironment)
            {
                case SatEnvironmentCode.Test: return new Uri("https://test.asiakastieto.fi/services/psd2-api/bankAccountsAggregations");
                case SatEnvironmentCode.Prod: return new Uri("https://api.asiakastieto.fi/PSD2/bankAccountsAggregations");
                default: throw new NotImplementedException();
            }
        }

        private Uri GetRedirectBaseUrl()
        {
            switch (SatEnvironment)
            {
                case SatEnvironmentCode.Test: return new Uri("https://test.asiakastieto.fi");
                case SatEnvironmentCode.Prod: return new Uri("https://www.asiakastieto.fi");
                default: throw new NotImplementedException();
            }
        }

        private Lazy<PSD2Settings> settings;

        private Lazy<NTechSimpleSettings> psd2Settings = new Lazy<NTechSimpleSettings>(() =>
        {
            var file = NTechEnvironment.Instance.StaticResourceFile("ntech.psd2.settingsfile", "psd2-settings.txt", false);
            if (!file.Exists)
                return null;
            return NTechSimpleSettings.ParseSimpleSettingsFile(file.FullName, forceFileExistance: true);
        });

        public bool IsThereAPsd2SettingFileThatExists => psd2Settings.Value != null;

        public string GetPsd2Setting(string name, bool isRequired)
        {
            var settings = psd2Settings.Value;
            if (settings == null && isRequired)
                throw new Exception("PSD2 settings file missing");
            return isRequired ? settings.Req(name) : settings?.Opt(name);
        }

        public Uri CreateExternalSelfUrl(string relativeUrl, params Tuple<string, string>[] queryStringParameters) =>
            NEnv.ServiceRegistry.External.ServiceUrl("nGccCustomerApplication", relativeUrl);

        private class DocumentSourceRequest
        {
            public string Token { get; set; }
            public int ApplicantNr { get; set; }

            public PSD2Session Psd2Session { get; set; }
        }

        private class PSD2Session
        {
            public string SessionKey { get; set; }
            public string SessionToken { get; set; }
            public int ApplicantNr { get; set; }

            public string ApplicationNr { get; set; }
            public string ExternalUrlToIndex { get; set; }
        }

        private class ReturnTokens
        {
            [JsonProperty("nonce")]
            public string Nonce { get; set; }
            [JsonProperty("sessionToken")]
            public string SessionToken { get; set; }
        }

        private class BankAccountModel
        {
            [JsonProperty("service")]
            public Service Service { get; set; }

            [JsonProperty("integration")]
            public Integration Integration { get; set; }
            [JsonProperty("basis")]
            public Basis Basis { get; set; }
        }

        private class Service
        {
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("lenderName")]
            public string LenderName { get; set; }
            [JsonProperty("noRawData")]
            public bool NoRawData { get; set; }
        }

        private class Integration
        {
            [JsonProperty("accountDataCallback")]
            public string AccountDataCallback { get; set; }
            [JsonProperty("calculationResultCallback")]
            public string CalculationResultCallback { get; set; }
            [JsonProperty("endUserRedirectSuccess")]
            public string EndUserRedirectSuccess { get; set; }
            [JsonProperty("endUserRedirectError")]
            public string EndUserRedirectError { get; set; }
        }

        private class Basis
        {
            [JsonProperty("requestId")]
            public string RequestId { get; set; }
            [JsonProperty("companyName")]
            public string CompanyName { get; set; }
            [JsonProperty("locale")]
            public string Locale { get; set; }
            [JsonProperty("purposeCode")]
            public string PurposeCode { get; set; }

        }

        private class PSD2Settings
        {
            public bool IsEnabled { get; set; }
            public string AppId { get; set; }
            public string AppKey { get; set; }
            public string ServiceName { get; set; }
            public string ServiceLenderName { get; set; }
            public string BasisCompanyName { get; set; }
            public string BasisPurposeCode { get; set; }
            public string BasisLocale { get; set; }
            public string RawLogFolder { get; set; }
        }
    }
}