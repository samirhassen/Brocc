using Microsoft.AspNetCore.Mvc;
using nCredit.DbModel.BusinessEvents;
using NTech.Core.Credit.Shared.Services;

namespace NTech.Core.Credit.Controllers
{
    [ApiController]
    public class PaymentPlacementController
    {
        private readonly MultiCreditPlacePaymentBusinessEventManager placementManager;
        private readonly PaymentFileImportService paymentFileImportService;
        private readonly RepayPaymentBusinessEventManager repayPaymentManager;

        public PaymentPlacementController(MultiCreditPlacePaymentBusinessEventManager placementManager, PaymentFileImportService paymentFileImportService,
            RepayPaymentBusinessEventManager repayPaymentManager)
        {
            this.placementManager = placementManager;
            this.paymentFileImportService = paymentFileImportService;
            this.repayPaymentManager = repayPaymentManager;
        }

        [HttpPost]
        [Route("Api/Credit/PaymentPlacement/Placement-InitialData")]
        public PaymentPlacementInitialDataResponse PlacementInitialData(PaymentPlacementInitialDataRequest request) =>
            placementManager.GetPlacementInitialData(request);

        [HttpPost]
        [Route("Api/Credit/PaymentPlacement/Compute-PlacementSuggestion")]
        public PaymentPlacementSuggestionResponse ComputePlacementSuggestion(PaymentPlacementSuggestionRequest request) =>
            placementManager.ComputeMultiCreditPlacementInstruction(request);

        [HttpPost]
        [Route("Api/Credit/PaymentPlacement/Place-PlacementSuggestion")]
        public ActionResult PlacePlacementSuggestion(PaymentPlacementRequest request)
        {
            placementManager.PlaceFromUnplaced(request);
            return new OkResult();
        }

        [HttpPost]
        [Route("Api/Credit/PaymentPlacement/Find-PaymentPlacement-CreditNrs")]
        public FindPaymentPlacementCreditNrsResponse FindPaymentPlacementCreditNrs(FindPaymentPlacementCreditNrsRequest request) =>
            placementManager.FindPaymentPlacementCreditNrs(request);        

        [HttpPost]
        [Route("Api/Credit/PaymentPlacement/Import-PaymentFile")]
        public PaymentFileImportResponse ImportPaymentFile(PaymentFileImportRequest request) =>         
            paymentFileImportService.ImportFile(request);

        [HttpPost]
        [Route("Api/Credit/PaymentPlacement/PaymentFile-Data")]
        public PaymentFileFileDataResponse GetFileDataPaymentFileData(PaymentFileFileDataRequest request) =>
            paymentFileImportService.GetFileData(request);

        [HttpPost]
        [Route("Api/Credit/PaymentPlacement/Repay-UnplacedPayment")]
        public UnplacedCreditRepaymentResponse GetFileDataPaymentFileData(UnplacedCreditRepaymentRequest request) =>
            repayPaymentManager.RepayPayment(request);
    }
}