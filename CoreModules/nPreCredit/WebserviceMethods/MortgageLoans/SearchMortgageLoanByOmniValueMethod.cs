using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static nPreCredit.Code.Services.MortgageLoanWorkListService;

namespace nPreCredit.WebserviceMethods.MortgageLoans
{
    public class SearchMortgageLoanByOmniValueMethod : TypedWebserviceMethod<SearchMortgageLoanByOmniValueMethod.Request, SearchMortgageLoanByOmniValueMethod.Response>
    {
        public override string Path => "MortgageLoan/Search/ByOmniValue";

        public override bool IsEnabled => NEnv.IsOnlyNonStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var s = requestContext.Resolver().Resolve<IMortgageLoanWorkListService>();

            SearchResultPage applicationsResult = null;
            SearchResultPage leadsResult = null;

            if (request.IncludeLeads.GetValueOrDefault())
            {
                leadsResult = s.Search(new MortgageLoanWorkListService.SearchFilter
                {
                    OmniSearchValue = request.OmniSearchValue,
                    PageNr = request.PageNr,
                    PageSize = request.PageSize
                }, true);
            }

            if (!request.SkipApplications.GetValueOrDefault())
            {
                applicationsResult = s.Search(new MortgageLoanWorkListService.SearchFilter
                {
                    OmniSearchValue = request.OmniSearchValue,
                    PageNr = request.PageNr,
                    PageSize = request.PageSize
                }, false);
            }

            if (leadsResult == null && applicationsResult == null)
                return Error("Must either IncludeLeads or not SkipApplications", errorCode: "invalidResultRequest");

            Response.FilterModel ToResultFilter(SearchResultPage p)
            {
                if (p == null)
                    return null;
                return new Response.FilterModel
                {
                    OmniSearchValue = p.Filter.OmniSearchValue,
                    PageNr = p.Filter.PageNr,
                    PageSize = p.Filter.PageSize
                };
            }

            var applicationsFilter = ToResultFilter(applicationsResult);

            return new Response
            {
                Applications = applicationsResult?.Applications,
                Leads = leadsResult?.Applications,
                Filter = applicationsFilter,
                ApplicationsFilter = applicationsFilter,
                LeadsFilter = ToResultFilter(leadsResult)
            };
        }

        public class Request
        {
            [Required]
            public string OmniSearchValue { get; set; }

            public bool? SkipApplications { get; set; }
            public bool? IncludeLeads { get; set; }

            public int? PageSize { get; set; }
            public int? PageNr { get; set; }
        }

        public class Response
        {
            public class FilterModel
            {
                public string OmniSearchValue { get; set; }

                public int? PageSize { get; set; }
                public int? PageNr { get; set; }
            }

            public List<ResultPage.Application> Applications { get; set; }
            public List<ResultPage.Application> Leads { get; set; }

            /// <summary>
            /// Kept for backwards compatibility. Same as ApplicationsFilter
            /// </summary>
            public FilterModel Filter { get; set; }
            public FilterModel LeadsFilter { get; set; }
            public FilterModel ApplicationsFilter { get; set; }
        }
    }
}