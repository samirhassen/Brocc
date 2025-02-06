using Newtonsoft.Json;
using System;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;

namespace nDataWarehouse.Controllers
{
    public class SpaHostController : NController
    {
        private const string RoutePrefix = "Ui/S";

        public ActionResult Handle()
        {
            ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.GetEncoding("iso-8859-1").GetBytes(JsonConvert.SerializeObject(new
            {
                isTest = !NEnv.IsProduction,
                translation = GetTranslations(),
                spaHostUrlPrefix = Url.Action("Handle", "SpaHost"),
                treatNotificationsAsClosedMaxBalance = NEnv.TreatNotificationsAsClosedMaxBalance,
                backofficeUrl = new Uri(NEnv.ServiceRegistry.External["nBackoffice"]).ToString()
            })));
            return View();
        }

        public static void SetupRouting(RouteCollection routes)
        {
            routes.MapRoute(
                name: "SpaHost",
                url: RoutePrefix + "/{*path}",
                defaults: new { controller = "SpaHost", action = "Handle" });
        }
    }
}