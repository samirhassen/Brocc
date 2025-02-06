using nPreCredit.Code.Datasources;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class ApplicationDataSourceService
    {
        private readonly Dictionary<string, IApplicationDataSource> dataSources = new Dictionary<string, IApplicationDataSource>();

        public ApplicationDataSourceService(Dictionary<string, IApplicationDataSource> dataSources)
        {
            this.dataSources = dataSources;
        }

        public static ApplicationDataSourceService Create(ICreditApplicationCustomEditableFieldsService creditApplicationCustomEditableFieldsService,
            IPreCreditContextFactoryService preCreditContextFactoryService, EncryptionService encryptionService, ApplicationInfoService applicationInfoService,
            ICustomerClient customerClient) =>
            new ApplicationDataSourceService(new Dictionary<string, IApplicationDataSource>
                {
                    { CreditApplicationItemDataSource.DataSourceNameShared, new CreditApplicationItemDataSource(creditApplicationCustomEditableFieldsService, preCreditContextFactoryService, encryptionService) },
                    { CreditApplicationInfoSource.DataSourceNameShared, new CreditApplicationInfoSource(applicationInfoService) },
                    { BankAccountTypeAndNrCreditApplicationItemDataSource.DataSourceNameShared, new BankAccountTypeAndNrCreditApplicationItemDataSource(creditApplicationCustomEditableFieldsService, preCreditContextFactoryService, encryptionService) },
                    { ComplexApplicationListDataSource.DataSourceNameShared, new ComplexApplicationListDataSource(creditApplicationCustomEditableFieldsService, preCreditContextFactoryService) },
                    { CustomerCardItemDataSource.DataSourceNameShared, new CustomerCardItemDataSource(customerClient, preCreditContextFactoryService) },
                    { CurrentCreditDecisionItemsDataSource.DataSourceNameShared, new CurrentCreditDecisionItemsDataSource(preCreditContextFactoryService) }
                });

        public Dictionary<string, Dictionary<string, string>> GetData(string applicationNr, params ApplicationDataSourceServiceRequest[] requests)
        {
            var result = new Dictionary<string, Dictionary<string, string>>(requests.Length);
            foreach (var r in requests)
            {
                var datasourceName = r.DataSourceName;
                var itemNames = r.Names;
                if (!dataSources.ContainsKey(datasourceName))
                    throw new NTechCoreWebserviceException($"Application {applicationNr}: No such datasource '{datasourceName}'. Valid datasources: {string.Join(", ", this.dataSources.Keys)}");
                var dataSource = dataSources[datasourceName];
                result[datasourceName] = dataSource.GetItems(
                    applicationNr,
                    r.Names,
                    r.MissingItemStrategy,
                    observeMissingItems: r.ObserveMissingItems,
                    getDefaultValue: (r.GetDefaultValue ?? (_ => "null")),
                    observeChangedItems: r.ObserveChangedItems);
            }
            return result;
        }

        public int? SetData(string applicationNr, ApplicationDataSourceEditModel edit, INTechCurrentUserMetadata currentUser)
        {
            var ds = dataSources[edit.DataSourceName];
            if (!ds.IsSetDataSupported)
                throw new NTechCoreWebserviceException($"{edit.DataSourceName} does not support SetData")
                {
                    ErrorCode = "setDataNotSupported",
                    IsUserFacing = true,
                    ErrorHttpStatusCode = 400
                };

            var defaultValueIfMissing = Guid.NewGuid().ToString();
            var currentValue = ds.GetItems(applicationNr,
                new HashSet<string> { edit.CompoundItemName },
                ApplicationDataSourceMissingItemStrategy.UseDefaultValue,
                getDefaultValue: _ => defaultValueIfMissing).Opt(edit.CompoundItemName) ?? defaultValueIfMissing;
            var isMissingCurrentValue = currentValue == defaultValueIfMissing;
            currentValue = isMissingCurrentValue ? null : currentValue;

            return ds.SetData(applicationNr, edit.CompoundItemName, edit.IsDelete, isMissingCurrentValue, currentValue, edit.NewValue, currentUser);
        }

        public bool SetDataBatch(string applicationNr, List<ApplicationDataSourceEditModel> edits, INTechCurrentUserMetadata currentUser)
        {
            //TODO: Split this up by datasource and make each datasource expose if it supports batch edits or not. If it does allow:
            //      Level 1: Setting a batch of values
            //      Level 2: Starting a unit of work, settig batched of values across multiple datasources, commiting all of them
            var wasEdited = false;
            foreach (var edit in edits)
            {
                if (SetData(applicationNr, edit, currentUser).HasValue)
                    wasEdited = true;
            }
            return wasEdited;
        }

        public static string MissingItemValue => ApplicationDataSourceResult.MissingItemValue;

        public ApplicationDataSourceResult GetDataSimple(string applicationNr, Dictionary<string, HashSet<string>> itemNamesByDataSourceName)
        {
            var requests = itemNamesByDataSourceName.Keys.Select(x => new ApplicationDataSourceServiceRequest
            {
                DataSourceName = x,
                MissingItemStrategy = Datasources.ApplicationDataSourceMissingItemStrategy.UseDefaultValue,
                GetDefaultValue = _ => MissingItemValue,
                Names = itemNamesByDataSourceName[x]
            }).ToArray();

            var result = GetData(applicationNr, requests);

            return new ApplicationDataSourceResult(applicationNr, result);
        }

        public Dictionary<string, HashSet<string>> NewSimpleRequest(string dataSourceName, params string[] itemNames)
        {
            return AppendToSimpleRequest(null, dataSourceName, itemNames);
        }

        public Dictionary<string, HashSet<string>> AppendToSimpleRequest(Dictionary<string, HashSet<string>> request, string dataSourceName, params string[] itemNames)
        {
            request = request ?? new Dictionary<string, HashSet<string>>();
            if (!request.ContainsKey(dataSourceName))
                request[dataSourceName] = new HashSet<string>();
            if (itemNames != null)
            {
                foreach (var n in itemNames.Where(x => x != null))
                {
                    request[dataSourceName].Add(n);
                }
            }

            return request;
        }
    }
}