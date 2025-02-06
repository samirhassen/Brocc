using Microsoft.AspNetCore.Mvc;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.PreCredit.Shared.Services;

namespace NTech.Core.PreCredit.Controllers
{
    [ApiController]
    public class CreditApplicationItemController : Controller
    {
        private readonly CreditApplicationItemService creditApplicationItemService;

        public CreditApplicationItemController(CreditApplicationItemService creditApplicationItemService)
        {
            this.creditApplicationItemService = creditApplicationItemService;
        }

        [Route("Api/PreCredit/CreditApplicationItems/Bulk-Fetch")]
        [HttpPost]
        public Dictionary<string, Dictionary<string, Dictionary<string, string>>> BulkFetchCreditApplicationItems(BulkFetchCreditApplicationItemsRequest request) =>
            creditApplicationItemService.BulkFetchCreditApplicationItems(request);
    }
}
