using NTech.Services.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCustomer.Controllers
{
    [NTechAuthorizeKyc()]
    [RoutePrefix("Ui/KycManagement")]
    public class KycManagementController : NController
    {
        [HttpGet]
        [Route("Manage")]
        public ActionResult Manage(int? customerId)
        {
            if (!customerId.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");

            SetInitialData(new
            {
                customerId = customerId.Value,
                translation = this.GetTranslations()
            });
            return View();
        }

        [HttpGet]
        [Route("FatcaCrs")]
        public ActionResult FatcaCrs(int? customerId)
        {
            if (!customerId.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");

            SetInitialData(new
            {
                customerId = customerId.Value,
                translation = this.GetTranslations(),
                allCountryCodesAndNames = ISO3166.GetCountryCodesAndNames("en").ToDictionary(x => x.code, x => x.name)
            });
            return View();
        }
    }
}