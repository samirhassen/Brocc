using nCustomerPages.Code;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Web.Mvc;
using System.Web.Routing;

namespace nCustomerPages.Controllers
{
    public class SignatureLandingPageController : BaseController
    {
        [AllowAnonymous]
        [Route("signature-result/success")]
        public ActionResult Success()
        {
            ViewBag.HideHeader = true;
            ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
            {
                translation = BaseController.GetTranslationsShared(this.Url, this.Request)
            })));
            return View("Success");
        }

        [AllowAnonymous]
        [Route("signature-result/{localSessionId}/success")]
        public ActionResult SuccessWithId(string localSessionId)
        {
            if (!string.IsNullOrWhiteSpace(localSessionId))
            {
                var client = new SystemUserCustomerClient();
                var session = client.GetSignatureSessionByLocalSessionId(localSessionId);
                if (session.HaveAnySigned() == true && session.RedirectAfterSuccessUrl != null)
                    return Redirect(session.RedirectAfterSuccessUrl);
            }

            //Fallback to the old static display page
            return Success();
        }

        [AllowAnonymous]
        [Route("signature-result-redirect/{localSessionId}/success")]
        public ActionResult RedirectSuccessWithId(string localSessionId)
        {
            if (!string.IsNullOrWhiteSpace(localSessionId))
            {
                var client = new SystemUserCustomerClient();
                var session = client.GetSignatureSessionByLocalSessionId(localSessionId);

                if (session.HaveAnySigned() == true)
                {
                    return Redirect(NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages", "signature-result/success").ToString());
                }
            }

            //Fallback to the old static display page
            return Success();
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("signature-result/failure")]
        public ActionResult Failure()
        {
            ViewBag.HideHeader = true;
            ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
            {
                translation = BaseController.GetTranslationsShared(this.Url, this.Request)
            })));
            return View("Failure");
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("signature-result/{localSessionId}/failure")]
        public ActionResult FailureWithId(string localSessionId)
        {
            if (!string.IsNullOrWhiteSpace(localSessionId))
            {
                var client = new SystemUserCustomerClient();
                var session = client.GetSignatureSessionByLocalSessionId(localSessionId);
                if (session?.IsFailed() == true && session.RedirectAfterFailedUrl != null)
                    return Redirect(session.RedirectAfterFailedUrl);
            }

            //Fallback to the old static display page
            return Failure();
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("signature-result-redirect/{localSessionId}/failure")]
        public ActionResult RedirectFailureWithId(string localSessionId)
        {
            if (!string.IsNullOrWhiteSpace(localSessionId))
            {
                var client = new SystemUserCustomerClient();
                var session = client.GetSignatureSessionByLocalSessionId(localSessionId);
                if (session?.IsFailed() == true)
                {
                    return Redirect(NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages", "signature-result/failure").ToString());
                }
            }

            //Fallback to the old static display page
            return Failure();
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("api/signature/{localSessionId}/signal-event")]
        public ActionResult ReceiveProviderEvent(string localSessionId)
        {
            if (localSessionId != null)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    var client = new SystemUserCustomerClient();
                    client.HandleProviderSignatureEvent(new Dictionary<string, string>
                    {
                        { "localSessionId", localSessionId }
                    });
                });
            }
            return Success();
        }
    }
}