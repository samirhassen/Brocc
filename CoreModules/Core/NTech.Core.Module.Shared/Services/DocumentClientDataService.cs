using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.Module.Shared.Services
{
    public class DocumentClientDataService
    {
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly ISharedEnvSettings envSettings;
        private CachedSettingsService settingsService;

        public DocumentClientDataService(ICustomerClientLoadSettingsOnly customerClient, IClientConfigurationCore clientConfiguration, ISharedEnvSettings envSettings)
        {
            this.clientConfiguration = clientConfiguration;
            this.envSettings = envSettings;
            settingsService = new CachedSettingsService(customerClient);
        }

        public Dictionary<string, object> GetCommonContext()
        {
            var enabledForFeatureNames = new string[]
            {
                "ntech.feature.unsecuredloans.standard",
                "ntech.feature.mortgageloans.standard",
                "ntech.feature.unsecuredloans"
            };
            if (!enabledForFeatureNames.Any(clientConfiguration.IsFeatureEnabled))
                return new Dictionary<string, object>();

            var settings = settingsService.LoadSettings("documentClientData", envSettings.IsTemplateCacheDisabled);
            return settings?.Keys?.ToDictionary(x => $"documentClientData_{x}", x => (object)settings[x])
                ?? new Dictionary<string, object>();
        }

        public Dictionary<string, object> ExtendContextWithCommonContext(Dictionary<string, object> context)
        {
            context = context ?? new Dictionary<string, object>();

            var commonContext = GetCommonContext();

            foreach (var item in commonContext)
            {
                if (!context.ContainsKey(item.Key))
                    context[item.Key] = item.Value;
            }

            return context;
        }
    }
}
