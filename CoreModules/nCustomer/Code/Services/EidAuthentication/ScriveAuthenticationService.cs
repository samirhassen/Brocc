using Newtonsoft.Json;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.CivicRegNumbers.Se;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.ElectronicAuthentication;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace nCustomer.Code.Services.EidAuthentication
{
    public class ScriveAuthenticationService : IEidAuthenticationService
    {
        private readonly NTechSimpleSettings settings;
        private AuthenticationSessionService authenticationSessionService;

        public ScriveAuthenticationService(NTechSimpleSettings settings, ICombinedClock clock)
        {
            authenticationSessionService = new AuthenticationSessionService(clock);
            this.settings = settings;
        }

        public static string ProviderName = "scrive";

        public CommonElectronicAuthenticationSession CreateSession(ICivicRegNumber civicRegNumber, ReturnUrlModel returnUrl, NtechCurrentUserMetadata currentUser, Dictionary<string, string> customData)
        {
            return authenticationSessionService.CreateSession(civicRegNumber, currentUser, customData, ProviderName, x =>
            {
                var newTransactionResult = Task.Run(() => NewTransaction(civicRegNumber, returnUrl.GetReturnUrl(x))).GetAwaiter().GetResult();
                x.SetCustomData("authenticationMechanismName", newTransactionResult.AuthenticationMechanismName);
                return (ProviderSessionId: newTransactionResult.Transaction.id, BeginLoginRedirectUrl: newTransactionResult.Transaction.accessUrl);
            });
        }

        public (CommonElectronicAuthenticationSession Session, bool WasAuthenticated) HandleProviderLoginEvent(string localSessionId, NtechCurrentUserMetadata currentUser, Dictionary<string, string> providerEventData)
        {
            return authenticationSessionService.HandleProviderEvent(localSessionId, currentUser, localSession =>
            {
                var transaction = Task.Run(() => GetTransaction(localSession.ProviderSessionId)).GetAwaiter().GetResult();
                if (transaction == null)
                    return new AuthenticationSessionService.ProviderAuthenticationResult { FailedMessage = "Session missing from provider" };

                //scrive statues: "new", "started", "failed""complete"
                if (transaction.status == "complete")
                {
                    var authenticationMechanismName = localSession.CustomData.Opt("authenticationMechanismName");
                    if (authenticationMechanismName == GetProviderNameByCivicRegNrCountry("SE"))
                    {
                        var data = transaction?.providerInfo?.seBankID?.completionData;
                        var user = new CommonElectronicAuthenticationSession.AuthenticatedUserModel
                        {
                            IpAddress = data?.device?.ipAddress,
                            CivicRegNumber = CivicRegNumberSe.Parse(data?.user?.personalNumber).NormalizedValue,
                            FirstName = data?.user?.givenName,
                            LastName = data?.user?.surname,
                            FullName = data?.user?.name
                        };
                        return new AuthenticationSessionService.ProviderAuthenticationResult { AuthenticatedAsUser = user };
                    }
                    else
                        throw new NotImplementedException();
                }
                else
                    return new AuthenticationSessionService.ProviderAuthenticationResult { FailedMessage = transaction.status };
            });
        }

        private async Task<TResult> PostOrGet<TResult>(string relativeUrl, object requestData, HttpMethod method, Action<string> observeRawResponse = null)
        {
            var client = new HttpClient();
            client.BaseAddress = ScriveBaseUrl;
            var request = new HttpRequestMessage(method, relativeUrl);
            request.Headers.Add("Authorization", $"Bearer {BearerToken}");

            string rawRequest = null;
            if (method == HttpMethod.Post)
            {
                rawRequest = JsonConvert.SerializeObject(requestData);
                request.Content = new StringContent(JsonConvert.SerializeObject(requestData), System.Text.Encoding.UTF8, "application/json");
            }

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var rawResponse = await response.Content.ReadAsStringAsync();
            observeRawResponse?.Invoke(rawResponse);
            return JsonConvert.DeserializeObject<TResult>(rawResponse);
        }

        private string GetProviderNameByCivicRegNrCountry(string country)
        {
            if (country == "SE")
                return "seBankID";
            else
                throw new NotImplementedException();
        }

        private string GetUiLocaleByCivicRegNrCountry(string country)
        {
            if (country == "SE")
                return "sv";
            else
                throw new NotImplementedException();
        }

        private async Task<(NewTransactionResult Transaction, string AuthenticationMechanismName)> NewTransaction(ICivicRegNumber civicRegNumber, Uri redirectAfterUrl)
        {
            var authenticationMechanismName = GetProviderNameByCivicRegNrCountry(civicRegNumber.Country);
            var transaction = await PostOrGet<NewTransactionResult>("api/v1/transaction/new", new
            {
                method = "auth",
                provider = authenticationMechanismName,
                redirectUrl = redirectAfterUrl,
                providerParameters = new
                {
                    auth = new
                    {
                        seBankID = new
                        {
                            uiLocale = GetUiLocaleByCivicRegNrCountry(civicRegNumber.Country),
                            personalNumber = civicRegNumber.NormalizedValue,
                            requireAutoStartToken = true
                        }
                    }
                }
            }, HttpMethod.Post);

            return (Transaction: transaction, AuthenticationMechanismName: authenticationMechanismName);
        }
        private class NewTransactionResult
        {
            public string id { get; set; }
            public string accessUrl { get; set; }
        }

        private async Task<GetTransactionResult> GetTransaction(string id, Action<string> observeRawResponse = null)
        {
            return await PostOrGet<GetTransactionResult>($"api/v1/transaction/{id}", null, HttpMethod.Get, observeRawResponse: observeRawResponse);
        }
        private class GetTransactionResult
        {
            public string status { get; set; } //"new""started""failed""complete"
            public ProviderInfoModel providerInfo { get; set; }
            public class ProviderInfoModel
            {
                public ProviderInfoBankIdSeModel seBankID { get; set; }
            }
            public class ProviderInfoBankIdSeModel
            {
                public ProviderInfoBankIdSeCompletionDataModel completionData { get; set; }
            }
            public class ProviderInfoBankIdSeCompletionDataModel
            {
                public BankIdSeUserModel user { get; set; }
                public BankIdSeDeviceModel device { get; set; }
            }
            public class BankIdSeUserModel
            {
                public string personalNumber { get; set; }
                public string name { get; set; }
                public string givenName { get; set; }
                public string surname { get; set; }
            }
            public class BankIdSeDeviceModel
            {
                public string ipAddress { get; set; }
            }
        }

        private Uri ScriveBaseUrl => new Uri(settings.Req("scriveBaseUrl"));
        private string BearerToken => settings.Req("bearerToken");
    }
}