using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Mvc;
using nSavings.DbModel;
using nSavings.DbModel.BusinessEvents;
using nSavings.DbModel.DbException;
using nSavings.Service;
using nSavings.ViewModel.FixedRateProduct;
using nSavings.ViewModel.FixedRateProduct.Common;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Ui;
#nullable enable

[NTechAuthorizeSavingsHigh]
public class FixedRateProductManagementController : NController
{
    [HttpGet]
    [Route("Ui/FixedRateProductManagement")]
    public async Task<ActionResult> Index()
    {
        using var savingsContext = new SavingsContext();
        var faService = new FixedAccountProductService(savingsContext);
        var products = await faService.GetAllProducts();
        var model = new FixedRateProductManagementViewModel
        {
            FutureProducts = [],
            ActiveProducts = [],
            HistoricalProducts = [],
            AuditLog = await faService.GetAuditLog()
        };

        foreach (var product in products)
        {
            if (product.ValidTo < Clock.Now)
            {
                model.HistoricalProducts.Add(product);
                continue;
            }

            if (product.ResponseStatus == ResponseStatus.Approved && product.ValidFrom < DateTime.Now)
            {
                model.ActiveProducts.Add(product);
                continue;
            }

            model.FutureProducts.Add(product);
        }

        return View(model);
    }

    [HttpGet]
    [Route("Ui/FixedRateProductManagement/CreateProductView")]
    public ActionResult CreateProductView()
    {
        return PartialView("_CreateProduct", new ProductViewModel
        {
            ValidFrom = DateTime.Now
        });
    }

    [HttpGet]
    [Route("Ui/FixedRateProductManagement/{id}/edit")]
    public async Task<ActionResult> EditProductView(string id)
    {
        using var savingsContext = new SavingsContext();
        var faService = new FixedAccountProductService(savingsContext);

        var guid = Guid.Parse(id);
        var product = await faService.GetProduct(guid);
        if (product == null)
        {
            return new HttpStatusCodeResult(400, $"Product with id {id} was not found");
        }

        return PartialView("_CreateProduct", product);
    }

    [HttpGet]
    [Route("Ui/FixedRateProductManagement/{id}/respond/{response}")]
    public async Task<ActionResult> RespondToProductView(string id, string response)
    {
        var guid = Guid.Parse(id);

        var approved = response.EqualsIgnoreCase("approve");
        if (!approved && !response.EqualsIgnoreCase("reject"))
        {
            return new HttpStatusCodeResult(400, "Invalid response, must be one of ['approve', 'deny']");
        }

        using var eventManager = new FixedAccountProductBusinessEventManager(CurrentUserId, InformationMetadata, Clock);

        await eventManager.RespondToProductProposal(guid, approved, User.Identity.Name);

        return new HttpStatusCodeResult(200);
    }

    //[HttpDelete]
    //[Route("Ui/FixedRateProductManagement/{id}/edit")]
    //public ActionResult DeleteProductView(string id)
    //{
    //    var guid = Guid.Parse(id);

    //    return PartialView("_CreateProduct", Products.Single(x => x.Id == guid));
    //}

    [HttpPost]
    [Route("Ui/FixedRateProductManagement/CreateProduct")]
    public async Task<ActionResult> CreateProduct(ProductViewModel? model)
    {
        model ??= ParseFromForm<ProductViewModel>();

        var name = User.Identity.Name;
        model.Id = Guid.NewGuid();
        model.ResponseStatus = ResponseStatus.Pending;
        model.ApprovedBy = null;

        using var eventManager = new FixedAccountProductBusinessEventManager(CurrentUserId, InformationMetadata, Clock);

        await eventManager.AddProduct(model, name);

        return RedirectToAction("Index");
    }

    [HttpPost]
    [Route("Ui/FixedRateProductManagement/Update")]
    public async Task<ActionResult> UpdateProduct(ProductViewModel? model)
    {
        var newProduct = model ?? ParseFromForm<ProductViewModel>();

        if (Request.Form["id"] == null)
        {
            return new HttpStatusCodeResult(400, "No id was provided");
        }

        newProduct.Id = Guid.Parse(Request.Form["id"]);

        try
        {
            using var eventManager =
                new FixedAccountProductBusinessEventManager(CurrentUserId, InformationMetadata, Clock);
            await eventManager.UpdateProduct(newProduct, User.Identity.Name);
        }
        catch (EntityNotFoundException ex)
        {
            return new HttpStatusCodeResult(400, ex.Message);
        }

        return RedirectToAction("Index");
    }


    private T ParseFromForm<T>() where T : new()
    {
        var t = typeof(T);
        var res = new T();

        foreach (var name in Request.Form.AllKeys)
        {
            var pi = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (pi == null || !pi.CanWrite) continue;
            try
            {
                var val = Request.Form[name];
                if (val == null) continue;

                var nullableType = Nullable.GetUnderlyingType(pi.PropertyType);
                if (nullableType != null)
                {
                    var conv = Convert.ChangeType(val, nullableType, CultureInfo.InvariantCulture);
                    pi.SetValue(res, conv, null);
                }
                else
                {
                    var conv = Convert.ChangeType(val, pi.PropertyType, CultureInfo.InvariantCulture);
                    pi.SetValue(res, conv, null);
                }
            }
            catch
            {
                // Ignored, we'll just leave the value as null for validation to handle instead
            }
        }

        return res;
    }
}