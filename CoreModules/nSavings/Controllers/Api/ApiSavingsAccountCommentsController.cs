using Newtonsoft.Json;
using nSavings.Code;
using NTech.Services.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechApi]
    [RoutePrefix("Api/SavingsAccountComment")]
    public class ApiCreditCommentsController : NController
    {
        public class CreateCommentRequest
        {
            public string SavingsAccountNr { get; set; }
            public string CommentText { get; set; }
        }

        [HttpPost]
        [Route("Create")]
        public ActionResult Create(string savingsAccountNr, string commentText, string eventType, bool? dontReturnComment, string attachedFileAsDataUrl, string attachedFileName, int? customerSecureMessageId)
        {
            if (string.IsNullOrWhiteSpace(savingsAccountNr) || string.IsNullOrWhiteSpace(commentText))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing savingsAccountNr or commentText");

            string attachedFileArchiveDocumentKey = null;
            string mimeType = null;
            if (!string.IsNullOrWhiteSpace(attachedFileAsDataUrl) && !string.IsNullOrWhiteSpace(attachedFileName))
            {
                byte[] fileData;
                if (!TryParseDataUrl(attachedFileAsDataUrl, out mimeType, out fileData))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid attached file");
                }
                var client = new DocumentClient();
                attachedFileArchiveDocumentKey = client.ArchiveStore(fileData, mimeType, attachedFileName);
            }

            var attachment = attachedFileArchiveDocumentKey == null
                ? null : new { archiveKey = attachedFileArchiveDocumentKey, filename = attachedFileName, mimeType = mimeType };

            var now = Clock.Now;
            using (var context = new SavingsContext())
            {
                var c = new SavingsAccountComment
                {
                    SavingsAccountNr = savingsAccountNr,
                    EventType = eventType ?? "UserComment",
                    CommentText = commentText,
                    ChangedById = CurrentUserId,
                    ChangedDate = now,
                    CommentById = CurrentUserId,
                    CommentDate = now,
                    InformationMetaData = InformationMetadata,
                    Attachment = JsonConvert.SerializeObject(attachment),
                    CustomerSecureMessageId = customerSecureMessageId
                };
                context.SavingsAccountComments.Add(c);

                context.SaveChanges();

                if (dontReturnComment ?? false)
                {
                    return Json2(new { Id = c.Id });
                }
                else
                {
                    var userClient = new Code.UserClient();
                    return Json2(new
                    {
                        Id = c.Id,
                        comment = new
                        {
                            CommentDate = c.CommentDate,
                            CommentText = c.CommentText,
                            ArchiveLinks = AttachmentArchiveLinks(c.Attachment),
                            DisplayUserName = userClient.GetUserDisplayNameByUserId(c.CommentById.ToString()),
                            CustomerSecureMessageId = customerSecureMessageId
                        }
                    });
                }
            }
        }

        private List<string> AttachmentArchiveLinks(string attachment)
        {
            if (attachment == null)
                return null;
            var item = JsonConvert.DeserializeAnonymousType(attachment, new { archiveKey = "", filename = "", mimeType = "", archiveKeys = new string[] { } });
            if (item == null)
                return null;
            else if (item.archiveKeys != null)
                return item.archiveKeys.Select(x => Url.Action("ArchiveDocument", "ApiArchiveDocument", new { key = x })).ToList();
            else if (item.archiveKey != null)
                return new List<string>() { Url.Action("ArchiveDocument", "ApiArchiveDocument", new { key = item.archiveKey }) };
            else
                return null;
        }

        [Route("LoadForSavingsAccount")]
        public ActionResult LoadForSavingsAccount(string savingsAccountNr, List<string> excludeTheseEventTypes, List<string> onlyTheseEventTypes)
        {
            if (string.IsNullOrWhiteSpace(savingsAccountNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing savingsAccountNr");
            var now = Clock.Now;

            var userClient = new Code.UserClient();
            using (var context = new SavingsContext())
            {
                var pre = context
                    .SavingsAccountComments
                    .Where(x => x.SavingsAccountNr == savingsAccountNr);

                if (excludeTheseEventTypes != null && excludeTheseEventTypes.Count > 0)
                    pre = pre.Where(x => !excludeTheseEventTypes.Contains(x.EventType));

                if (onlyTheseEventTypes != null && onlyTheseEventTypes.Count > 0)
                    pre = pre.Where(x => onlyTheseEventTypes.Contains(x.EventType));

                var comments = pre
                    .OrderByDescending(x => x.CommentDate)
                    .ThenByDescending(x => x.Timestamp)
                    .Select(x => new
                    {
                        x.CommentDate,
                        x.CommentText,
                        x.Attachment,
                        x.CommentById,
                        x.CustomerSecureMessageId
                    })
                    .ToList()
                    .Select(x => new
                    {
                        x.CommentDate,
                        x.CommentText,
                        ArchiveLinks = AttachmentArchiveLinks(x.Attachment),
                        DisplayUserName = userClient.GetUserDisplayNameByUserId(x.CommentById.ToString()),
                        CustomerSecureMessageId = x.CustomerSecureMessageId
                    })
                    .ToList();

                return Json2(comments);
            }
        }
    }
}