using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Net;
using nGccCustomerApplication.Code;

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
    }
}