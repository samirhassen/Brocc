using nPreCredit.Code.Services.Comments;

namespace nPreCredit.Code.Services
{
    public class CommentAttachment
    {
        public string RequestIpAddress { get; set; }

        public static MetadataOnlyCommentAttachment CreateMetadataOnly(string requestIpAddress = null)
        {
            return new MetadataOnlyCommentAttachment { RequestIpAddress = requestIpAddress };
        }

        public static CommentAttachment CreateFileFromDataUrl(string dataUrl, string attachedFileName, string requestIpAddress = null)
        {
            return new DataUrlFileCommentAttchment
            {
                AttachedFileAsDataUrl = dataUrl,
                AttachedFileName = attachedFileName,
                RequestIpAddress = requestIpAddress
            };
        }

        public static CommentAttachment CreateSharedBankAccountData(string rawJsonDataArchiveKey, string pdfSummaryArchiveKey, string requestIpAddress = null)
        {
            return new SharedBankAccountDataAttachment
            {
                PdfSummaryArchiveKey = pdfSummaryArchiveKey,
                RawJsonDataArchiveKey = rawJsonDataArchiveKey,
                RequestIpAddress = requestIpAddress
            };
        }

        public static CommentAttachment CreateFileFromArchiveKey(string archiveKey, string mimeType, string attachedFileName, string requestIpAddress = null)
        {
            return new ArchiveFileCommentAttachment
            {
                ArchiveKey = archiveKey,
                MimeType = mimeType,
                AttachedFileName = attachedFileName,
                RequestIpAddress = requestIpAddress
            };
        }

        public static CommentAttachment CreateWithUrl(string url, string urlShortName, string requestIpAddress = null)
        {
            return new UrlCommentAttachment
            {
                Url = url,
                UrlShortName = urlShortName,
                RequestIpAddress = requestIpAddress
            };
        }

        public static CommentAttachment CreateWithSecureMessage(int? customerSecureMessageId, string requestIpAddress = null)
        {
            return new SecureMessageCommentAttachment
            {
                CustomerSecureMessageId = customerSecureMessageId,
                RequestIpAddress = requestIpAddress
            };
        }
    }
}
namespace nPreCredit.Code.Services.Comments
{
    public class MetadataOnlyCommentAttachment : CommentAttachment
    {

    }

    public class DataUrlFileCommentAttchment : CommentAttachment
    {
        public string AttachedFileAsDataUrl { get; set; }
        public string AttachedFileName { get; set; }
    }

    public class ArchiveFileCommentAttachment : CommentAttachment
    {
        public string ArchiveKey { get; set; }
        public string MimeType { get; set; }
        public string AttachedFileName { get; set; }
    }

    public class UrlCommentAttachment : CommentAttachment
    {
        public string Url { get; set; }
        public string UrlShortName { get; set; }
    }

    public class SharedBankAccountDataAttachment : CommentAttachment
    {
        public string RawJsonDataArchiveKey { get; set; }
        public string PdfSummaryArchiveKey { get; set; }
    }

    public class SecureMessageCommentAttachment : CommentAttachment
    {
        public int? CustomerSecureMessageId { get; set; }
    }

    public enum CreditApplicationCommentAttachmentTypeCode
    {
        singleFile,
        singleUrl,
        metadataOnly,
        unknown,
        sharedBankAccountData,
        customerSecureMessage
    }
}
