using nCustomerPages.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;

namespace nCustomerPages.Controllers
{
    public class MockEidSignatureController : BaseController
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (NEnv.IsProduction)
            {
                filterContext.Result = HttpNotFound();
            }

            base.OnActionExecuting(filterContext);
        }

        private NTech.ElectronicSignatures.CommonElectronicIdSignatureSession.SigningCustomer GetCustomerBySignerToken(NTech.ElectronicSignatures.CommonElectronicIdSignatureSession session, string signerToken) =>
            session.SigningCustomersBySignerNr.Values.Single(x => x.GetCustomDataOpt("signerToken") == signerToken);

        [Route("mock-eid/{signerToken}/sign")]
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Sign(string signerToken)
        {
            var client = new SystemUserCustomerClient();
            var session = client.GetSignatureSessioBySearchTerm("signerToken", signerToken);
            if (session == null)
                return Content("No such session exists"); //This seems super unrealistic

            
            if(session.SignatureProviderName?.ToLowerInvariant() != "mock" && session.GetCustomDataOpt("isMock") != "true")
                return Content("No such session exists");

            var customer = GetCustomerBySignerToken(session, signerToken);

            if (customer.SignedDateUtc.HasValue)
                return Redirect(session.RedirectAfterSuccessUrl);

            ViewBag.IsSessionClosed = session.ClosedDate.HasValue;

            ViewBag.SignerToken = customer.GetCustomDataOpt("signerToken");
            ViewBag.UnsignedDocumentUrl = Url.Action("ViewUnsignedDocument", "MockEidSignature", new { archiveKey = session.UnsignedPdf.ArchiveKey });

            return View();
        }

        [Route("mock-eid/do-sign")]
        [HttpPost]
        [AllowAnonymous]
        public ActionResult DoSign()
        {
            var client = new SystemUserCustomerClient();
            var eventData = new Dictionary<string, string>();
            var signerToken = Request.Form["signerToken"];
            if (!string.IsNullOrWhiteSpace(signerToken))
                eventData["signerToken"] = signerToken;
            if (Request.Form["hasSigned"]?.ToLowerInvariant() == "true")
                eventData["hasSigned"] = "true";

            var session = client.HandleProviderSignatureEvent(eventData);
            if (session == null)
                throw new Exception("No such session exists");

            //This is to emulate the strange things the signicat2 provider does
            string AddLocalSessionIdToRedirectUrl(string urlPattern)
            {
                if (!urlPattern.Contains("{localSessionId}"))
                {
                    throw new Exception("The callback must contain {localSessionId}.");
                }
                return urlPattern.Replace("{localSessionId}", session.ProviderSessionId);
            }

            var customer = GetCustomerBySignerToken(session, signerToken);
            return Redirect(customer?.SignedDateUtc != null ? AddLocalSessionIdToRedirectUrl(session.RedirectAfterSuccessUrl) : AddLocalSessionIdToRedirectUrl(session.RedirectAfterFailedUrl));
        }

        [Route("mock-eid/view-unsigned-document")]
        [HttpGet]
        [AllowAnonymous]
        public ActionResult ViewUnsignedDocument(string archiveKey)
        {
            var data = new SystemUserDocumentClient().FetchRawWithFilename(archiveKey, out var contentType, out var filename);
            return File(data, contentType);
        }
    }
}