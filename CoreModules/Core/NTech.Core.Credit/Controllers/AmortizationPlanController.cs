using Microsoft.AspNetCore.Mvc;
using NTech.Core.Credit.Shared.Services;
using System.ComponentModel.DataAnnotations;

namespace NTech.Core.Credit.Controllers
{
    [ApiController]
    public class AmortizationPlanController : Controller
    {
        private readonly AmortizationPlanService amortizationPlanService;

        public AmortizationPlanController(AmortizationPlanService amortizationPlanService)
        {
            this.amortizationPlanService = amortizationPlanService;
        }

        [HttpPost]
        [Route("Api/Credit/AmortizationPlan")]
        public AmortizationPlanUiModel AmortizationPlan(AmortizationPlanRequest request) => amortizationPlanService.GetAmortizationPlan(request.CreditNr);

        [HttpPost]
        [Route("Api/Credit/AddFuturePaymentFreeMonth")]
        public AmortizationPlanUiModel AddFuturePaymentFreeMonth(PaymentFreeMonthRequest request) =>
            amortizationPlanService.AddFuturePaymentFreeMonth(request.CreditNr, request.ForMonth, request.ReturningAmortizationPlan);

        [HttpPost]
        [Route("Api/Credit/CancelFuturePaymentFreeMonth")]
        public AmortizationPlanUiModel CancelFuturePaymentFreeMonth(PaymentFreeMonthRequest request) =>
            amortizationPlanService.CancelFuturePaymentFreeMonth(request.CreditNr, request.ForMonth, request.ReturningAmortizationPlan);
    }

    public class AmortizationPlanRequest
    {
        [Required]
        public string CreditNr { get; set; }
    }

    public class PaymentFreeMonthRequest
    {
        [Required]
        public string CreditNr { get; set; }
        [Required]
        public DateTime? ForMonth { get; set; }
        public bool? ReturningAmortizationPlan { get; set; }
    }
}
