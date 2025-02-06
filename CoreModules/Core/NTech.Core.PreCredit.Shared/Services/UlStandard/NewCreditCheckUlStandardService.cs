using Newtonsoft.Json;
using nPreCredit.Code.Services.UnsecuredLoans;
using nPreCredit.Code.StandardPolicyFilters;
using nPreCredit.Code.StandardPolicyFilters.DataSources;
using NTech.Core.Module.Shared.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services.NewUnsecuredLoans
{
    public class NewCreditCheckUlStandardService
    {
        private readonly ApplicationInfoService applicationInfoService;
        private readonly UnsecuredLoanLtlAndDbrService ltlAndDbrService;
        private readonly IPreCreditContextFactoryService preCreditContextFactory;
        private readonly UnsecuredLoanStandardApplicationPolicyFilterDataSourceFactory dataSourceFactory;
        private readonly IClientConfigurationCore clientConfiguration;

        public NewCreditCheckUlStandardService(ApplicationInfoService applicationInfoService, UnsecuredLoanLtlAndDbrService ltlAndDbrService,
            IPreCreditContextFactoryService preCreditContextFactory, UnsecuredLoanStandardApplicationPolicyFilterDataSourceFactory dataSourceFactory,
            IClientConfigurationCore clientConfiguration)
        {
            this.applicationInfoService = applicationInfoService;
            this.ltlAndDbrService = ltlAndDbrService;
            this.preCreditContextFactory = preCreditContextFactory;
            this.dataSourceFactory = dataSourceFactory;
            this.clientConfiguration = clientConfiguration;
        }
        public UnsecuredLoanStandardCreditRecommendationModel NewCreditCheck(string applicationNr)
        {
            var ai = applicationInfoService.GetApplicationInfo(applicationNr);
            if (!ai.IsActive)
                throw new NTechCoreWebserviceException("Application is not active") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            if (ai.IsFinalDecisionMade)
                throw new NTechCoreWebserviceException("Application cannot be changed") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            var ltlAndDbr = ltlAndDbrService.CalculateLeftToLiveOnAndDbr(applicationNr);

            using (var context = preCreditContextFactory.CreateExtended())
            {
                EngineResult policyFilterResult = null;

                var dbRuleSet = context.StandardPolicyFilterRuleSetsQueryable.Where(x => x.SlotName == "A").SingleOrDefault();
                var dataSource = dataSourceFactory.CreateDataSource(applicationNr, false, true);
                if (dbRuleSet != null)
                {
                    var ruleSet = JsonConvert.DeserializeObject<RuleSet>(dbRuleSet.RuleSetModelData);
                    var engine = new PolicyFilterEngine();
                    policyFilterResult = engine.Evaluate(ruleSet, dataSource);
                }

                var probabilityOfDefault = GetProbabilityOfDefault(applicationNr);

                var recommendation = UnsecuredLoanStandardCreditRecommendationModel.Create(
                    ltlAndDbr.LeftToLiveOn,
                    ltlAndDbr.Dbr, ltlAndDbr.DbrMissingReason,
                    policyFilterResult,
                probabilityOfDefault,
                    probabilityOfDefault.HasValue ? null : "mainApplicantCreditReportRiskValue missing",
                    clientConfiguration.Country.BaseCountry, "en");

                return recommendation;
            }
        }

        private decimal? GetProbabilityOfDefault(string applicationNr)
        {
            //Use risk value for the main applicant but only if the there is already a credit report. Either because someone bought one
            //manually for this application or one of the scoring rules forced it. Dont buy one just for pd.
            var dataSource = dataSourceFactory.CreateDataSource(applicationNr, false, false);
            const string PdScoringVariableName = "mainApplicantCreditReportRiskValue";
            var variables = dataSource.LoadVariables(new[] { PdScoringVariableName }.ToHashSetShared(), new string[] { }.ToHashSetShared());
            return new ScopedVariableSet(variables, null).GetDecimalOptional(PdScoringVariableName);
        }
    }
}