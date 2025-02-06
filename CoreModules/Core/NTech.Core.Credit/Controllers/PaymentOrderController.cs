using Microsoft.AspNetCore.Mvc;
using NTech.Core.Credit.Shared.Services;

namespace NTech.Core.Credit.Controllers
{
    [ApiController]
    public class PaymentOrderController : Controller
    {
        private readonly PaymentOrderService paymentOrderService;

        public PaymentOrderController(PaymentOrderService paymentOrderService)
        {
            this.paymentOrderService = paymentOrderService;
        }

        [HttpPost]
        [Route("Api/Credit/PaymentOrder/UiItems")]
        public List<PaymentOrderUiItem> UiItems()
        {
            return paymentOrderService.GetPaymentOrderUiItems();
        }
    }
}
