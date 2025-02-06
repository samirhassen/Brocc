using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class SearchLoanStandardByOmniValueMethod : TypedWebserviceMethod<SearchLoanStandardByOmniValueMethod.Request, SearchLoanStandardByOmniValueMethod.Response>
    {
        public override string Path => "LoanStandard/Search/ByOmniValue";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled || NEnv.IsStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var s = requestContext.Resolver().Resolve<LoanStandardApplicationSearchService>();

            var applications = s.Search(request.OmniSearchValue, request.ForceShowUserHiddenItems.GetValueOrDefault());

            var providerNames = NEnv.GetAffiliateModels().ToDictionary(x => x.ProviderName, x => x.DisplayToEnduserName);
            applications.ForEach(a => a.ProviderName = providerNames[a.ProviderName]);

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
            public List<LoanStandardApplicationSearchHit> Applications { get; set; }
        }
    }
}