using nGccCustomerApplication.Code;
using nGccCustomerApplication.Code.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace nGccCustomerApplication.Controllers.Login
{
    public class MockEidLoginController : BaseController
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (NEnv.IsProduction)
            {
                filterContext.Result = HttpNotFound();
            }

            base.OnActionExecuting(filterContext);
        }

        [Route("mock-eid/{sessionId}/login")]
        [HttpGet]
        [PreventBackButton]
        public ActionResult Login(string sessionId)
        {
            ViewBag.SessionId = sessionId;
            return View();
        }

        [Route("mock-eid/authenticate")]
        [HttpPost]
        public ActionResult Authenticate()
        {
            var client = new SystemUserCustomerClient();

            var session = client.GetElectronicIdAuthenticationSession(this.Request.Form["sessionId"]);
            if (session == null || session.IsClosed || session.ProviderName != "mock")
                return Content("No such session");

            return Redirect(session.CustomData.Opt("standardReturnUrl"));
        }
    }
}