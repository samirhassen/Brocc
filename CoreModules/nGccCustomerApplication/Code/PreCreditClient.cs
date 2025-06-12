using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace nGccCustomerApplication.Code
{
    public class PreCreditClient
    {
        private Tuple<bool, TResponse> Call<TResponse>(string uri, object input, string bearerToken = null) where TResponse : class
        {
            Func<Tuple<bool, TResponse>> fail = () =>
            {
                return Tuple.Create<bool, TResponse>(false, null);
            };
            try
            {
                if (bearerToken == null)
                    bearerToken = AccessToken.GetToken();

                var client = new HttpClient();
                client.BaseAddress = new Uri(NEnv.ServiceRegistry.Internal["nPreCredit"]);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.SetBearerToken(bearerToken);
                var response = client.PostAsJsonAsync(uri, input).Result;
                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<TResponse>(response.Content.ReadAsStringAsync().Result);

                    return Tuple.Create<bool, TResponse>(true, result);
                }
                else
                {
                    NLog.Warning("Failed {failedMessage}", response.ReasonPhrase);
                    return fail();
                }
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Error");
                return fail();
            }
        }

        public Tuple<bool, ApiResult> CreateCreditApplication(CreditApplicationRequest request, string bearerToken = null, bool? disableAutomation = null)
        {
            return Call<ApiResult>("api/creditapplication/create", new { request = request, disableAutomation = disableAutomation }, bearerToken: bearerToken);
        }

        public bool TryExternalProviderEvent(ExternalProviderEventRequest request, string bearerToken = null)
        {
            //ApiResult has no meaning here. There is no result
            var result = Call<ApiResult>("api/creditapplication/external-provider-event", request, bearerToken: bearerToken);
            return result.Item1;
        }

        private static NTechSelfRefreshingBearerToken AccessToken
        {
            get
            {
                return NTechCache.WithCache("d5e15892-db0e-46ee-9e0f-cf3952729ecb", TimeSpan.FromMinutes(5), () =>
                {
                    var credentials = NEnv.CreateApplicationCredentials;
                    return NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, credentials.Item1, credentials.Item2);
                });
            }
        }

        public string PassThroughPostRequest(string url, string request)
        {
            return NHttp
                .Begin(NEnv.ServiceRegistry.Internal.ServiceRootUri("nPreCredit"), AccessToken.GetToken())
                .PostJsonRaw(url, request)
                .ParseAsRawJson();
        }

        public class ExternalProviderEventRequest
        {
            public string ProviderApplicationId { get; set; }
            public string ProviderName { get; set; }
            public string EventName { get; set; }
        }

        public class ApiResult
        {
            public string ApplicationNr { get; set; }
        }

        public class CreditApplicationRequest
        {
            public string UserLanguage { get; set; }
            public int NrOfApplicants { get; set; }
            public string RequestIpAddress { get; set; }
            public Item[] Items { get; set; }
            public class Item
            {
                public string Group { get; set; }
                public string Name { get; set; }
                public string Value { get; set; }
            }
            public string ProviderName { get; set; }
            public List<ExternalVariableItem> ExternalVariables { get; set; }
            public class ExternalVariableItem
            {
                public string Name { get; set; }
                public string Value { get; set; }
            }
        }

        public class AnswersModel
        {
            public Applicant Applicant1 { get; set; }
            public Applicant Applicant2 { get; set; }

            public class Applicant
            {
                public string ConsentRawJson { get; set; }
            }

            public string Iban { get; set; }
        }
    }
}