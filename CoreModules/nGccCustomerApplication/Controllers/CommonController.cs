using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Net;
using nGccCustomerApplication.Code;
using Newtonsoft.Json;
using System.Text;

namespace nGccCustomerApplication.Controllers
{
    public class CommonController : NController
    {
        [AllowAnonymous]
        [Route("hb")]
        public ActionResult Hb()
        {
            var a = System.Reflection.Assembly.GetExecutingAssembly();
            return Json(new
            {
                status = "ok",
                name = a.GetName().Name,
                build = System.Reflection.AssemblyName.GetAssemblyName(a.Location).Version.ToString()
            }, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        [Route("error")]
        public ActionResult Error()
        {
            return View();
        }

        [AllowAnonymous]
        [Route("translation")]
        public ActionResult Translation(string lang)
        {
            var translation = new BalanziaApplicationTranslation();
            var t = Translations.FetchTranslation(lang);
            if (t != null)
            {
                return Json2(t);
            }
            else
            {
                return HttpNotFound();
            }
        }

        [AllowAnonymous]
        [Route("access-denied")]
        public ActionResult AccessDenied(bool? isTokenExpired)
        {
            ViewBag.HideHeader = true;
            ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
            {
                translation = BaseController.GetTranslationsShared(this.Url, this.Request)
            })));
            ViewBag.IsTokenExpired = isTokenExpired ?? false;
            ViewBag.ShowLogin = !(Session == null || (Session != null && Session["EidSignatureCustomerApplicationTarget"] == null));
            ViewBag.EidSignatureCustomerTarget = Session != null && Session["EidSignatureCustomerApplicationTarget"] != null ? Session["EidSignatureCustomerApplicationTarget"].ToString() : "";
            if (NEnv.IsStandardEmbeddedGccCustomerApplicationEnabled)
            {
                ViewBag.Message = "Du har loggat ut, saknar rättigheter eller har inga tjänster hos oss.";
                return View("EmbedddedCustomerPagesSimpleMessage");
            }
            else
            {
                return View();
            }
        }
    }
}