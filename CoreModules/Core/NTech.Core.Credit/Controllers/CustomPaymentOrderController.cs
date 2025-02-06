using Microsoft.AspNetCore.Mvc;
using NTech.Core.Credit.Shared.Services;

namespace NTech.Core.Credit.Controllers
{
    [ApiController]
    [NTechRequireFeatures(RequireFeaturesAll = new[] { "ntech.feature.customcosts" })]
    public class CustomPaymentOrderController : Controller
    {
        private readonly CustomCostTypeService costTypeService;
        private readonly PaymentOrderService paymentOrderService;

        public CustomPaymentOrderController(CustomCostTypeService costTypeService, PaymentOrderService paymentOrderService)
        {
            this.costTypeService = costTypeService;
            this.paymentOrderService = paymentOrderService;
        }

        [HttpPost]
        [Route("Api/Credit/CustomCosts/All")]
        public List<CustomCost> AllCustomCosts() => costTypeService.GetCustomCosts();

        [HttpPost]
        [Route("Api/Credit/CustomCosts/SetCosts")]
        public ActionResult SetCosts(List<CustomCost> costs)
        {
            costTypeService.SetCosts(costs);
            return Ok();
        }

        [HttpPost]
        [Route("Api/Credit/PaymentOrder/SetOrder")]
        public ActionResult SetPaymentOrder(List<PaymentOrderItem> orderItems)
        {
            paymentOrderService.SetOrder(orderItems);
            return Ok();
        }
    }
}
