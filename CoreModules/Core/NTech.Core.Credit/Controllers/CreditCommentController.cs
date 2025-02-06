using Microsoft.AspNetCore.Mvc;
using NTech.Core.Credit.Shared.Services;

namespace NTech.Core.Credit.Controllers
{
    [ApiController]
    public class CreditCommentController : Controller
    {
        private readonly CreditCommentService commentService;

        public CreditCommentController(CreditCommentService commentService)
        {
            this.commentService = commentService;
        }

        [HttpPost]
        [Route("Api/Credit/Comment/Create")]
        public CreateCreditCommentResponse CreateComment(CreateCreditCommentRequest request)
        {
            var comment = commentService.CreateComment(request.CreditNr, request.CommentText, request.EventType,
                request.AttachedFileAsDataUrl, request.AttachedFileName, request.CustomerSecureMessageId);

            return new CreateCreditCommentResponse
            {
                CommentId = comment.Id
            };
        }
    }

    public class CreateCreditCommentRequest
    {
        public string CreditNr { get; set; }
        public string CommentText { get; set; }
        public string EventType { get; set; }
        public string AttachedFileAsDataUrl { get; set; }
        public string AttachedFileName { get; set; }
        public int? CustomerSecureMessageId { get; set; }
    }

    public class CreateCreditCommentResponse
    {
        public int CommentId { get; set; }
    }
}
