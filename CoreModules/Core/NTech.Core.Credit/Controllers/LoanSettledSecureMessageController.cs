using Microsoft.AspNetCore.Mvc;
using NTech.Core.Credit.Shared.Services;

namespace NTech.Core.Credit.Controllers
{
    [ApiController]
    public class LoanSettledSecureMessageController : Controller
    {
        private readonly LoanSettledSecureMessageService service;

        public LoanSettledSecureMessageController(LoanSettledSecureMessageService service)
        {
            this.service = service;
        }

        /// <summary>
        /// Scheduled task to send secure messages to customers with recently settled loans.
        /// </summary>
        [HttpPost]
        [Route("Api/Credit/SendLoanSettledSecureMessages")]
        public LoanSettledSecureMessageResponse SendLoanSettledSecureMessages(LoanSettledSecureMessageRequest request) => service.SendLoanSettledSecureMessages(request);
    }
}