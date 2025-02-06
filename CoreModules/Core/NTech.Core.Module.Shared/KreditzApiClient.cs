using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NTech.Banking.CivicRegNumbers;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Core.Module.Shared
{
    public class KreditzApiClient
    {
        private static async Task<(string AccessToken, DateTimeOffset ExpirationDate)?> GetKreditzAccessToken(HttpClient client, string clientId, string clientSecret)
        {
            var tokenResponse = await client.PostAsync("https://vista.kreditz.com/kreditz/api/v4/authorizations/access_token", new StringContent(JsonConvert.SerializeObject(new
            {
                client_id = clientId,
                client_secret = clientSecret
            }), Encoding.UTF8, "application/json"));
            var rawResponse = await tokenResponse.Content.ReadAsStringAsync();
            var parsedResponse = KreditzApiClient.ParseApiResponse<AccessTokenApiDataFormat>(rawResponse);
            if (!parsedResponse.Status)
                throw new NTechCoreWebserviceException("Failed to get kreditz access token: " + parsedResponse.Message);
            
            if (parsedResponse.ParsedData.access_token == null)
                throw new NTechCoreWebserviceException("Failed to get kreditz access token: Got status = true but no token");

            //Time format: 2023-11-07T09:11:11.654Z
            var expirationDate = parsedResponse.ParsedData.access_token_expired_at;
            return (
                AccessToken: parsedResponse.ParsedData.access_token, 
                ExpirationDate: expirationDate == null ? DateTimeOffset.Now.AddMinutes(5) : DateTimeOffset.Parse(expirationDate, CultureInfo.InvariantCulture));
        }

        private class AccessTokenApiDataFormat
        {
            public string access_token { get; set; }
            public string access_token_expired_at { get; set; }
        }

        public static JObject GenerateTestData(string caseId, ICivicRegNumber civicRegNr, int? ltlAmount, int? incomeAmount)
        {
            return JObject.FromObject(new
            {
                case_id = caseId,
                user_info = new
                {
                    ssn = civicRegNr.NormalizedValue
                }
            });
        }

        public static async Task<bool> DeleteCase(HttpClient client, string caseId, string accessToken)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete, $"https://vista.kreditz.com/kreditz/api/v4/bank_data/find_by_case/{caseId}");
            message.Headers.Add("Authorization", $"Bearer {accessToken}");
            var result = await client.SendAsync(message);
            return result.IsSuccessStatusCode;
        }

        public static async Task<(bool HasData, ICivicRegNumber CivicRegNr, JObject RawBankData)> FindByCase(HttpClient client, string caseId, string accessToken, IClientConfigurationCore clientConfig)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, $"https://vista.kreditz.com/kreditz/api/v4/bank_data/find_by_case/{caseId}");
            message.Headers.Add("Authorization", $"Bearer {accessToken}");
            var result = await client.SendAsync(message);
            if (result.StatusCode == System.Net.HttpStatusCode.NotFound)
                return (HasData: false, CivicRegNr: null, RawBankData: null);

            if (!result.IsSuccessStatusCode)
                throw new NTechCoreWebserviceException($"Kreditz find_by_case({caseId}) failed: {result.StatusCode} - {result.ReasonPhrase}");

            var rawContent = await result.Content.ReadAsStringAsync();
            var parsedContent = ParseApiResponse<CaseDataApiFormat>(rawContent);
            if(!parsedContent.Status)
                throw new NTechCoreWebserviceException($"Kreditz find_by_case({caseId}) returned status=false: {parsedContent.Message}");

            if(parsedContent.ParsedData?.user_info?.ssn == null)
                throw new NTechCoreWebserviceException($"Kreditz find_by_case({caseId}) missing ssn");

            if(!new CivicRegNumberParser(clientConfig.Country.BaseCountry).TryParse(parsedContent.ParsedData.user_info.ssn, out var parsedCivicRegNr))
            {
                throw new NTechCoreWebserviceException($"Kreditz find_by_case({caseId}) invalid ssn");
            }

            return (HasData: true, CivicRegNr: parsedCivicRegNr, RawBankData: parsedContent.RawData);
        }

        public static (int? LtlAmount, int? IncomeAmount) ParseScoringVariables(JObject rawBankData)
        {
            var disposableIncome = rawBankData.GetDecimalPropertyValueByPath("alta", "affordability", "disposable_income");
            var totalIncome = rawBankData.GetDecimalPropertyValueByPath("alta", "affordability", "total_income");
            return (
                LtlAmount: disposableIncome.HasValue ? (int)Math.Round(disposableIncome.Value) : new int?(),
                IncomeAmount: totalIncome.HasValue ? (int)Math.Round(totalIncome.Value) : new int?());
        }

        public static async Task<string> GetCachedAccessTokenAsync(HttpClient client, string clientId, string clientSecret, FewItemsCache cache) =>        
            await cache.WithCacheAsync($"339f7504-22a6-4529-80d1-c7be08ef9257_{clientId}", async () =>
            {
                var accessTokenResult = await GetKreditzAccessToken(client, clientId, clientSecret);
                if (!accessTokenResult.HasValue)
                    throw new NTechCoreWebserviceException("Failed to aquire kreditz access token");
                return (
                    Value: accessTokenResult.Value.AccessToken,
                    ExpirationDate: accessTokenResult.Value.ExpirationDate
                );
            });

        private class CaseDataApiFormat
        {
            public string case_id { get; set; }
            public CaseDataApiFormatUserInfo user_info { get; set; }

            public class CaseDataApiFormatUserInfo
            {
                public string ssn { get; set; }
            }
        }

        public static KreditzApiResponse<T> ParseApiResponse<T>(string rawResponse) where T : class
        {
            if (rawResponse == null)
                return new KreditzApiResponse<T>
                {
                    Status = false,
                    Message = "(local)No json data returned"
                };
            try
            {
                var wrapper = JsonConvert.DeserializeAnonymousType(rawResponse, new
                {
                    Status = (bool?)null,
                    Message = (string)null,
                    Data = (JObject)null
                });
                if(wrapper == null || !wrapper.Status.HasValue)
                    return new KreditzApiResponse<T>
                    {
                        Status = false,
                        Message = "(local)No status returned"
                    };

                if(wrapper.Status == false)
                    return new KreditzApiResponse<T>
                    {
                        Status = false,
                        Message = wrapper.Message ?? "(local)Status false but no error message"
                    };

                if (wrapper.Data == null)
                    return new KreditzApiResponse<T>
                    {
                        Status = false,
                        Message = wrapper.Message ?? "(local)Data missing"
                    };

                return new KreditzApiResponse<T>
                {
                    Status = true,
                    Message = wrapper.Message,
                    RawData = wrapper.Data,
                    ParsedData = wrapper.Data.ToObject<T>()
                };
            } 
            catch
            {
                return new KreditzApiResponse<T>
                {
                    Status = false,
                    Message = "(local)Failed to parse returned data"
                };
            }
        }

        public class KreditzApiResponse<T> where T : class
        {
            public bool Status { get; set; }
            public string Message { get; set; }
            public JObject RawData { get; set; }
            public T ParsedData { get; set; }
        }

        public static KreditzSettings GetSettings(INTechEnvironment env)
        {
            var file = env.StaticResourceFile("ntech.kreditz.settingsfile", "kreditz-settings.txt", mustExist: false);
            if (!file.Exists)
                return new KreditzSettings
                {
                    UseMock = true
                };
            var settings = NTechSimpleSettingsCore.ParseSimpleSettingsFile(file.FullName, forceFileExistance: true);
            var useMock = settings.OptBool("usemock");
            if(useMock)
                return new KreditzSettings
                {
                    UseMock = true,
                    MockDataFile = settings.Opt("mock.datafile")
                };
            return new KreditzSettings
            {
                UseMock = false,
                IFrameClientId = settings.Req("clientid.iframe"),
                ApiClientId = settings.Req("clientid.api"),
                ApiClientSecret = settings.Req("clientsecret.api"),
                TestCivicRegNr = env.IsProduction ? null : settings.Opt("civicregnr.test"),
                SkipDelete = settings.OptBool("skipdelete"),
                IsDirectUiEnabled = settings.OptBool("directui.enabled"),
                //NOTE: If using kreditz scoring-engine alta never set this to anything other than 12
                FetchMonthCount = int.Parse(settings.Opt("fetch.monthcount") ?? "12")
            };
        }

        public const string DataSharingProviderName = "kreditz";

        public class KreditzSettings
        {
            public bool UseMock { get; set; }
            public string IFrameClientId { get; set; }
            public string ApiClientId { get; set; }
            public string ApiClientSecret { get; set; }
            public string TestCivicRegNr { get; set; }
            public bool SkipDelete { get; set; }
            public bool IsDirectUiEnabled { get; set; }
            public string MockDataFile { get; set; }
            public int FetchMonthCount { get; set; }
        }
    }
}
