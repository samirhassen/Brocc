using nPreCredit.Code.Services;
using NTech.Services.Infrastructure;
using System.Collections.Generic;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api/ApplicationComments")]
    public class ApplicationCommentsController : NController
    {
        private readonly IApplicationCommentService applicationCommentService;

        public ApplicationCommentsController(IApplicationCommentService applicationCommentService)
        {
            this.applicationCommentService = applicationCommentService;
        }

        [HttpPost]
        [Route("Add")]
        public ActionResult Add(string applicationNr, string commentText, string eventType, string attachedFileAsDataUrl, string attachedFileName, int? customerSecureMessageId)
        {
            CreditApplicationCommentModel addedComment = null;
            string failedMessage;
            CommentAttachment a = null;
            if (!string.IsNullOrWhiteSpace(attachedFileAsDataUrl) && !string.IsNullOrWhiteSpace(attachedFileName))
                a = CommentAttachment.CreateFileFromDataUrl(attachedFileAsDataUrl, attachedFileName);
            if (customerSecureMessageId.HasValue)
                a = CommentAttachment.CreateWithSecureMessage(customerSecureMessageId.Value);
            if (applicationCommentService.TryAddComment(applicationNr, commentText, eventType, a, out failedMessage, observeCreatedComment: x => addedComment = x))
                return Json2(addedComment);
            else
                return ServiceError(failedMessage);
        }

        [HttpPost]
        [Route("FetchForApplication")]
        public ActionResult FetchForApplication(string applicationNr, List<string> hideTheseEventTypes = null, List<string> showOnlyTheseEventTypes = null)
        {
            return Json2(applicationCommentService.FetchCommentsForApplication(applicationNr, hideTheseEventTypes: hideTheseEventTypes, showOnlyTheseEventTypes: showOnlyTheseEventTypes));
        }

        [HttpPost]
        [Route("FetchSingle")]
        public ActionResult FetchSingle(int commentId)
        {
            return Json2(applicationCommentService.FetchSingle(commentId));
        }
    }
}