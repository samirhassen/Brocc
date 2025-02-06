using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class SearchCompanyLoanByOmniValueMethod : TypedWebserviceMethod<SearchCompanyLoanByOmniValueMethod.Request, SearchCompanyLoanByOmniValueMethod.Response>
    {
        public override string Path => "CompanyLoan/Search/ByOmniValue";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var s = requestContext.Resolver().Resolve<ICompanyLoanApplicationSearchService>();

            var applications = s.Search(request.OmniSearchValue, request.ForceShowUserHiddenItems.GetValueOrDefault());

            return new Response
            {
                Applications = applications
            };
        }

        public class Request
        {
            [Required]
            public string OmniSearchValue { get; set; }
            public bool? ForceShowUserHiddenItems { get; set; }
        }

        public class Response
        {
            public List<CompanyLoanApplicationSearchHit> Applications { get; set; }
        }
    }
}