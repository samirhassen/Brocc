using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class FetchSearchLoanStandardWorkListDataPageMethod : TypedWebserviceMethod<FetchSearchLoanStandardWorkListDataPageMethod.Request, FetchSearchLoanStandardWorkListDataPageMethod.Response>
    {
        public override string Path => "LoanStandard/Search/WorkListDataPage";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled || NEnv.IsStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var searchService = requestContext.Resolver().Resolve<LoanStandardApplicationSearchService>();
            var handlerService = requestContext.Resolver().Resolve<IApplicationAssignedHandlerService>();
            var toHandlerResponse = HandlerResponseModel.CreateFactory(requestContext.Resolver().Resolve<IUserDisplayNameService>());

            var result = searchService.GetDataPage(
                request.ListName,
                request.ProviderName,
                request.AssignedHandler,
                request.ForceShowUserHiddenItems.GetValueOrDefault(),
                request.ZeroBasedPageNr.Value, request.PageSize.Value,
                request.IncludeListCounts.GetValueOrDefault());


            var assignableCount = searchService.GetAssignabeCount(request.ForceShowUserHiddenItems.GetValueOrDefault());

            WorkflowModel workflowModel = null;
            if (request.IncludeWorkflowModel.GetValueOrDefault())
            {
                workflowModel = NEnv.IsStandardMortgageLoansEnabled ? NEnv.MortgageLoanStandardWorkflow : NEnv.UnsecuredLoanStandardWorkflow;
            }

            return new Response
            {
                PageApplications = result.PageApplications,
                CurrentPageNr = result.CurrentPageNr,
                TotalNrOfPages = result.TotalNrOfPages,
                ListCountsByName = result.ListCountsByName,
                CurrentWorkflowModel = workflowModel,
                ProviderDisplayNameByName = request.IncludeProviders.GetValueOrDefault()
                    ? NEnv.GetAffiliateModels().ToDictionary(x => x.ProviderName, x => x.DisplayToEnduserName) : null,
                AssignedHandlerDisplayNameByUserId = toHandlerResponse(handlerService.GetPossibleHandlerUserIds()).ToDictionary(x => x.UserId.ToString(), x => x.UserDisplayName),
                AssignableCount = assignableCount
            };
        }

        public class Request
        {
            [Required]
            public int? PageSize { get; set; }
            [Required]
            public int? ZeroBasedPageNr { get; set; }

            public string ListName { get; set; }
            public string ProviderName { get; set; }
            public ApplicationAssignedHandlerModel AssignedHandler { get; set; }
            public bool? ForceShowUserHiddenItems { get; set; }
            public bool? IncludeListCounts { get; set; }
            public bool? IncludeProviders { get; set; }
            public bool? IncludeWorkflowModel { get; set; }
        }

        public class Response
        {
            public List<LoanStandardApplicationSearchHit> PageApplications { get; set; }
            public Dictionary<string, int> ListCountsByName { get; set; }
            public int CurrentPageNr { get; set; }
            public int TotalNrOfPages { get; set; }
            public int AssignableCount { get; set; }
            public WorkflowModel CurrentWorkflowModel { get; set; }
            public Dictionary<string, string> ProviderDisplayNameByName { get; set; }
            public Dictionary<string, string> AssignedHandlerDisplayNameByUserId { get; set; }
        }
    }
}