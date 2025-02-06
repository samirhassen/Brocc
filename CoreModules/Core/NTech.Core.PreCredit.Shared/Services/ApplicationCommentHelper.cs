using nPreCredit.Code.Services.Comments;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.PreCredit.Shared;
using System;

namespace nPreCredit.Code.Services
{
    //Temporary logic share to not have to move all of the comment logic to core at once.
    public class ApplicationCommentHelper
    {
        public static bool TryCreateCommentWithAttachment(string applicationNr, string commentText, string eventType, CommentAttachment attachment, IDocumentClient documentClient, IPreCreditContextExtended context, out string failedMessage, out CreditApplicationComment createdComment)
        {
            createdComment = null;
            CreditApplicationCommentAttachmentStorageModel attachmentModel;
            if (!TryCreateAttachmentModel(attachment, documentClient, out attachmentModel, out failedMessage))
            {
                return false;
            }

            var c = CreateComment(commentText?.Trim(), string.IsNullOrWhiteSpace(eventType) ? "UserComment" : eventType.Trim(), context, applicationNr: applicationNr);
            c.Attachment = attachmentModel?.Serialize();

            failedMessage = null;
            createdComment = c;

            return true;
        }

        public static CreditApplicationComment CreateComment(string commentText, string eventType, IPreCreditContextExtended context, string applicationNr = null, CreditApplicationHeader creditApplicationHeader = null)
        {
            var c = new CreditApplicationComment
            {
                ApplicationNr = applicationNr,
                CreditApplication = creditApplicationHeader,
                CommentDate = context.CoreClock.Now,
                CommentById = context.CurrentUserId,
                CommentText = CreditApplicationComment.CleanCommentText(commentText),
                EventType = eventType
            };
            context.FillInfrastructureFields(c);
            return c;
        }

        private static bool TryCreateAttachmentModel(CommentAttachment i, IDocumentClient documentClient, out CreditApplicationCommentAttachmentStorageModel attachment, out string failedMessage)
        {
            failedMessage = null;
            attachment = null;

            if (i == null)
                return true;

            if (i as MetadataOnlyCommentAttachment != null)
            {
                attachment = CreditApplicationCommentAttachmentStorageModel.CreateMetadataOnly((MetadataOnlyCommentAttachment)i);
                return true;
            }

            Lazy<CreditApplicationCommentAttachmentStorageModel> a = new Lazy<CreditApplicationCommentAttachmentStorageModel>(() => new CreditApplicationCommentAttachmentStorageModel());
            string localFailedMessage = null;

            void DoOnAttachmentType<T>(Action<T> doOnType) where T : CommentAttachment
            {
                var derivedType = i as T;
                if (derivedType == null)
                    return;
                doOnType(derivedType);
            }

            DoOnAttachmentType<DataUrlFileCommentAttchment>(x =>
            {
                byte[] fileData;
                string mimeType;
                if (!FileUtilities.TryParseDataUrl(x.AttachedFileAsDataUrl, out mimeType, out fileData))
                {
                    localFailedMessage = "Invalid attached file";
                }
                string archiveKey;
                try
                {
                    archiveKey = documentClient.ArchiveStore(fileData, mimeType, x.AttachedFileName);
                }
                catch (NTechCoreWebserviceException ex)
                {
                    if (ex?.ErrorCode == "fileTypeNotAllowed")
                    {
                        localFailedMessage = ex.ErrorCode;
                        return;
                    }
                    throw;
                }

                a.Value.attachmentType = CreditApplicationCommentAttachmentTypeCode.singleFile.ToString();
                a.Value.filename = x.AttachedFileName;
                a.Value.mimeType = mimeType;
                a.Value.archiveKey = archiveKey;
            });

            DoOnAttachmentType<ArchiveFileCommentAttachment>(x =>
            {
                a.Value.attachmentType = CreditApplicationCommentAttachmentTypeCode.singleFile.ToString();
                a.Value.filename = x.AttachedFileName;
                a.Value.mimeType = x.MimeType;
                a.Value.archiveKey = x.ArchiveKey;
            });

            DoOnAttachmentType<UrlCommentAttachment>(x =>
            {
                a.Value.attachmentType = CreditApplicationCommentAttachmentTypeCode.singleUrl.ToString();
                a.Value.url = x.Url;
                a.Value.urlShortName = x.UrlShortName;
            });

            DoOnAttachmentType<SharedBankAccountDataAttachment>(x =>
            {
                a.Value.attachmentType = CreditApplicationCommentAttachmentTypeCode.sharedBankAccountData.ToString();
                a.Value.sharedBankAccountDataRawJsonDataArchiveKey = x.RawJsonDataArchiveKey;
                a.Value.sharedBankAccountDataPdfSummaryArchiveKey = x.PdfSummaryArchiveKey;
            });

            DoOnAttachmentType<SecureMessageCommentAttachment>(x =>
            {
                a.Value.attachmentType = CreditApplicationCommentAttachmentTypeCode.customerSecureMessage.ToString();
                a.Value.customerSecureMessageId = x.CustomerSecureMessageId;
            });

            if (localFailedMessage != null)
            {
                failedMessage = localFailedMessage;
                return false;
            }

            if (!string.IsNullOrWhiteSpace(i.RequestIpAddress))
            {
                if (!a.IsValueCreated)
                    a.Value.attachmentType = CreditApplicationCommentAttachmentTypeCode.metadataOnly.ToString();
                a.Value.requestIpAddress = i.RequestIpAddress;
            }

            if (a.IsValueCreated)
                attachment = a.Value;
            else
                attachment = null;

            failedMessage = null;

            return true;
        }

    }
}