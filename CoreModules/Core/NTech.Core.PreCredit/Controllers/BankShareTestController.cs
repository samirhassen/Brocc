using Microsoft.AspNetCore.Mvc;
using NTech.Core.PreCredit.Shared.Services;

namespace NTech.Core.PreCredit.Controllers
{
    [ApiController]
    public class BankShareTestController: Controller
    {
        private readonly BankShareTestService bankShareTestService;

        public BankShareTestController(BankShareTestService bankShareTestService)
        {
            this.bankShareTestService = bankShareTestService;
        }

        [Route("Api/PreCredit/BankShareTest/Settings")]
        [HttpPost]
        public BankShareTestSettingsResponse Settings(BankShareTestSettingsRequest request) => bankShareTestService.GetSettings(request);

        [Route("Api/PreCredit/BankShareTest/Poll")]
        [HttpPost]
        public Task<BankShareTestPollingResponse> Poll(BankShareTestPollingRequest request) => bankShareTestService.Poll(request);
    }
}
