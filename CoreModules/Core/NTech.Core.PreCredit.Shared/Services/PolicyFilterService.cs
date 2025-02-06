using Newtonsoft.Json;
using nPreCredit;
using nPreCredit.Code.Services;
using nPreCredit.Code.StandardPolicyFilters;
using nPreCredit.DbModel;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Infrastructure.CoreValidation;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace NTech.Core.PreCredit.Shared.Services
{
    public class PolicyFilterService
    {
        private readonly PreCreditContextFactory preCreditContextFactory;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly IPreCreditEnvSettings envSettings;
        private readonly KeyValueStore webApplicationPreScore;

        public PolicyFilterService(PreCreditContextFactory preCreditContextFactory, IClientConfigurationCore clientConfiguration, IPreCreditEnvSettings envSettings, KeyValueStoreService keyValueStoreService)
        {
            this.preCreditContextFactory = preCreditContextFactory;
            this.clientConfiguration = clientConfiguration;
            this.envSettings = envSettings;
            this.webApplicationPreScore = new KeyValueStore(KeyValueStoreKeySpaceCode.WebApplicationPreScoreResult, keyValueStoreService);
        }

        public static bool IsEnabled(IPreCreditEnvSettings envSettings) => envSettings.IsStandardUnsecuredLoansEnabled || envSettings.IsStandardMortgageLoansEnabled;

        public CreateOrEditPolicyFilterSetResponse CreateOrEditPolicyFilterSet(CreateOrEditPolicyFilterSetRequest request)
        {
            request = request ?? new CreateOrEditPolicyFilterSetRequest();

            if ((request.UpdateExisting != null) == (request.NewPending != null))
            {
                throw new NTechCoreWebserviceException("Exactly one of UpdateExisting and NewPending must be used") { IsUserFacing = true };
            }

            if (request.NewPending != null)
            {
                var useSuppliedName = !string.IsNullOrWhiteSpace(request.NewPending.Name);
                var useGeneratedName = request.NewPending.UseGeneratedName.GetValueOrDefault();
                if (useSuppliedName == useGeneratedName)
                    throw new NTechCoreWebserviceException("Either set NewPending.Name or use NewPending.UseGeneratedName = true") { IsUserFacing = true };
            }

            using (var context = preCreditContextFactory.CreateContext())
            {
                if (request.NewPending != null)
                {
                    StandardPolicyFilterRuleSet ruleSet;
                    ruleSet = context.StandardPolicyFilterRuleSetsQueryable.SingleOrDefault(x => x.SlotName == "Pending");
                    if (ruleSet == null)
                    {
                        ruleSet = context.FillInfrastructureFields(new StandardPolicyFilterRuleSet
                        {
                            RuleSetName = request.NewPending.UseGeneratedName.GetValueOrDefault()
                                ? $"New policy {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}" //Intentionally not using the timemachine so changes can be seen in test where time stands still
                                : request.NewPending.Name,
                            SlotName = "Pending",
                            RuleSetModelData = JsonConvert.SerializeObject(new RuleSet()) //Start with empty and the use can fill it in.
                        });
                        context.AddStandardPolicyFilterRuleSets(ruleSet);

                        context.SaveChanges();
                    }

                    return new CreateOrEditPolicyFilterSetResponse
                    {
                        Id = ruleSet.Id,
                        WasCreated = true
                    };
                }
                else
                {
                    var ruleSet = context.StandardPolicyFilterRuleSetsQueryable.SingleOrDefault(x => x.Id == request.UpdateExisting.Id.Value);
                    if (ruleSet == null)
                        throw new NTechCoreWebserviceException("No such ruleset exists") { IsUserFacing = true, ErrorHttpStatusCode = 400, ErrorCode = "noSuchSetExists" };
                    ruleSet.RuleSetModelData = JsonConvert.SerializeObject(request.UpdateExisting.RuleSet);

                    if (!string.IsNullOrWhiteSpace(request.UpdateExisting.UpdatedName))
                        ruleSet.RuleSetName = request.UpdateExisting.UpdatedName;

                    context.SaveChanges();

                    return new CreateOrEditPolicyFilterSetResponse
                    {
                        Id = ruleSet.Id
                    };
                }
            }
        }

        public FetchPolicyFilterRuleSetsResponse FetchPolicyFilterRuleSets(FetchPolicyFilterRuleSetsRequest request)
        {
            request = request ?? new FetchPolicyFilterRuleSetsRequest();

            using (var context = preCreditContextFactory.CreateContext())
            {
                var response = new FetchPolicyFilterRuleSetsResponse
                {
                    RuleSets = context.StandardPolicyFilterRuleSetsQueryable.OrderByDescending(x => x.Id).Select(x => new FetchPolicyFilterRuleSetsResponse.RuleSet
                    {
                        Id = x.Id,
                        RuleSetName = x.RuleSetName,
                        SlotName = x.SlotName,
                        ModelData = x.RuleSetModelData
                    }).ToList()
                };

                if (request.IncludeAllRules.GetValueOrDefault())
                {
                    var country = clientConfiguration.Country.BaseCountry;
                    var language = "en";

                    response.AllRules = RuleFactory
                        .GetProductFilteredRuleNames(envSettings)
                        .Select(ruleName =>
                        {
                            var rule = RuleFactory.GetRuleByName(ruleName);
                            return new FetchPolicyFilterRuleSetsResponse.RuleDisplayModel
                            {
                                RuleName = ruleName,
                                RuleDisplayName = rule.GetDisplayName(country, language),
                                Description = rule.GetDescription(country, language),
                                StaticParameters = rule.StaticParameters?.Select(x => new FetchPolicyFilterRuleSetsResponse.RuleStaticParameter
                                {
                                    Name = x.Name,
                                    TypeCode = x.TypeCode.ToString(),
                                    IsList = x.IsList,
                                    Options = x.Options?.Select(y => new FetchPolicyFilterRuleSetsResponse.StaticParameterOption
                                    {
                                        Code = y.Value,
                                        DisplayName = y.GetDisplayName(language)
                                    })?.ToList()
                                })?.ToList(),
                                DefaultRejectionReasonName = rule.DefaultRejectionReasonName
                            };
                        })
                        .ToList();
                }

                return response;
            }
        }

        public int? ChangePolicyFilterSetSlot(int id, string slotName)
        {
            if (!(string.IsNullOrWhiteSpace(slotName) || slotName.IsOneOf("A", "B", "Pending", "WebPreScore")))
                throw new NTechCoreWebserviceException("Invalid slotName")
                {
                    IsUserFacing = true,
                    ErrorCode = "invalidSlotName",
                    ErrorHttpStatusCode = 400
                };

            using (var context = preCreditContextFactory.CreateContext())
            {
                var ruleSet = context.StandardPolicyFilterRuleSetsQueryable.SingleOrDefault(x => x.Id == id);
                if (ruleSet == null)
                    throw new NTechCoreWebserviceException("No such ruleset exists")
                    {
                        IsUserFacing = true,
                        ErrorCode = "noSuchSetExists",
                        ErrorHttpStatusCode = 400
                    };

                context.BeginTransaction();
                try
                {
                    int? movedToInactiveId = null;
                    if (slotName != null)
                    {
                        var currentRuleSetInSlot = context.StandardPolicyFilterRuleSetsQueryable.SingleOrDefault(x => x.SlotName == slotName);
                        if (currentRuleSetInSlot != null)
                        {
                            if (currentRuleSetInSlot.Id == id)
                                throw new NTechCoreWebserviceException("Ruleset is already in that slot")
                                {
                                    IsUserFacing = true,
                                    ErrorCode = "alreadyInSlot",
                                    ErrorHttpStatusCode = 400
                                };
                            currentRuleSetInSlot.SlotName = null;
                            movedToInactiveId = currentRuleSetInSlot.Id;
                            /*
                             This is why we need a transaction. EF is stupid and does the updates in the wrong order
                             otherwise and hits the unique constraint for slot name.
                             */
                            context.SaveChanges();
                        }
                    }

                    ruleSet.SlotName = !string.IsNullOrWhiteSpace(slotName) ? slotName : null;

                    context.SaveChanges();
                    context.CommitTransaction();
                    return movedToInactiveId;
                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }
            }
        }

        public DirectPolicyFilterScoringResponse ScoreDirect(DirectPolicyFilterScoringRequest request)
        {
            using(var context = preCreditContextFactory.CreateContext())
            {
                var dbRuleSet = context.StandardPolicyFilterRuleSetsQueryable.SingleOrDefault(x => x.SlotName == request.SlotName);
                if (dbRuleSet == null)
                {
                    if (request.IsSlotNameMissingAllowed == true)
                        return new DirectPolicyFilterScoringResponse
                        {
                            IsSlotNameMissing = true
                        };
                    else
                        throw new NTechCoreWebserviceException("No such slot exists") { IsUserFacing = true, ErrorHttpStatusCode = 400, ErrorCode = "slotNameMissing" };
                }
                var ruleSet = JsonConvert.DeserializeObject<RuleSet>(dbRuleSet.RuleSetModelData);
                var dataSource = new DirectPolicyFilterScoringRequestDataSource(request);
                var engine = new PolicyFilterEngine();
                var policyResult = engine.Evaluate(ruleSet, dataSource);
                return new DirectPolicyFilterScoringResponse
                {
                    IsSlotNameMissing = false,
                    PolicyFilterResult = policyResult
                };
            }
        }

        public WebPreScorePolicyFilterResponse PreScoreWebApplication(WebPreScorePolicyFilterRequest request)
        {
            var scoreResult = ScoreDirect(new DirectPolicyFilterScoringRequest
            {
                IsSlotNameMissingAllowed = true,
                NrOfApplicants = request.NrOfApplicants,
                ScoringVariables = request.ScoringVariables,
                SlotName = "WebPreScore"
            });
            string persistedId = null;
            var isAcceptRecommended = scoreResult.IsSlotNameMissing || scoreResult.PolicyFilterResult.IsAcceptRecommended == true;
            if (request.PersistResult == true)
            {
                persistedId = OneTimeTokenGenerator.SharedInstance.GenerateUniqueToken();
                
                webApplicationPreScore.SetValue(persistedId, JsonConvert.SerializeObject(new StoredPreScoreResult
                {
                    Version = 1,
                    IsAcceptRecommended = isAcceptRecommended,
                    PolicyFilterResult = scoreResult.PolicyFilterResult,
                    IsSlotNameMissing = scoreResult.IsSlotNameMissing
                }));
            }
            return new WebPreScorePolicyFilterResponse
            {
                IsAcceptRecommended = isAcceptRecommended,
                PreScoreResultId = persistedId
            };
        }        

        public ImportPolicyFilterResponse ImportPolicyFilter(ImportPolicyFilterRequest request)
        {
            if (!RuleSet.TryParseImportCode(request.ImportCode, out var importedRuleSet))
                throw new NTechCoreWebserviceException("Invalid ImportCode") { ErrorHttpStatusCode = 400, IsUserFacing = true };

            var existingRuleNames = RuleFactory.GetProductFilteredRuleNames(envSettings).ToHashSetShared();
            var importedRuleNames = importedRuleSet.InternalRules.Concat(importedRuleSet.ExternalRules).Concat(importedRuleSet.ManualControlOnAcceptedRules).Select(x => x.RuleName).ToHashSetShared();
            var invalidRuleNames = importedRuleNames.Except(existingRuleNames).ToList();
            if (invalidRuleNames.Any())
                throw new NTechCoreWebserviceException("The following rule names are not allowed: " + string.Join(",", invalidRuleNames)) { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            using(var context = preCreditContextFactory.CreateContext())
            {
                StandardPolicyFilterRuleSet inactivatedRuleSet = null;
                if(request.SlotName != null)
                {
                    inactivatedRuleSet = context.StandardPolicyFilterRuleSetsQueryable.Where(x => x.SlotName == request.SlotName).SingleOrDefault();
                    if (inactivatedRuleSet != null)
                    {
                        inactivatedRuleSet.SlotName = null;
                        inactivatedRuleSet.ChangedDate = context.CoreClock.Now;
                        inactivatedRuleSet.ChangedById = context.CurrentUserId;
                    }
                }                    

                var newRuleSet = context.FillInfrastructureFields(new StandardPolicyFilterRuleSet
                {
                    RuleSetName = request.RuleSetName,
                    SlotName = request.SlotName,
                    RuleSetModelData = JsonConvert.SerializeObject(importedRuleSet)
                });                
                context.AddStandardPolicyFilterRuleSets(newRuleSet);

                context.SaveChanges();

                return new ImportPolicyFilterResponse
                {
                    NewId = newRuleSet.Id,
                    MovedToInactiveId = inactivatedRuleSet?.Id
                };
            }
        }
    }

    public class StoredPreScoreResult
    {
        public int Version { get; set; }
        public bool IsAcceptRecommended { get; set; }
        public EngineResult PolicyFilterResult { get; set; }
        public bool IsSlotNameMissing { get; set; }

        public static T Deserialize<T>(string storedValue) where T : StoredPreScoreResult, new()
        {
            if (storedValue == null)
                return null;
            return JsonConvert.DeserializeObject<T>(storedValue);
        }
    }

    public class ImportPolicyFilterRequest
    {
        [IsAnyOf(new[] { "A", "B", "WebPreScore", "Pending" })]
        public string SlotName { get; set; }

        [Required]
        public string RuleSetName { get; set; }

        [Required]
        public string ImportCode { get; set; }
    }

    public class ImportPolicyFilterResponse
    {
        public int? MovedToInactiveId { get; set; }
        public int NewId { get; set; }
    }

    public class CreateOrEditPolicyFilterSetRequest
    {
        public NewPendingModel NewPending { get; set; }
        public UpdateExistingModel UpdateExisting { get; set; }
        public class UpdateExistingModel
        {
            public int? Id { get; set; }
            public string UpdatedName { get; set; }
            public RuleSet RuleSet { get; set; }
        }
        public class NewPendingModel
        {
            public string Name { get; set; }
            public bool? UseGeneratedName { get; set; }
        }
    }

    public class CreateOrEditPolicyFilterSetResponse
    {
        public int Id { get; set; }
        public bool WasCreated { get; set; }
    }
    public class FetchPolicyFilterRuleSetsRequest
    {
        public bool? IncludeAllRules { get; set; }
    }

    public class FetchPolicyFilterRuleSetsResponse
    {
        public List<RuleSet> RuleSets { get; set; }
        public List<RuleDisplayModel> AllRules { get; set; }
        public class RuleSet
        {
            public int Id { get; set; }
            public string SlotName { get; set; }
            public string RuleSetName { get; set; }
            public string ModelData { get; set; }
        }

        public class RuleDisplayModel
        {
            public string RuleName { get; set; }
            public string RuleDisplayName { get; set; }
            public string Description { get; set; }
            public List<RuleStaticParameter> StaticParameters { get; set; }
            public string DefaultRejectionReasonName { get; set; }
        }

        public class RuleStaticParameter
        {
            public string Name { get; set; }
            public bool IsList { get; set; }
            public string TypeCode { get; set; }
            public List<StaticParameterOption> Options { get; set; }
        }

        public class StaticParameterOption
        {
            public string Code { get; set; }
            public string DisplayName { get; set; }
        }
    }

    public class WebPreScorePolicyFilterRequest : DirectPolicyFilterScoringRequestBase
    {
        /// <summary>
        /// If true the result is stored and an id is returned.
        /// </summary>
         public bool? PersistResult { get; set; }
    }

    public class WebPreScorePolicyFilterResponse
    {
        public bool IsAcceptRecommended { get; set; }
        /// <summary>
        /// Included if PersistResult = true
        /// </summary>
        public string PreScoreResultId { get; set; }
    }

    public class DirectPolicyFilterScoringRequestBase
    {
        [Required]
        public List<DirectPolicyFilterScoringRequestScoringVariable> ScoringVariables { get; set; }
        [Required]
        public int NrOfApplicants { get; set; }
    }

    public class DirectPolicyFilterScoringRequest : DirectPolicyFilterScoringRequestBase
    {
        [Required]
        public string SlotName { get; set; }
        public bool? IsSlotNameMissingAllowed { get; set; }
    }

    public class DirectPolicyFilterScoringResponse
    {
        public EngineResult PolicyFilterResult { get; set; }
        public bool IsSlotNameMissing { get; set; }
    }

    public class DirectPolicyFilterScoringRequestScoringVariable
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Value { get; set; }
        public int? ApplicantNr { get; set; }
    }

    public class DirectPolicyFilterScoringRequestDataSource : IPolicyFilterDataSource
    {
        private readonly DirectPolicyFilterScoringRequest request;

        public DirectPolicyFilterScoringRequestDataSource(DirectPolicyFilterScoringRequest request)
        {
            this.request = request;
        }

        public VariableSet LoadVariables(ISet<string> applicationVariableNames, ISet<string> applicantVariableNames)
        {
            var variableSet = new VariableSet(request.NrOfApplicants);
            foreach (var variable in request.ScoringVariables)
            {
                if (!variable.ApplicantNr.HasValue && applicationVariableNames.Contains(variable.Name))
                    variableSet.SetApplicationValue(variable.Name, variable.Value);
                else if (variable.ApplicantNr.HasValue && variable.ApplicantNr > 0 && variable.ApplicantNr <= request.NrOfApplicants && applicantVariableNames.Contains(variable.Name))
                    variableSet.SetApplicantValue(variable.Name, variable.ApplicantNr.Value, variable.Value);
            }
            
            return variableSet;
        }
    }
}