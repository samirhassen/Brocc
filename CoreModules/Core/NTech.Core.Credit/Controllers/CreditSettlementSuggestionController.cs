using Microsoft.AspNetCore.Mvc;
using nCredit.DbModel.BusinessEvents;
using System.Net;

namespace NTech.Core.Credit.Controllers
{
    [ApiController]
    public class CreditSettlementSuggestionController : Controller
    {
        private readonly CreditSettlementSuggestionBusinessEventManager settlementManager;

        public CreditSettlementSuggestionController(CreditSettlementSuggestionBusinessEventManager settlementManager)
        {
            this.settlementManager = settlementManager;
        }

        [HttpPost]
        [Route("Api/Credit/SettlementSuggestion/ComputeSuggestion")]
        public ComputeCreditSuggestionResponse ComputeSuggestion(ComputeCreditSuggestionRequest request) =>        
            settlementManager.ComputeSuggestion(request);        

        [HttpPost]
        [Route("Api/Credit/SettlementSuggestion/CancelPendingSuggestion")]
        public CancelCreditSettlementSuggestionResponse CancelPendingSuggestion(CancelCreditSettlementSuggestionRequest request) =>
            settlementManager.CancelPendingSuggestion(request);        

        [HttpPost]
        [Route("Api/Credit/SettlementSuggestion/FetchInitialData")]
        public CreditSettlementFetchInitialDataResponse FetchInitialData(CreditSettlementFetchInitialDataRequest request) =>
            settlementManager.FetchInitialData(request);        

        [HttpPost]
        [Route("Api/Credit/SettlementSuggestion/CreateAndSendSuggestion")]
        public CreateAndSendCreditSettlementResponse CreateAndSendSuggestion(CreateAndSendCreditSettlementRequest request) =>
            settlementManager.CreateAndSendSuggestion(request);        
    }
}
