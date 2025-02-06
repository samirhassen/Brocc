using nPreCredit.Code.Services;
using NTech.Banking.PluginApis.AlterApplication;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using System;

namespace nPreCredit.Code.Plugins
{
    public class PluginMortgageLoanSubmitAdditionalQuestionsRequestTranslator : AlterApplicationRequestTranslatorBase, ISubmitAdditionalQuestionsPlugin
    {
        private static Lazy<Type> requestType = new Lazy<Type>(() =>
            FindPluginTypeImplementingInterface(typeof(AlterMortgageLoanApplicationPlugin<>), typeof(ISubmitAdditionalQuestionsPlugin)));

        public PluginMortgageLoanSubmitAdditionalQuestionsRequestTranslator(
            ICoreClock clock, ICustomerClient customerClient, IMortgageLoanWorkflowService workflowService,
            IKeyValueStoreService keyValueStoreService,
            ApplicationDataSourceService applicationDataSourceService) : base(clock, customerClient, workflowService, keyValueStoreService, applicationDataSourceService)
        {
        }

        public static Type GetRequestType(object instance = null)
        {
            return GetRequestType(requestType.Value, instance: instance);
        }

        public bool TranslateApplicationRequest(object externalRequest, out AlterApplicationRequestModel request, out Tuple<string, string> errorCodeAndMessage)
        {
            return TranslateApplicationRequest(externalRequest, requestType.Value, out request, out errorCodeAndMessage);
        }
    }
}