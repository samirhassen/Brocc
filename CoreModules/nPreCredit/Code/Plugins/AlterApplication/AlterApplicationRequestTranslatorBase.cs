using nPreCredit.Code.Services;
using NTech.Banking.PluginApis.AlterApplication;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using System;
using System.Linq;

namespace nPreCredit.Code.Plugins
{
    public abstract class AlterApplicationRequestTranslatorBase : PluginTranslatorBase
    {
        protected Context context;

        public AlterApplicationRequestTranslatorBase(
            ICoreClock clock,
            ICustomerClient customerClient,
            ISharedWorkflowService workflowService,
            IKeyValueStoreService keyValueStoreService,
            ApplicationDataSourceService applicationDataSourceService)
        {
            context = new Context(clock, customerClient, workflowService, keyValueStoreService, applicationDataSourceService);
        }

        protected bool TranslateApplicationRequest(object externalRequest, Type requestType, out AlterApplicationRequestModel request, out Tuple<string, string> errorCodeAndMessage)
        {
            var t = requestType;

            var instance = t.GetConstructors().Single().Invoke(null);
            t.GetProperty("Context").SetValue(instance, context);

            var result = (Tuple<bool, AlterApplicationRequestModel, Tuple<string, string>>)t.GetMethod("TryTranslateRequest").Invoke(instance, new object[] { externalRequest });

            request = result.Item2;
            errorCodeAndMessage = result.Item3;

            return result.Item1;
        }

        protected class Context : ApplicationPluginContextBase, IApplicationAlterationContext
        {
            public Context(
                ICoreClock clock,
                ICustomerClient customerClient,
                ISharedWorkflowService workflowService,
                IKeyValueStoreService keyValueStoreService,
                ApplicationDataSourceService applicationDataSourceService) : base(clock, customerClient, workflowService, keyValueStoreService, applicationDataSourceService)
            {
            }
        }
    }
}