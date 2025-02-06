using NTech.Legacy.Module.Shared;
using nTest.Code;
using System;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace nTest.Controllers
{
    public class CommonController : Controller
    {
        [AllowAnonymous]
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

        [Route("")]
        public ActionResult StartPage()
        {
            return RedirectToAction("Index", "Main");
        }

        [AllowAnonymous]
        public ActionResult Error()
        {
            return View();
        }

        public ActionResult Logout()
        {
            this.HttpContext.GetOwinContext().Authentication.SignOut();
            return RedirectToAction("Loggedout");
        }

        [AllowAnonymous]
        public ActionResult Loggedout()
        {
            return View();
        }

        [Route("Setup")]
        [HttpGet()]
        public ActionResult Setup(bool? verifyDb = null, bool? clearCache = null)
        {
            bool? isDbValid = null;
            if (verifyDb ?? true)
            {
                try
                {
                    DbSingleton.SharedInstance.Db.ResetExistanceFlag();
                    TimeMachine.SharedInstance.Init();
                    using (var tr = DbSingleton.SharedInstance.Db.BeginTransaction())
                    {
                        var g = Guid.NewGuid().ToString();
                        tr.Get<string>(g, g); //Just make sure a call is actually made. We dont care about the result just that a connection can be made and such.
                    }
                    isDbValid = true;
                }
                catch
                {
                    isDbValid = false;
                }
            }

            bool? isCacheCleared = null;
            if (clearCache ?? true)
            {
                CacheHandler.ClearAllCaches();
                isCacheCleared = true;
            }

            return Json(new { isDbValid = isDbValid, isCacheCleared = isCacheCleared }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost()]
        [Route("Api/SleepForSeconds/{sleepSeconds}")]
        public ActionResult SleepForSeconds(int sleepSeconds)
        {
            Thread.Sleep(1000 * sleepSeconds);
            return Json(new { });
        }
    }
}