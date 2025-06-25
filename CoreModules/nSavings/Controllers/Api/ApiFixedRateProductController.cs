using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using nSavings.Service;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Api;

[NTechApi]
[RoutePrefix("Api/FixedRateProduct")]
public class ApiFixedRateProductController : NController
{
    [HttpGet]
    [Route("GetActiveProducts")]
    public async Task<ActionResult> GetActiveProducts()
    {
        using var service = new FixedAccountProductService();
        var products = await service.GetActiveProductsAt(DateTime.Now);
        return Json2(products);
    }
}