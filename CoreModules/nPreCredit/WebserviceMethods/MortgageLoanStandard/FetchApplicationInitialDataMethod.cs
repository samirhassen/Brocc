using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.CreditStandard;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.MortgageLoanStandard
{
    public class FetchApplicationInitialDataMethod : TypedWebserviceMethod<FetchApplicationInitialDataMethod.Request, FetchApplicationInitialDataMethod.Response>
    {
        public override string Path => "MortgageLoanStandard/FetchApplicationInitialData";

        public override bool IsEnabled => NEnv.IsStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var applicationInfoService = requestContext.Resolver().Resolve<ApplicationInfoService>();

            var info = applicationInfoService.GetApplicationInfo(request.ApplicationNr, true);
            if (info == null)
                return Error("No such application exists", errorCode: "noSuchApplicationExists");

            var applicants = applicationInfoService.GetApplicationApplicants(request.ApplicationNr);

            using (var context = new PreCreditContext())
            {
                var application = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == request.ApplicationNr)
                    .Select(x => new
                    {
                        x.CurrentCreditDecisionId,
                        ComplexListItems = x.ComplexApplicationListItems.OrderBy(y => y.Id).Select(y => new ComplexApplicationListItemBase { ListName = y.ListName, Nr = y.Nr, ItemName = y.ItemName, ItemValue = y.ItemValue, IsRepeatable = y.IsRepeatable }),
                        Decisions = x
                            .CreditDecisions
                            .Select(y => new
                            {
                                y.Id,
                                y.DecisionType,
                                DecisionItems = y.DecisionItems.Select(z => new { z.ItemName, z.Value, z.IsRepeatable })
                            })
                    })
                    .Single();

                var decisions = application.Decisions.Select(x => new
                {
                    x.Id,
                    Model = new Response.CreditDecisionModel
                    {
                        IsFinal = x.DecisionType != "Initial",
                        IsCurrent = x.Id == application.CurrentCreditDecisionId,
                        CreditDecisionItems = x.DecisionItems.Select(y => new Response.CreditDecisionItemModel
                        {
                            IsRepeatable = y.IsRepeatable,
                            ItemName = y.ItemName,
                            Value = y.Value
                        }).ToList()
                    }
                });

                var currentInitialDecision = decisions.Where(x => !x.Model.IsFinal).OrderByDescending(x => x.Id).FirstOrDefault()?.Model;
                var currentFinalDecision = decisions.Where(x => x.Model.IsFinal).OrderByDescending(x => x.Id).FirstOrDefault()?.Model;

                AppendRecommendations(context, currentFinalDecision, currentInitialDecision);

                var enums = CreditStandardEnumService.Instance;
                var enumLanguage = NEnv.ClientCfg.Country.GetBaseLanguage();
                return new Response
                {
                    ApplicationNr = request.ApplicationNr,
                    ApplicationWorkflowVersion = int.Parse(info.WorkflowVersion),
                    CurrentWorkflowModel = NEnv.MortgageLoanStandardWorkflow,
                    ApplicationInfo = info,
                    CustomerIdByApplicantNr = applicants.CustomerIdByApplicantNr,
                    ApplicantInfoByApplicantNr = applicants.ApplicantInfoByApplicantNr,
                    AllConnectedCustomerIdsWithRoles = applicants.AllConnectedCustomerIdsWithRoles.ToDictionary(x => x.Key, x => x.Value.ToList()),
                    NrOfApplicants = applicants.NrOfApplicants,
                    ComplexListItems = application.ComplexListItems.ToList(),
                    Enums = new Response.EnumsModel
                    {
                        CivilStatuses = enums.GetApiItems(CreditStandardEnumTypeCode.CivilStatus, language: enumLanguage),
                        EmploymentStatuses = enums.GetApiItems(CreditStandardEnumTypeCode.Employment, language: enumLanguage),
                        HousingTypes = enums.GetApiItems(CreditStandardEnumTypeCode.HousingType, language: enumLanguage),
                        OtherLoanTypes = enums.GetApiItems(CreditStandardEnumTypeCode.OtherLoanType, language: enumLanguage)
                    },
                    CurrentInitialCreditDecision = currentInitialDecision,
                    CurrentFinalCreditDecision = currentFinalDecision,
                    Settings = new Response.SettingsModel
                    {
                        //NOTE: This is a really sketchy temporary solution to let us keep prototyping this while deploying but not having it effect anything unless ucbv is active
                        //      When this feature is more solid this should be a feature toggle instead.
                        IsPropertyValuationActive = NTechEnvironment.Instance.StaticResourceFile(
                            "ntech.creditreport.ucbvse.settingsfile",
                            "uc-bv-se-settings.txt", false).Exists
                    },
                    CustomerPagesApplicationsUrl = NEnv.ServiceRegistry.External.ServiceUrl(
                        "nCustomerPages",
                        "login/eid",
                        Tuple.Create("targetName", "ApplicationsOverview")).ToString(),
                };
            }
        }

        private void AppendRecommendations(PreCreditContext context, params Response.CreditDecisionModel[] models)
        {
            var modelsWithKeys = models
                .Where(x => x != null)
                .SelectMany(x => x.CreditDecisionItems.Where(y => y.ItemName == "recommendationKeyValueItemKey").Select(y => new
                {
                    Model = x,
                    Key = y.Value
                }))
                .ToList();
            if (modelsWithKeys.Count == 0)
                return;

            var keys = modelsWithKeys.Select(x => x.Key).ToList();

            var jsonByKey = context
                .KeyValueItems
                .Where(x => x.KeySpace == MortgageLoanStandardCreditRecommendationModel.KeyValueStoreKeySpace && keys.Contains(x.Key))
                .Select(x => new { x.Key, x.Value })
                .ToDictionary(x => x.Key, x => x.Value);

            foreach (var mk in modelsWithKeys)
            {
                var json = jsonByKey.Opt(mk.Key);
                if (json != null)
                {
                    mk.Model.Recommendation = MortgageLoanStandardCreditRecommendationModel.ParseJson(json);
                }
            }
        }

        public class Response
        {
            public string ApplicationNr { get; set; }
            public ApplicationInfoModel ApplicationInfo { get; set; }
            public int NrOfApplicants { get; set; }
            public Dictionary<int, int> CustomerIdByApplicantNr { get; set; }
            public Dictionary<int, ApplicantInfoModel> ApplicantInfoByApplicantNr { get; set; }
            public Dictionary<int, List<string>> AllConnectedCustomerIdsWithRoles { get; set; }
            public WorkflowModel CurrentWorkflowModel { get; set; }
            public int ApplicationWorkflowVersion { get; set; }
            public EnumsModel Enums { get; set; }
            public List<ComplexApplicationListItemBase> ComplexListItems { get; set; }

            public CreditDecisionModel CurrentInitialCreditDecision { get; set; }
            public CreditDecisionModel CurrentFinalCreditDecision { get; set; }

            public SettingsModel Settings { get; set; }
            public string CustomerPagesApplicationsUrl { get; set; }

            public class EnumsModel
            {
                public List<EnumItemApiModel> CivilStatuses { get; set; }
                public List<EnumItemApiModel> EmploymentStatuses { get; set; }
                public List<EnumItemApiModel> HousingTypes { get; set; }
                public List<EnumItemApiModel> OtherLoanTypes { get; set; }
            }
            public class CreditDecisionModel
            {
                public bool IsFinal { get; set; }
                public bool IsCurrent { get; set; }
                public List<CreditDecisionItemModel> CreditDecisionItems { get; set; }

                public MortgageLoanStandardCreditRecommendationModel Recommendation { get; set; }
            }
            public class CreditDecisionItemModel
            {
                public string ItemName { get; set; }
                public string Value { get; set; }
                public bool IsRepeatable { get; set; }
            }
            public class SettingsModel
            {
                public bool IsPropertyValuationActive { get; set; }
            }
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

        }
    }
}