using NTech.Services.Infrastructure;
using System;
using System.Net;
using System.Web.Mvc;

namespace nCustomer.Controllers
{
    [NTechAuthorize]
    [RoutePrefix("Api/CustomerComments")]
    public class CustomerCommentsController : NController
    {
        [HttpPost()]
        [Route("FetchAllForCustomer")]
        public ActionResult FetchAllForCustomer(int customerId)
        {
            return Json2(Service.CustomerComment.FetchCommentsForCustomer(customerId));
        }

        [HttpPost()]
        [Route("Add")]
        public ActionResult AddComment(int customerId, string newCommentText, string eventType = null,
            string attachedFileAsDataUrl = null, string attachedFileName = null,
            string attachedDirectUrl = null, string attachedDirectUrlShortText = null)
        {
            Code.Services.CustomerCommentModel c;
            string msg;
            if (this.Service.CustomerComment.TryAddComment(
                customerId, newCommentText,
                this.GetCurrentUserMetadata(),
                out c, out msg,
                eventType: eventType,
                attachedFileDataUrlAndFileName: (!string.IsNullOrWhiteSpace(attachedFileAsDataUrl) && !string.IsNullOrWhiteSpace(attachedFileName))
                    ? Tuple.Create(attachedFileAsDataUrl, attachedFileName)
                    : null,
                attachedUrlShortNameAndUrl: (!string.IsNullOrWhiteSpace(attachedDirectUrl) && !string.IsNullOrWhiteSpace(attachedDirectUrlShortText))
                    ? Tuple.Create(attachedDirectUrl, attachedDirectUrlShortText)
                    : null))
                return Json2(c);
            else
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, msg);
        }
    }
}