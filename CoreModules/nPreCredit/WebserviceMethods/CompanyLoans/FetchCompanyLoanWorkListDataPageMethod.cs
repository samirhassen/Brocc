using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class FetchCompanyLoanWorkListDataPageMethod : TypedWebserviceMethod<FetchCompanyLoanWorkListDataPageMethod.Request, FetchCompanyLoanWorkListDataPageMethod.Response>
    {
        public override string Path => "CompanyLoan/Search/WorkListDataPage";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var s = requestContext.Resolver().Resolve<ICompanyLoanApplicationSearchService>();

            var result = s.GetDataPage(
                request.ListName,
                request.ProviderName,
                request.ForceShowUserHiddenItems.GetValueOrDefault(),
                request.ZeroBasedPageNr.Value, request.PageSize.Value,
                request.IncludeListCounts.GetValueOrDefault());

            return new Response
            {
                PageApplications = result.PageApplications,
                CurrentPageNr = result.CurrentPageNr,
                TotalNrOfPages = result.TotalNrOfPages,
                ListCountsByName = result.ListCountsByName
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
            public bool? ForceShowUserHiddenItems { get; set; }
            public bool? IncludeListCounts { get; set; }
        }

        public class Response
        {
            public List<CompanyLoanApplicationSearchHit> PageApplications { get; set; }
            public Dictionary<string, int> ListCountsByName { get; set; }
            public int CurrentPageNr { get; set; }
            public int TotalNrOfPages { get; set; }
        }
    }
}