using NTech.Core.Module.Shared;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace NTech.Core.PreCredit.Shared.Services
{
    public class BankShareTestService
    {
        private readonly INTechEnvironment environment;
        private readonly IClientConfigurationCore clientConfiguration;
        private static Lazy<FewItemsCache> cache = new Lazy<FewItemsCache>();

        public BankShareTestService(INTechEnvironment environment, IClientConfigurationCore clientConfiguration)
        {
            this.environment = environment;
            this.clientConfiguration = clientConfiguration;
        }

        public BankShareTestSettingsResponse GetSettings(BankShareTestSettingsRequest request)
        {
            var settings = KreditzApiClient.GetSettings(environment);
            if(settings.UseMock || !settings.IsDirectUiEnabled)
                return new BankShareTestSettingsResponse
                {
                    IsEnabled = false
                };

            return new BankShareTestSettingsResponse
            {
                IsEnabled = true,
                KreditzIFrameClientId = settings.IFrameClientId,
                ProviderName = KreditzApiClient.DataSharingProviderName
            };
        }

        public async Task<BankShareTestPollingResponse> Poll(BankShareTestPollingRequest request)
        {
            if(request.ProviderName == KreditzApiClient.DataSharingProviderName)
                return await PollKreditz(request);
            else
                throw new NTechCoreWebserviceException("Unsupported provider") { IsUserFacing = true, ErrorHttpStatusCode = 400 };
        }

        private async Task<BankShareTestPollingResponse> PollKreditz(BankShareTestPollingRequest request)
        {
            var settings = KreditzApiClient.GetSettings(environment);
            if (settings.UseMock || !settings.IsDirectUiEnabled)
                throw new NTechCoreWebserviceException("Disabled") { IsUserFacing = true, ErrorHttpStatusCode = 400 };
            var client = new HttpClient();
            var accessToken = await KreditzApiClient.GetCachedAccessTokenAsync(client, settings.ApiClientId, settings.ApiClientSecret, cache.Value);
            var caseResult = await KreditzApiClient.FindByCase(client, request.Id, accessToken, clientConfiguration);

            if (!caseResult.HasData)
                return new BankShareTestPollingResponse
                {
                    HasData = false
                };

            await KreditzApiClient.DeleteCase(client, request.Id, accessToken);

            var scoringVariables = KreditzApiClient.ParseScoringVariables(caseResult.RawBankData);
            var parsedData = new Dictionary<string, string>();
            if (scoringVariables.IncomeAmount.HasValue)
                parsedData["Left to live on"] = scoringVariables.LtlAmount.Value.ToString(CultureInfo.InvariantCulture);
            if (scoringVariables.IncomeAmount.HasValue)
                parsedData["Income"] = scoringVariables.IncomeAmount.Value.ToString(CultureInfo.InvariantCulture);
            return new BankShareTestPollingResponse
            {
                HasData = true,
                RawJsonData = caseResult.RawBankData.ToString(Newtonsoft.Json.Formatting.None),
                ParsedData = parsedData
            };
        }
    }

    public class BankShareTestSettingsRequest
    {

    }

    public class BankShareTestSettingsResponse
    {
        public bool IsEnabled { get; set; }
        public string ProviderName { get; set; }
        public string KreditzIFrameClientId { get; set; }
    }

    public class BankShareTestPollingRequest
    {
        [Required]
        public string Id { get; set; }

        [Required]
        public string ProviderName { get; set; }
    }

    public class BankShareTestPollingResponse
    {
        public bool HasData { get; set; }
        public string RawJsonData { get; set; }
        public Dictionary<string, string> ParsedData { get; set; }
    }
}
