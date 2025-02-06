using nPreCredit.Code.Services.Comments;
using NTech.Core.PreCredit.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class ApplicationCommentService : IApplicationCommentServiceComposable
    {
        private IServiceRegistryUrlService urlService;
        private readonly NTech.Core.Module.Shared.Clients.IDocumentClient documentClient;
        private readonly IPreCreditContextFactoryService preCreditContextFactoryService;
        private readonly IUserDisplayNameService userDisplayNameService;

        public ApplicationCommentService(IPreCreditContextFactoryService preCreditContextFactoryService, IUserDisplayNameService userDisplayNameService,
            IServiceRegistryUrlService urlService, NTech.Core.Module.Shared.Clients.IDocumentClient documentClient)
        {
            this.urlService = urlService;
            this.documentClient = documentClient;
            this.preCreditContextFactoryService = preCreditContextFactoryService;
            this.userDisplayNameService = userDisplayNameService;
        }

        public bool TryAddComment(string applicationNr, string commentText, string eventType, CommentAttachment attachment, out string failedMessage, Action<CreditApplicationCommentModel> observeCreatedComment = null)
        {
            using (var context = preCreditContextFactoryService.CreateExtended())
            {
                var result = TryAddCommentComposable(applicationNr, commentText, eventType, attachment, out failedMessage, context,
                    observeCreatedComment: observeCreatedComment);
                context.SaveChanges();

                return result;
            }
        }

        public bool TryAddCommentComposable(string applicationNr, string commentText, string eventType, CommentAttachment attachment, out string failedMessage, IPreCreditContextExtended context, Action<CreditApplicationCommentModel> observeCreatedComment = null)
        {
            if (!ApplicationCommentHelper.TryCreateCommentWithAttachment(applicationNr, commentText, eventType, attachment, documentClient, context, out failedMessage, out var createdComment))
                return false;

            if (observeCreatedComment != null)
                observeCreatedComment?.Invoke(ToCommentModel(createdComment));

            context.AddCreditApplicationComments(createdComment);

            failedMessage = null;

            return true;
        }

        public List<CreditApplicationCommentModel> FetchCommentsForApplication(string applicationNr, List<string> hideTheseEventTypes = null, List<string> showOnlyTheseEventTypes = null)
        {
            using (var context = preCreditContextFactoryService.CreateExtended())
            {
                var q = context
                    .CreditApplicationCommentsQueryable
                    .Where(x => x.ApplicationNr == applicationNr);

                if (hideTheseEventTypes != null && hideTheseEventTypes.Count > 0)
                    q = q.Where(x => !hideTheseEventTypes.Contains(x.EventType));

                if (showOnlyTheseEventTypes != null && showOnlyTheseEventTypes.Count > 0)
                    q = q.Where(x => showOnlyTheseEventTypes.Contains(x.EventType));

                return q
                    .ToList()
                    .OrderByDescending(x => x.Id)
                    .Select(ToCommentModel)
                    .ToList();
            }
        }

        public CreditApplicationCommentModel FetchSingle(int commentId)
        {
            using (var context = preCreditContextFactoryService.CreateExtended())
            {
                return ToCommentModel(context
                    .CreditApplicationCommentsQueryable
                    .Single(x => x.Id == commentId));
            }
        }

        private CreditApplicationCommentModel ToCommentModel(CreditApplicationComment c)
        {
            var a = CreditApplicationCommentAttachmentStorageModel.Deserialize(c.Attachment);
            var attachementType = a?.GetAttachmentType();

            return new CreditApplicationCommentModel
            {
                Id = c.Id,
                AttachmentFilename = attachementType == CreditApplicationCommentAttachmentTypeCode.singleFile ? a.filename : null,
                AttachmentUrl = attachementType == CreditApplicationCommentAttachmentTypeCode.singleFile ? urlService.ArchiveDocumentUrl(a.archiveKey).ToString() : null,
                CommentByName = userDisplayNameService.GetUserDisplayNameByUserId(c.CommentById.ToString()),
                CommentDate = c.CommentDate,
                CommentText = c.CommentText,
                DirectUrlShortName = attachementType == CreditApplicationCommentAttachmentTypeCode.singleUrl ? a.urlShortName : null,
                DirectUrl = attachementType == CreditApplicationCommentAttachmentTypeCode.singleUrl ? a.url : null,
                RequestIpAddress = a?.requestIpAddress,
                BankAccountPdfSummaryArchiveKey = attachementType == CreditApplicationCommentAttachmentTypeCode.sharedBankAccountData ? a.sharedBankAccountDataPdfSummaryArchiveKey : null,
                BankAccountRawJsonDataArchiveKey = attachementType == CreditApplicationCommentAttachmentTypeCode.sharedBankAccountData ? a.sharedBankAccountDataRawJsonDataArchiveKey : null,
                CustomerSecureMessageId = attachementType == CreditApplicationCommentAttachmentTypeCode.customerSecureMessage ? a.customerSecureMessageId : null
            };
        }
    }

    public class CreditApplicationCommentModel
    {
        public int Id { get; set; }
        public DateTimeOffset CommentDate { get; set; }
        public string CommentText { get; set; }
        public string AttachmentFilename { get; set; }
        public string AttachmentUrl { get; set; }
        public string CommentByName { get; set; }
        public string DirectUrlShortName { get; set; }
        public string DirectUrl { get; set; }
        public string RequestIpAddress { get; set; }
        public string BankAccountRawJsonDataArchiveKey { get; set; }
        public string BankAccountPdfSummaryArchiveKey { get; set; }
        public int? CustomerSecureMessageId { get; set; }
    }
    public interface IApplicationCommentService
    {
        bool TryAddComment(string applicationNr, string commentText, string eventType, CommentAttachment attachment, out string failedMessage, Action<CreditApplicationCommentModel> observeCreatedComment = null);

        List<CreditApplicationCommentModel> FetchCommentsForApplication(string applicationNr, List<string> hideTheseEventTypes = null, List<string> showOnlyTheseEventTypes = null);

        CreditApplicationCommentModel FetchSingle(int commentId);
    }

    public interface IApplicationCommentServiceComposable : IApplicationCommentService
    {
        bool TryAddCommentComposable(string applicationNr, string commentText, string eventType, CommentAttachment attachment, out string failedMessage, IPreCreditContextExtended context, Action<CreditApplicationCommentModel> observeCreatedComment = null);
    }
}