using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class ApiCreditCommentsController : NController
    {
        public class CreateCommentRequest
        {
            public string CreditNr { get; set; }
            public string CommentText { get; set; }
        }

        [HttpPost]
        [Route("Api/CreditComment/Create")]
        public ActionResult Create(string creditNr, string commentText, string eventType, bool? dontReturnComment, string attachedFileAsDataUrl, string attachedFileName, int? customerSecureMessageId)
        {
            var service = new CreditCommentService(
                CoreClock.SharedInstance,
                new CreditContextFactory(() => new CreditContextExtended(GetCurrentUserMetadata(), CoreClock.SharedInstance)),
                LegacyServiceClientFactory.CreateDocumentClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry));

            var c = service.CreateComment(creditNr, commentText, eventType, attachedFileAsDataUrl, attachedFileName, customerSecureMessageId);

            if (dontReturnComment ?? false)
            {
                return Json2(new { Id = c.Id });
            }
            else
            {
                var userClient = new Code.UserClient();
                var a = CreditCommentAttachmentModel.Parse(c.Attachment);
                var archiveLinks = FormatAttachmentArchiveLinks(a, out var archiveLinkKeys);
                return Json2(new
                {
                    Id = c.Id,
                    comment = new
                    {
                        CommentDate = c.CommentDate,
                        CommentText = c.CommentText,
                        EventType = c.EventType,
                        ArchiveLinks = archiveLinks,
                        ArchiveLinkKeys = archiveLinkKeys,
                        DisplayUserName = userClient.GetUserDisplayNameByUserId(c.CommentById.ToString()),
                        CustomerSecureMessageId = a?.customerSecureMessageId
                    }
                });
            }
        }

        private List<string> FormatAttachmentArchiveLinks(CreditCommentAttachmentModel item, out List<string> archiveKeys)
        {
            archiveKeys = null;

            if (item == null)
                return null;
            else if (item.archiveKeys != null)
            {
                archiveKeys = item.archiveKeys.ToList();
                return item.archiveKeys.Select(x => Url.Action("ArchiveDocument", "ApiArchiveDocument", new { key = x })).ToList();
            }
            else if (item.archiveKey != null)
            {
                archiveKeys = new List<string> { item.archiveKey };
                return new List<string>() { Url.Action("ArchiveDocument", "ApiArchiveDocument", new { key = item.archiveKey }) };
            }
            else
                return null;
        }

        [HttpPost]
        [Route("Api/CreditComment/LoadForCredit")]
        public ActionResult LoadForCredit(string creditNr, List<string> excludeTheseEventTypes, List<string> onlyTheseEventTypes)
        {
            if (string.IsNullOrWhiteSpace(creditNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing creditNr");
            var now = Clock.Now;

            var userClient = new Code.UserClient();
            using (var context = new CreditContext())
            {
                var pre = context
                .CreditComments
                .Where(x => x.CreditNr == creditNr);

                if (excludeTheseEventTypes != null && excludeTheseEventTypes.Count > 0)
                    pre = pre.Where(x => !excludeTheseEventTypes.Contains(x.EventType));

                if (onlyTheseEventTypes != null && onlyTheseEventTypes.Count > 0)
                    pre = pre.Where(x => onlyTheseEventTypes.Contains(x.EventType));

                var comments = pre
                    .OrderByDescending(x => x.Id)
                    .Select(x => new
                    {
                        x.CommentDate,
                        x.CommentText,
                        x.Attachment,
                        x.CommentById,
                        x.EventType
                    })
                    .ToList()
                    .Select(x =>
                    {
                        var a = CreditCommentAttachmentModel.Parse(x.Attachment);
                        var archiveLinks = FormatAttachmentArchiveLinks(a, out var archiveLinkKeys);
                        return new
                        {
                            x.CommentDate,
                            x.CommentText,
                            x.EventType,
                            ArchiveLinks = archiveLinks,
                            ArchiveLinkKeys = archiveLinkKeys,
                            DisplayUserName = userClient.GetUserDisplayNameByUserId(x.CommentById.ToString()),
                            CustomerSecureMessageId = a?.customerSecureMessageId
                        };
                    })
                    .ToList();

                return Json2(comments);
            }
        }
    }
}