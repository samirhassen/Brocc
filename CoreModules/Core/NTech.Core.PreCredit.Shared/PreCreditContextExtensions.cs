
using nPreCredit.Code.Services;
using NTech.Core.PreCredit.Shared;

namespace nPreCredit
{
    public static class PreCreditContextExtensions
    {
        public static CreditApplicationComment CreateAndAddComment(this IPreCreditContextExtended source, string commentText, string eventType, string applicationNr = null, CreditApplicationHeader creditApplicationHeader = null)
        {
            var comment = ApplicationCommentHelper.CreateComment(commentText, eventType, source,
                    applicationNr: applicationNr,
                    creditApplicationHeader: creditApplicationHeader);
            source.AddCreditApplicationComments(comment);
            return comment;
        }

        public static CreditApplicationDocumentHeader CreateAndAddApplicationDocument(this IPreCreditContextExtended source, string archiveKey, string filename, CreditApplicationDocumentTypeCode documentType, string applicationNr = null, CreditApplicationHeader creditApplicationHeader = null, int? applicantNr = null, int? customerId = null, string documentSubType = null)
        {
            var d = new CreditApplicationDocumentHeader
            {
                ApplicationNr = applicationNr,
                CreditApplication = creditApplicationHeader,
                ApplicantNr = applicantNr,
                CustomerId = customerId,
                DocumentArchiveKey = archiveKey,
                DocumentFileName = filename,
                DocumentType = documentType.ToString(),
                DocumentSubType = documentSubType,
                AddedByUserId = source.CurrentUserId,
                AddedDate = source.CoreClock.Now
            };
            source.FillInfrastructureFields(d);
            source.AddCreditApplicationDocumentHeaders(d);
            return d;
        }
    }
}
