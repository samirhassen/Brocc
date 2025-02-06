using nCustomer.DbModel;
using Newtonsoft.Json;
using NTech;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.Code.Services
{
    public class CustomerCommentService : ICustomerCommentService
    {
        private Func<string, string> getUserDisplayNameByUserId;
        private Func<CustomersContext> createContext;
        private readonly IClock clock;
        private readonly IUrlService urlService;

        public CustomerCommentService(Func<string, string> getUserDisplayNameByUserId, Func<CustomersContext> createContext, IClock clock, IUrlService urlService)
        {
            this.getUserDisplayNameByUserId = getUserDisplayNameByUserId;
            this.createContext = createContext;
            this.clock = clock;
            this.urlService = urlService;
        }

        public bool TryAddComment(int customerId, string commentText, NtechCurrentUserMetadata user, out CustomerCommentModel addedComment, out string failedMessage, string eventType = null, Tuple<string, string> attachedFileDataUrlAndFileName = null, Tuple<string, string> attachedUrlShortNameAndUrl = null)
        {
            string attachment = null;
            if (attachedFileDataUrlAndFileName != null)
            {
                var attachedFileAsDataUrl = attachedFileDataUrlAndFileName.Item1;
                var attachedFileName = attachedFileDataUrlAndFileName.Item2;
                byte[] fileData;
                string mimeType;
                if (!FileUtilities.TryParseDataUrl(attachedFileAsDataUrl, out mimeType, out fileData))
                {
                    addedComment = null;
                    failedMessage = "Invalid attached file";
                    return false;
                }
                var client = new DocumentClient();
                var attachedFileArchiveDocumentKey = client.ArchiveStore(fileData, mimeType, attachedFileName);
                attachment = FormatSingleFileCommentAttachment(attachedFileArchiveDocumentKey, attachedFileName, mimeType);
            }
            else if (attachedUrlShortNameAndUrl != null)
            {
                attachment = FormatSingleUrlCommentAttachment(attachedUrlShortNameAndUrl.Item1, attachedUrlShortNameAndUrl.Item2);
            }

            using (var context = createContext())
            {
                var c = new CustomerComment
                {
                    Attachment = attachment,
                    ChangedById = user.UserId,
                    ChangedDate = clock.Now,
                    CommentById = user.UserId,
                    CommentDate = clock.Now,
                    CommentText = commentText,
                    CustomerId = customerId,
                    EventType = eventType ?? "UserComment",
                    InformationMetaData = user.InformationMetadata
                };
                context.CustomerComments.Add(c);

                context.SaveChanges();

                addedComment = ToCommentModel(c);
                failedMessage = null;

                return true;
            }
        }

        private string FormatSingleFileCommentAttachment(string archiveKey, string filename, string mimeType)
        {
            return JsonConvert.SerializeObject(new { attachmentType = "singleFile", archiveKey, filename, mimeType });
        }

        private string FormatSingleUrlCommentAttachment(string urlShortName, string url)
        {
            return JsonConvert.SerializeObject(new { attachmentType = "singleUrl", urlShortName, url });
        }

        public List<CustomerCommentModel> FetchCommentsForCustomer(int customerId)
        {
            using (var context = createContext())
            {
                return context
                    .CustomerComments
                    .Where(x => x.CustomerId == customerId)
                    .ToList()
                    .OrderByDescending(x => x.CommentDate)
                    .ThenByDescending(x => x.Id)
                    .Select(ToCommentModel)
                    .ToList();
            }
        }

        public CustomerCommentModel FetchSingle(int commentId)
        {
            using (var context = createContext())
            {
                return ToCommentModel(context
                    .CustomerComments
                    .Single(x => x.Id == commentId));
            }
        }

        private CustomerCommentModel ToCommentModel(CustomerComment c)
        {
            var singleFile = FilenameAndArchiveLinkFromAttachment(c.Attachment);
            var singleUrl = UrlFromAttachment(c.Attachment);
            return new CustomerCommentModel
            {
                Id = c.Id,
                AttachmentFilename = singleFile?.Item1,
                AttachmentUrl = singleFile?.Item2,
                CommentByName = this.getUserDisplayNameByUserId(c.CommentById.ToString()),
                CommentDate = c.CommentDate,
                CommentText = c.CommentText,
                DirectUrlShortName = singleUrl?.Item1,
                DirectUrl = singleUrl?.Item2
            };
        }

        private Tuple<string, string> FilenameAndArchiveLinkFromAttachment(string attachment)
        {
            if (attachment == null)
                return null;
            var item = JsonConvert.DeserializeAnonymousType(attachment, new { archiveKey = "", filename = "", mimeType = "" });
            if (item == null || item.archiveKey == null || item.filename == null)
                return null;
            return Tuple.Create(item.filename, urlService.ArchiveDocumentUrl(item.archiveKey, false));
        }

        private Tuple<string, string> UrlFromAttachment(string attachment)
        {
            if (attachment == null)
                return null;

            var item = JsonConvert.DeserializeAnonymousType(attachment, new { attachmentType = "", urlShortName = "", url = "" });
            if (item == null || item.attachmentType == null || item.attachmentType != "singleUrl")
                return null;
            return Tuple.Create(item.urlShortName, item.url);
        }
    }

    public class CustomerCommentModel
    {
        public int Id { get; set; }
        public DateTimeOffset CommentDate { get; set; }
        public string CommentText { get; set; }
        public string AttachmentFilename { get; set; }
        public string AttachmentUrl { get; set; }
        public string CommentByName { get; set; }
        public string DirectUrlShortName { get; set; }
        public string DirectUrl { get; set; }
    }

    public interface ICustomerCommentService
    {
        bool TryAddComment(int customerId, string commentText, NtechCurrentUserMetadata user, out CustomerCommentModel addedComment, out string failedMessage, string eventType = null, Tuple<string, string> attachedFileDataUrlAndFileName = null, Tuple<string, string> attachedUrlShortNameAndUrl = null);
        List<CustomerCommentModel> FetchCommentsForCustomer(int customerId);
        CustomerCommentModel FetchSingle(int commentId);
    }
}