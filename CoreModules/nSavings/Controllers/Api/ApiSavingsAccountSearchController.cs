using System.Web.Mvc;
using nSavings.Code;
using nSavings.Code.Services;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;

namespace nSavings.Controllers.Api
{
    [NTechApi]
    public class ApiSavingsAccountSearchController : NController
    {
        [HttpPost]
        [Route("Api/SavingsAccount/Search")]
        public ActionResult Search(SavingsAccountSearchRequest request)
        {
            var customerClient =
                LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceHttpContextUser.SharedInstance,
                    NEnv.ServiceRegistry);
            var service = new SavingsAccountSearchService(customerClient);
            try
            {
                var result = service.Search(request);
                return Json2(new { hits = result });
            }
            catch (NTechWebserviceMethodException ex)
            {
                if (ex.IsUserFacing)
                {
                    return new HttpStatusCodeResult(ex.ErrorHttpStatusCode ?? 400, ex.Message);
                }

                throw;
            }
        }
    }
}