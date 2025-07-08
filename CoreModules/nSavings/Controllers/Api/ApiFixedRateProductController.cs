using System;
using System.Net;
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
        var products = await service.GetActiveProductsAt(Clock.Today);
        return Json2(products);
    }

    [HttpGet]
    [Route("{productId}")]
    public async Task<ActionResult> GetProduct(string productId)
    {
        Guid? prodIdGuid = Guid.TryParse(productId, out var guid) ? guid : null;
        if (prodIdGuid == null)
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Product id not specified or wrong format");
        using var service = new FixedAccountProductService();
        var product = await service.GetProduct(prodIdGuid!.Value);
        return Json2(product);
    }
}