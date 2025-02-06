using nPreCredit.Code.Services;
using NTech.Banking.PluginApis.CreateApplication;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using System;

namespace nPreCredit.Code.Plugins
{
    public class PluginMortgageLoanApplicationRequestTranslator : PluginApplicationRequestTranslatorBase
    {
        private static Lazy<Type> requestType = new Lazy<Type>(() =>
            FindPluginType(typeof(CreateMortgageLoanApplicationPlugin<>)));

        public PluginMortgageLoanApplicationRequestTranslator(
            ICoreClock clock, ICustomerClient customerClient, IMortgageLoanWorkflowService workflowService,
            IKeyValueStoreService keyValueStoreService, ApplicationDataSourceService applicationDataSourceService,
            IPreCreditContextFactoryService preCreditContextFactoryService) : base(clock, customerClient, workflowService, keyValueStoreService, applicationDataSourceService, "MA", preCreditContextFactoryService, NEnv.GetAffiliateModels)
        {
        }

        public static Type GetRequestType(object instance = null)
        {
            return GetRequestType(requestType.Value, instance: instance);
        }

        public bool TranslateApplicationRequest(object externalRequest, out CreateApplicationRequestModel request, out Tuple<string, string> errorCodeAndMessage)
        {
            return TranslateApplicationRequest(externalRequest, requestType.Value, out request, out errorCodeAndMessage);
        }
    }
}