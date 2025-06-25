using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Security;
using System.Linq;
using nGccCustomerApplication.Code;
using System.Dynamic;
using System.Threading;
using System.Web;
using System.Text;
using Serilog;


namespace nGccCustomerApplication.Controllers
{
    [CustomerPagesAuthorize(AllowEmptyRole = true)]
    public class ApplicationWrapperLinkController : NController
    {
        [Route("application-wrapper-link")]
        public ActionResult Index(string id)
        {
            if(string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Error", "Common");
            }

            return RedirectToAction("Index", "ApplicationWrapperDirect", new { token = id });
        }
        
        [Route("application-wrapper-link-active")]
        public ActionResult ApplicationActive()
        {
            return View();
        }

        [Route("application-wrapper-link-closed")]
        public ActionResult ApplicationClosed()
        {
            return View();
        }
    }
}
