using Newtonsoft.Json;
using nPreCredit.Code.StandardPolicyFilters;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class CreateTestPolicyFilterSetMethod : TypedWebserviceMethod<CreateTestPolicyFilterSetMethod.Request, CreateTestPolicyFilterSetMethod.Response>
    {
        public override string Path => "LoanStandard/PolicyFilters/Create-TestSet";

        public override bool IsEnabled => (NEnv.IsStandardUnsecuredLoansEnabled || NEnv.IsStandardMortgageLoansEnabled) && !NEnv.IsProduction;

        private static RuleAndStaticParameterValues CreateRuleAndStaticParameterValues(string name, StaticParameterSet staticParameterValues)
        {
            var rule = RuleFactory.GetRuleByName(name);
            return new RuleAndStaticParameterValues(rule.Name, staticParameterValues, rule.DefaultRejectionReasonName);
        }

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var slotName = request?.OverrideSlotName ?? "A";


            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                if (request?.SkipIfExists ?? false)
                {
                    if (context.StandardPolicyFilterRuleSets.Any(x => x.SlotName == slotName))
                        return new Response
                        {
                            WasCreated = false
                        };
                }

                RuleSet rules;
                var clientSpecificTestRuleSetFile = NTechEnvironment.Instance.StaticResourceFile("precredit.policyfilter.testrulesetfile", "initial-test-policyfilter-ruleset.json", false);
                if (clientSpecificTestRuleSetFile.Exists)
                {
                    rules = JsonConvert.DeserializeObject<RuleSet>(System.IO.File.ReadAllText(clientSpecificTestRuleSetFile.FullName, System.Text.Encoding.UTF8));
                }
                else
                {
                    rules = new RuleSet()
                    {
                        InternalRules = new RuleAndStaticParameterValues[]
                        {
                            CreateRuleAndStaticParameterValues("MinAllowedApplicantAge", StaticParameterSet.CreateEmpty().SetInt("minApplicantAgeInYears", 18)),
                        },
                        ExternalRules = CreateExternalRules(),
                        ManualControlOnAcceptedRules = new RuleAndStaticParameterValues[]
                        {

                        }
                    };
                }


                context.StandardPolicyFilterRuleSets.Add(context.FillInfrastructureFields(new DbModel.StandardPolicyFilterRuleSet
                {
                    RuleSetName = $"Test rules added {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}", //Intentionally not using the timemachine
                    SlotName = slotName,
                    RuleSetModelData = JsonConvert.SerializeObject(rules)
                }));

                context.SaveChanges();

                return new Response
                {
                    WasCreated = true
                };
            }
        }

        private static RuleAndStaticParameterValues[] CreateExternalRules()
        {
            if (NEnv.IsStandardUnsecuredLoansEnabled)
                return new RuleAndStaticParameterValues[]
                        {
                            CreateRuleAndStaticParameterValues("MaxAllowedMainApplicantPaymentRemarks", StaticParameterSet.CreateEmpty().SetInt("maxNrOfPaymentRemarks", 0)),
                            CreateRuleAndStaticParameterValues("MaxAllowedCoApplicantPaymentRemarks", StaticParameterSet.CreateEmpty().SetInt("maxNrOfPaymentRemarks", 0)),
                            CreateRuleAndStaticParameterValues("ApplicantHasBoxAddress", StaticParameterSet.CreateEmpty()),
                            CreateRuleAndStaticParameterValues("ApplicantHasPosteRestanteAddress", StaticParameterSet.CreateEmpty()),
                            CreateRuleAndStaticParameterValues("ApplicantHasGuardian", StaticParameterSet.CreateEmpty()),
                            CreateRuleAndStaticParameterValues("ApplicantMissingAddress", StaticParameterSet.CreateEmpty()),
                            CreateRuleAndStaticParameterValues("ApplicantStatusCode", StaticParameterSet.CreateEmpty()),
                            CreateRuleAndStaticParameterValues("MaxAllowedDbr", StaticParameterSet.CreateEmpty().SetDecimal("maxAllowedDbr", 1.5m)),
                        };
            else if (NEnv.IsStandardMortgageLoansEnabled)
                return new RuleAndStaticParameterValues[]
                        {
                            CreateRuleAndStaticParameterValues("MaxAllowedMainApplicantPaymentRemarks", StaticParameterSet.CreateEmpty().SetInt("maxNrOfPaymentRemarks", 0)),
                            CreateRuleAndStaticParameterValues("MaxAllowedCoApplicantPaymentRemarks", StaticParameterSet.CreateEmpty().SetInt("maxNrOfPaymentRemarks", 0)),
                            CreateRuleAndStaticParameterValues("ApplicantHasBoxAddress", StaticParameterSet.CreateEmpty()),
                            CreateRuleAndStaticParameterValues("ApplicantHasPosteRestanteAddress", StaticParameterSet.CreateEmpty()),
                            CreateRuleAndStaticParameterValues("ApplicantHasGuardian", StaticParameterSet.CreateEmpty()),
                            CreateRuleAndStaticParameterValues("ApplicantMissingAddress", StaticParameterSet.CreateEmpty()),
                            CreateRuleAndStaticParameterValues("ApplicantStatusCode", StaticParameterSet.CreateEmpty()),
                        };
            else
                throw new NotImplementedException();
        }

        public class Request
        {
            public string OverrideSlotName { get; set; }
            public bool? SkipIfExists { get; set; }
        }

        public class Response
        {
            public bool WasCreated { get; set; }
        }
    }
}