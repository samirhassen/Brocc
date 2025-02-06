using NTech.Core.PreCredit.Shared.Services;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace nPreCredit.Code.Services
{
    public class AdServiceIntegrationService : IAdServiceIntegrationService
    {
        private readonly AdServicesSettings settings;
        private readonly ISet<string> usedExternalVariableNames;
        private readonly Lazy<HttpClient> httpClient;

        public class AdServicesSettings
        {
            public string CampaignId { get; set; }
            public bool IsEnabled { get; set; }
            public Uri EndpointUrl { get; set; }
        }

        public AdServiceIntegrationService(AdServicesSettings settings)
        {
            this.settings = settings;
            this.usedExternalVariableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!settings.IsEnabled)
                return;

            this.usedExternalVariableNames.Add(CoIdName);
            this.usedExternalVariableNames.Add(OrderIdName);
            this.usedExternalVariableNames.Add(PriceVariableName);
            this.httpClient = new Lazy<HttpClient>(() =>
            {
                var c = new HttpClient();
                return c;
            });
        }

        private const string CoIdName = "coid";
        private const string OrderIdName = "order_id";
        private const string PriceVariableName = "pricevariable";

        public bool IsEnabled => settings.IsEnabled;

        public ISet<string> GetUsedExternalVariableNames()
        {
            return this.usedExternalVariableNames;
        }

        public void ReportConversion(IDictionary<string, string> externalVariables, string orderId, string priceVariable)
        {
            var url = NTechServiceRegistry.CreateUrl(
                this.settings.EndpointUrl,
                "",
                Tuple.Create("camp_id", this.settings.CampaignId),
                Tuple.Create(CoIdName, externalVariables?.Opt(CoIdName)),
                Tuple.Create(OrderIdName, orderId),
                Tuple.Create(PriceVariableName, priceVariable));
            var response = this.httpClient.Value.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();
        }
    }
}