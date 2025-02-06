using nCredit;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;

namespace NTech.Core.Credit.Shared.Services
{
    public class CreditCommentService
    {
        private readonly ICoreClock clock;
        private readonly CreditContextFactory creditContextFactory;
        private readonly IDocumentClient documentClient;

        public CreditCommentService(ICoreClock clock, CreditContextFactory creditContextFactory, IDocumentClient documentClient)
        {
            this.clock = clock;
            this.creditContextFactory = creditContextFactory;
            this.documentClient = documentClient;
        }

        public CreditComment CreateComment(string creditNr, string commentText, string eventType, string attachedFileAsDataUrl, string attachedFileName, int? customerSecureMessageId)
        {
            if (string.IsNullOrWhiteSpace(creditNr) || string.IsNullOrWhiteSpace(commentText))
                throw new NTechCoreWebserviceException("Missing creditNr or commentText") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            var attachment = new CreditCommentAttachmentModel();
            if (!string.IsNullOrWhiteSpace(attachedFileAsDataUrl) && !string.IsNullOrWhiteSpace(attachedFileName))
            {
                byte[] fileData;
                if (!FileUtilities.TryParseDataUrl(attachedFileAsDataUrl, out var mimeType, out fileData))
                {
                    throw new NTechCoreWebserviceException("Invalid attached file") { IsUserFacing = true, ErrorHttpStatusCode = 400 };
                }
                attachment.archiveKey = documentClient.ArchiveStore(fileData, mimeType, attachedFileName);
                attachment.mimeType = mimeType;
                attachment.filename = attachedFileName;
            }
            attachment.customerSecureMessageId = customerSecureMessageId;

            var now = clock.Now;
            using (var context = creditContextFactory.CreateContext())
            {
                var c = new CreditComment
                {
                    CreditNr = creditNr,
                    EventType = eventType ?? "UserComment",
                    CommentText = commentText,
                    ChangedById = context.CurrentUser.UserId,
                    ChangedDate = now,
                    CommentById = context.CurrentUser.UserId,
                    CommentDate = now,
                    InformationMetaData = context.CurrentUser.InformationMetadata,
                    Attachment = attachment?.Serialize(),
                };
                context.AddCreditComment(c);

                context.SaveChanges();

                return c;
            }
        }
    }
}