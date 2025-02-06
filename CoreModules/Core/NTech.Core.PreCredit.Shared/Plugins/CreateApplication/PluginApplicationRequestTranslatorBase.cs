using nPreCredit.Code.Services;
using NTech.Banking.PluginApis.CreateApplication;
using NTech.Core;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.PreCredit.Services;
using NTech.Core.PreCredit.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Plugins
{
    public abstract class PluginApplicationRequestTranslatorBase : PluginTranslatorBase
    {
        protected Context context;

        public PluginApplicationRequestTranslatorBase(
            ICoreClock clock,
            ICustomerClient customerClient,
            ISharedWorkflowService workflowService,
            IKeyValueStoreService keyValueStoreService,
            ApplicationDataSourceService applicationDataSourceService,
            string applicationPrefix,
            IPreCreditContextFactoryService preCreditContextFactoryService,
            Func<List<AffiliateModel>> getAffiliates)
        {
            context = new Context(clock, customerClient, workflowService, keyValueStoreService, applicationDataSourceService, applicationPrefix, getAffiliates, preCreditContextFactoryService);
        }

        protected bool TranslateApplicationRequest(object externalRequest, Type requestType, out CreateApplicationRequestModel request, out Tuple<string, string> errorCodeAndMessage)
        {
            var t = requestType;

            var instance = t.GetConstructors().Single().Invoke(null);
            t.GetProperty("Context").SetValue(instance, context);

            var result = (Tuple<bool, CreateApplicationRequestModel, Tuple<string, string>>)t.GetMethod("TryTranslateRequest").Invoke(instance, new object[] { externalRequest });

            request = result.Item2;
            errorCodeAndMessage = result.Item3;

            return result.Item1;
        }

        public class Context : ApplicationPluginContextBase, IApplicationCreationContext
        {
            private readonly Lazy<CreditApplicationNrGenerator> applicationNrGenerator;
            private readonly Func<List<AffiliateModel>> getAffiliateModels;

            public Context(
                ICoreClock clock,
                ICustomerClient customerClient,
                ISharedWorkflowService workflowService,
                IKeyValueStoreService keyValueStoreService,
                ApplicationDataSourceService applicationDataSourceService,
                string applicationNrPrefix,
                Func<List<AffiliateModel>> getAffiliateModels,
                IPreCreditContextFactoryService preCreditContextFactoryService) : base(clock, customerClient, workflowService, keyValueStoreService, applicationDataSourceService)
            {
                this.applicationNrGenerator = new Lazy<CreditApplicationNrGenerator>(() => new CreditApplicationNrGenerator(() => applicationNrPrefix, new CreditApplicationKeySequenceGenerator(preCreditContextFactoryService)));
                this.getAffiliateModels = getAffiliateModels;
            }

            public string GenerateNewApplicationNr()
            {
                return applicationNrGenerator.Value.GenerateNewApplicationNr();
            }

            public List<IApplicationAffiliateModel> GetAffiliates()
            {
                return getAffiliateModels().AsEnumerable<IApplicationAffiliateModel>().ToList();
            }
        }
    }
}