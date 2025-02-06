using Microsoft.AspNetCore.Mvc;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Infrastructure;
using System.ComponentModel.DataAnnotations;

namespace NTech.Core.Credit.Controllers
{
    [ApiController]
    [NTechRequireFeatures(RequireFeaturesAll = new[] { "ntech.feature.paymentplan" })]
    public class AlternatePaymentPlanController : Controller
    {
        private readonly AlternatePaymentPlanService service;
        private readonly CreditContextFactory contextFactory;

        public AlternatePaymentPlanController(AlternatePaymentPlanService service, CreditContextFactory contextFactory)
        {
            this.service = service;
            this.contextFactory = contextFactory;
        }

        [HttpPost]
        [Route("Api/Credit/AlternatePaymentPlan/Credit-State")]
        public CreditAlternatePaymentPlanState State(GetPaymentPlanStateRequest request)
        {
            using var context = contextFactory.CreateContext();
            return service.GetPaymentPlanState(request.CreditNr, context);
        }

        [HttpPost]
        [Route("Api/Credit/AlternatePaymentPlan/Get-Suggested")]
        public AlternatePaymentPlanSpecification GetSuggested(GetPaymentPlanSuggestedRequest request)
        {
            return service.GetSuggestedPaymentPlan(request); 
        }

        [HttpPost]
        [Route("Api/Credit/AlternatePaymentPlan/Start")]
        public StartPaymentPlanResponse Start(ValidateOrStartAlternatePaymentPlanRequest request)
        {
            return service.StartPaymentPlan(request);
        }
        
        [HttpPost]
        [Route("Api/Credit/AlternatePaymentPlan/Validate")]
        public ValidatePaymentPlanResponse Validate(ValidateOrStartAlternatePaymentPlanRequest request)
        {
            var isValid = service.IsPaymentPlanValid(request.CreditNr, request.PaymentPlan.Count, out string failedMessage, paymentPlan: request.PaymentPlan);
            return new ValidatePaymentPlanResponse
            {
                IsValid = isValid,
                ErrorMessage = failedMessage
            };
        }

        [HttpPost]
        [Route("Api/Credit/AlternatePaymentPlan/Cancel")]
        public ActionResult Cancel(CancelPaymentPlanRequest request)
        {
            using var context = contextFactory.CreateContext();
            service.CancelPaymentPlan(context, request.CreditNr, request.IsManualCancel.GetValueOrDefault());

            return Ok();
        }
    }

    public class GetPaymentPlanStateRequest
    {
        [Required]
        public string CreditNr { get; set; }
    }

    public class CancelPaymentPlanRequest
    {
        [Required]
        public string CreditNr { get; set; }
        public bool? IsManualCancel { get; set; }
    }
}
