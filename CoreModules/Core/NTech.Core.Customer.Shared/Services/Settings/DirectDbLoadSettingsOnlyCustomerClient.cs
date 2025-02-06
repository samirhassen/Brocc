using nCustomer.Code.Services;
using nCustomer.Code.Services.Settings;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Customer.Shared.Settings;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;

namespace NTech.Core.Customer.Shared.Services.Settings
{
    public class DirectDbLoadSettingsOnlyCustomerClient : ICustomerClientLoadSettingsOnly
    {
        private readonly Func<ICustomerContext> createCustomerContext;
        private readonly ICoreClock coreClock;
        private readonly IClientConfigurationCore clientConfiguration;

        public DirectDbLoadSettingsOnlyCustomerClient(Func<ICustomerContext> createCustomerContext, ICoreClock coreClock, IClientConfigurationCore clientConfiguration)
        {
            this.createCustomerContext = createCustomerContext;
            this.coreClock = coreClock;
            this.clientConfiguration = clientConfiguration;
        }

        public Dictionary<string, string> LoadSettings(string settingCode)
        {
            var keyValueStore = new KeyValueStoreService(createCustomerContext, coreClock);
            var service = new ReadonlySettingsService(new SettingsModelSource(clientConfiguration), keyValueStore, clientConfiguration);
            return service.LoadSettingsValues(settingCode);
        }
    }
}
