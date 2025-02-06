using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech.Core.PreCredit.Shared.Services;
using NTech.Services.Infrastructure.CreditStandard;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using static nPreCredit.Code.StandardCreditRecommendationModelBase;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class FetchUnsecuredLoansApplicationInitialDataMethod : TypedWebserviceMethod<FetchUnsecuredLoansApplicationInitialDataMethod.Request, FetchUnsecuredLoansApplicationInitialDataMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/FetchApplicationInitialData";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var applicationInfoService = requestContext.Resolver().Resolve<ApplicationInfoService>();

            var info = applicationInfoService.GetApplicationInfo(request.ApplicationNr, true);
            if (info == null)
                return Error("No such application exists", errorCode: "noSuchApplicationExists");

            var applicants = applicationInfoService.GetApplicationApplicants(request.ApplicationNr);

            using (var context = requestContext.Resolver().Resolve<PreCreditContextFactoryService>().CreateExtendedConcrete())
            {
                var application = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == request.ApplicationNr)
                    .Select(x => new
                    {
                        Documents = x.Documents.Where(y => !y.RemovedByUserId.HasValue).Select(y => new Response.DocumentModel { Id = y.Id, DocumentType = y.DocumentType, ApplicantNr = y.ApplicantNr, CustomerId = y.CustomerId, DocumentArchiveKey = y.DocumentArchiveKey, DocumentSubType = y.DocumentSubType }),
                        ComplexListItems = x.ComplexApplicationListItems.OrderBy(y => y.Id).Select(y => new ComplexApplicationListItemBase { ListName = y.ListName, Nr = y.Nr, ItemName = y.ItemName, ItemValue = y.ItemValue, IsRepeatable = y.IsRepeatable }),
                        CurrentDecision = x.CurrentCreditDecision                        
                    })
                    .Single();

                //TODO: Should we cache this?
                var currentReferenceInterestRatePercent = requestContext.Resolver().Resolve<IReferenceInterestRateService>().GetCurrent();

                var currentCreditDecisionItems = application.CurrentDecision == null
                        ? new List<Response.CreditDecisionItemModel>()
                        : application.CurrentDecision.DecisionItems.OrderBy(y => y.Id).Select(y => new Response.CreditDecisionItemModel
                        {
                            ItemName = y.ItemName,
                            Value = y.Value,
                            IsRepeatable = y.IsRepeatable
                        }).ToList();

                UnsecuredLoanStandardCreditRecommendationModel recommendation = null;
                var recommendationKeyValueItemKey = currentCreditDecisionItems.FirstOrDefault(x => x.ItemName == "recommendationKeyValueItemKey")?.Value;
                if (recommendationKeyValueItemKey != null)
                {
                    var recommendationRaw = KeyValueStoreService.GetValueComposable(context, recommendationKeyValueItemKey, UnsecuredLoanStandardCreditRecommendationModel.KeyValueStoreKeySpace);
                    if (recommendationRaw != null)
                        recommendation = UnsecuredLoanStandardCreditRecommendationModel.ParseJson(recommendationRaw);
                }

                var complexListItems = application.ComplexListItems.ToList();

                string preScoreResultId = complexListItems
                    .Where(x => x.ListName == "Application" && x.Nr == 1 && x.ItemName == "preScoreResultId" && !x.IsRepeatable)
                    .Select(x => x.ItemValue).SingleOrDefault();
                UnsecuredLoanStandardPreScoreRecommendationModel preScoreRecommendation = null;
                if (preScoreResultId != null)
                {
                    var presScoreRaw = KeyValueStoreService.GetValueComposable(context, preScoreResultId, KeyValueStoreKeySpaceCode.WebApplicationPreScoreResult.ToString());
                    if(presScoreRaw != null)
                    {
                        preScoreRecommendation = StoredPreScoreResult.Deserialize<UnsecuredLoanStandardPreScoreRecommendationModel>(presScoreRaw);
                        if (preScoreRecommendation != null && preScoreRecommendation.PolicyFilterResult != null)
                            preScoreRecommendation.PolicyFilterDetailsDisplayItems = CreateRuleDisplayItems(preScoreRecommendation.PolicyFilterResult,
                                NEnv.ClientCfg.Country.BaseCountry, NEnv.ClientCfg.Country.GetBaseLanguage());
                    }
                }

                return new Response
                {
                    ApplicationNr = request.ApplicationNr,
                    ApplicationWorkflowVersion = int.Parse(info.WorkflowVersion),
                    CurrentWorkflowModel = NEnv.UnsecuredLoanStandardWorkflow,
                    ApplicationInfo = info,
                    CustomerIdByApplicantNr = applicants.CustomerIdByApplicantNr,
                    ApplicantInfoByApplicantNr = applicants.ApplicantInfoByApplicantNr,
                    AllConnectedCustomerIdsWithRoles = applicants.AllConnectedCustomerIdsWithRoles.ToDictionary(x => x.Key, x => x.Value.ToList()),
                    NrOfApplicants = applicants.NrOfApplicants,
                    Documents = application.Documents.ToList(),
                    CurrentCreditDecisionItems = currentCreditDecisionItems,
                    CurrentReferenceInterestRatePercent = currentReferenceInterestRatePercent,
                    ComplexListItems = complexListItems,
                    Enums = CreditStandardEnumService.Instance.GetApiEnums(language: NEnv.ClientCfg.Country.GetBaseLanguage()),
                    CustomerPagesApplicationsUrl = NEnv.ServiceRegistry.External.ServiceUrl(
                        "nCustomerPages",
                        "login/eid",
                        Tuple.Create("targetName", "ApplicationsOverview")).ToString(),
                    CurrentCreditDecisionRecommendation = recommendation,
                    PreScoreRecommendation = preScoreRecommendation
                };
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
            public EnumsApiModel Enums { get; set; }
            public List<DocumentModel> Documents { get; set; }
            public List<ComplexApplicationListItemBase> ComplexListItems { get; set; }
            public List<CreditDecisionItemModel> CurrentCreditDecisionItems { get; set; }
            public decimal CurrentReferenceInterestRatePercent { get; set; }
            public string CustomerPagesApplicationsUrl { get; set; }
            public UnsecuredLoanStandardCreditRecommendationModel CurrentCreditDecisionRecommendation { get; set; }
            public UnsecuredLoanStandardPreScoreRecommendationModel PreScoreRecommendation { get; set; }

            public class DocumentModel
            {
                public int Id { get; set; }
                public string DocumentType { get; set; }
                public int? ApplicantNr { get; set; }
                public int? CustomerId { get; set; }
                public string DocumentArchiveKey { get; set; }
                public string DocumentSubType { get; set; }
            }

            public class CreditDecisionItemModel
            {
                public string ItemName { get; set; }
                public string Value { get; set; }
                public bool IsRepeatable { get; set; }
            }
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
        }

        public class UnsecuredLoanStandardPreScoreRecommendationModel : StoredPreScoreResult
        {
            public List<PolicyFilterDetailsDisplayItem> PolicyFilterDetailsDisplayItems { get; set; }
        }
    }
}