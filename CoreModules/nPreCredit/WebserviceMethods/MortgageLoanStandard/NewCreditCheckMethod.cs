using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.SharedStandard;
using nPreCredit.Code.StandardPolicyFilters;
using nPreCredit.Code.StandardPolicyFilters.DataSources;
using NTech.Core.Module.Shared.Clients;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.MortgageLoanStandard
{
    public class NewCreditCheckMethod : TypedWebserviceMethod<NewCreditCheckMethod.Request, NewCreditCheckMethod.Response>
    {
        public override string Path => "MortgageLoanStandard/New-CreditCheck";
        public override bool IsEnabled => NEnv.IsStandardMortgageLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var ai = requestContext
                .Resolver()
                .Resolve<ApplicationInfoService>()
                .GetApplicationInfo(request.ApplicationNr);

            if (!ai.IsActive)
                return Error("Application is not active");

            if (ai.IsFinalDecisionMade)
                return Error("Application cannot be changed");

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                EngineResult policyFilterResult = null;

                var dbRuleSet = context.StandardPolicyFilterRuleSets.Where(x => x.SlotName == "A").SingleOrDefault();
                var dataSourceFactory = requestContext.Resolver().Resolve<MortgageLoanStandardApplicationPolicyFilterDataSourceFactory>();
                var dataSource = dataSourceFactory.CreateDataSource(request.ApplicationNr, false, true);
                if (dbRuleSet != null)
                {
                    var ruleSet = JsonConvert.DeserializeObject<RuleSet>(dbRuleSet.RuleSetModelData);
                    var engine = new PolicyFilterEngine();
                    policyFilterResult = engine.Evaluate(ruleSet, dataSource);
                }

                var resolver = requestContext.Resolver();

                var ltxService = new MortgageLoanLtxService(CoreClock.SharedInstance, resolver.Resolve<IPreCreditContextFactoryService>(),
                    resolver.Resolve<ICustomerClient>(), NEnv.ClientCfgCore, new LtlDataTables());

                var result = ltxService.CalculateAll(request.ApplicationNr);

                var recommendation = MortgageLoanStandardCreditRecommendationModel.Create(
                    policyFilterResult,
                    result.LeftToLiveOn,
                    result.Lti, result.LtiMissingReason,
                    result.Ltv, result.LtvMissingReason,
                    NEnv.ClientCfg.Country.BaseCountry, "en");

                var tempStorage = requestContext.Resolver().Resolve<IEncryptedTemporaryStorageService>();

                return new Response
                {
                    Recommendation = recommendation,
                    RecommendationTemporaryStorageKey = tempStorage.StoreString(JsonConvert.SerializeObject(recommendation), TimeSpan.FromHours(24))
                };
            }
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
        }

        public class Response
        {
            public MortgageLoanStandardCreditRecommendationModel Recommendation { get; set; }
            public string RecommendationTemporaryStorageKey { get; set; }
        }
    }
}