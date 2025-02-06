using Microsoft.AspNetCore.Mvc;
using nCredit.Code.Services;

namespace NTech.Core.PreCredit.Apis
{
    [ApiController]
    public class CreditSearchController : Controller
    {
        private readonly CreditSearchService creditSearchService;

        public CreditSearchController(CreditSearchService creditSearchService)
        {
            this.creditSearchService = creditSearchService;
        }

        [HttpPost]
        [Route("Api/Credit/Search")]
        public CreditSearchResult Search(SearchCreditRequest request) =>
            new CreditSearchResult
            {
                hits = creditSearchService.Search(request)
            };
    }


    public class CreditSearchResult
    {
        public List<SearchCreditHit> hits { get; set; }
    }
}
