using Microsoft.AspNetCore.Mvc;
using NTech.Core.Customer.Shared.Services;

namespace NTech.Core.Customer.Controllers
{
    [ApiController]
    [NTechRequireFeatures(RequireFeaturesAll = new[] { CustomerCheckPointService.FeatureName })]
    public class CustomerCheckPointController : Controller
    {
        private readonly CustomerCheckPointService checkPointService;

        public CustomerCheckPointController(CustomerCheckPointService checkPointService)
        {
            this.checkPointService = checkPointService;
        }

        [HttpPost]
        [Route("Api/Customer/Checkpoint/Set-State-On-Customer")]
        public SetCheckpointStateResult SetCheckpointState(SetCheckpointStateRequest request) =>
            checkPointService.SetCheckpointState(request);

        [HttpPost]
        [Route("Api/Customer/Checkpoint/Fetch-ReasonText")]
        public FetchReasonTextResult FetchReasonText(FetchReasonTextRequest request) =>
            checkPointService.FetchReasonText(request);

        [HttpPost]
        [Route("Api/Customer/Checkpoint/Get-State-And-History-On-Customer")]
        public FetchStateAndHistoryForCustomerResult FetchStateAndHistoryForCustomer(FetchStateAndHistoryForCustomerRequest request) =>
            checkPointService.FetchStateAndHistoryForCustomer(request);

        [HttpPost]
        [Route("Api/Customer/Checkpoint/Get-Active-On-Customers")]
        public GetActiveCheckPointIdsOnCustomerIdsResult GetActiveCheckPointIdsOnCustomerIds(GetActiveCheckPointIdsOnCustomerIdsRequest request) =>
            checkPointService.GetActiveCheckPointIdsOnCustomerIds(request);

        [HttpPost]
        [Route("Api/Customer/Checkpoint/Bulk-Insert-Checkpoints")]
        public ActionResult BulkInsertCheckpoints(BulkInsertCheckpointsRequest request)
        {
            checkPointService.BulkInsertCheckpoints(request);
            return Ok();
        }
    }
}
