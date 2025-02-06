using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.OrganisationNumbers;
using NTech.Banking.PluginApis;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Plugins
{
    public abstract class ApplicationPluginContextBase
    {
        protected readonly ICoreClock clock;
        protected readonly ICustomerClient customerClient;
        protected readonly ISharedWorkflowService workflowService;
        protected readonly IKeyValueStoreService keyValueStoreService;
        private readonly ApplicationDataSourceService applicationDataSourceService;

        public ApplicationPluginContextBase(
            ICoreClock clock,
            ICustomerClient customerClient,
            ISharedWorkflowService workflowService,
            IKeyValueStoreService keyValueStoreService,
            ApplicationDataSourceService applicationDataSourceService)
        {
            this.clock = clock;
            this.customerClient = customerClient;
            this.workflowService = workflowService;
            this.keyValueStoreService = keyValueStoreService;
            this.applicationDataSourceService = applicationDataSourceService;
        }

        public DateTimeOffset Now => clock.Now;
        public int WorkflowVersion => workflowService.Version;

        public int CreateOrUpdateCompany(IOrganisationNumber orgnr, Dictionary<string, string> customerData, bool isTrustedSource, string applicationNr, int? expectedCustomerId = null)
        {
            return customerClient.CreateOrUpdateCompany(new CreateOrUpdateCompanyRequest
            {
                CompanyName = customerData.Opt("companyName"),
                EventSourceId = applicationNr,
                EventType = "createApplication",
                ExpectedCustomerId = expectedCustomerId,
                Orgnr = orgnr.NormalizedValue,
                Properties = customerData.Select(x => new CreateOrUpdateCompanyRequest.Property
                {
                    Name = x.Key,
                    Value = x.Value,
                    ForceUpdate = isTrustedSource
                }).ToList()
            });
        }

        public int CreateOrUpdatePerson(ICivicRegNumber civicRegNr, Dictionary<string, string> customerData, bool isTrustedSource, string applicationNr, int? expectedCustomerId = null, DateTime? birthDate = null)
        {
            return customerClient.CreateOrUpdatePerson(new CreateOrUpdatePersonRequest
            {
                EventSourceId = applicationNr,
                EventType = "createApplication",
                ExpectedCustomerId = expectedCustomerId,
                CivicRegNr = civicRegNr.NormalizedValue,
                BirthDate = birthDate,
                Properties = customerData.Where(x => !string.IsNullOrWhiteSpace(x.Value)).Select(x => new CreateOrUpdatePersonRequest.Property
                {
                    Name = x.Key,
                    Value = x.Value,
                    ForceUpdate = isTrustedSource
                }).ToList()
            });
        }

        public string SerializeObject<T>(T value)
        {
            return JsonConvert.SerializeObject(value);
        }

        public void SetKeyValueStoreValue(string key, string keySpace, string value, Action<bool> observeWasUpdated = null)
        {
            keyValueStoreService.SetValue(key, keySpace, value);
        }

        public IApplicationDataSourceResponse GetDataSourceItems(string applicationNr, Dictionary<string, HashSet<string>> itemNamesByDataSourceName)
        {
            var result = applicationDataSourceService.GetDataSimple(applicationNr, itemNamesByDataSourceName);

            return new ApplicationDataSourceResponse(result);
        }

        private class ApplicationDataSourceResponse : IApplicationDataSourceResponse
        {
            private readonly ApplicationDataSourceResult r;

            public ApplicationDataSourceResponse(ApplicationDataSourceResult r)
            {
                this.r = r;
            }

            public T DeserializeJsonValue<T>(string value)
            {
                return JsonConvert.DeserializeObject<T>(value);
            }

            public List<string> ItemNames(string dataSourceName)
            {
                return r.ItemNames(dataSourceName);
            }

            public string Opt(string datasourceName, string itemName)
            {
                return r.Item(datasourceName, itemName).StringValue.Optional;
            }

            public string Req(string datasourceName, string itemName)
            {
                return r.Item(datasourceName, itemName).StringValue.Required;
            }
        }
    }
}