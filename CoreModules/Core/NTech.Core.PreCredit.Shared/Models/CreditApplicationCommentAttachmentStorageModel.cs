using Newtonsoft.Json;
using NTech.Banking.Conversion;

namespace nPreCredit.Code.Services.Comments
{
    public class CreditApplicationCommentAttachmentStorageModel
    {
        public string attachmentType { get; set; }

        //File
        public string archiveKey { get; set; }

        public string filename { get; set; }
        public string mimeType { get; set; }

        //Urls
        public string urlShortName { get; set; }

        public string url { get; set; }

        //Metadata
        public string requestIpAddress { get; set; }

        //Shared account data
        public string sharedBankAccountDataRawJsonDataArchiveKey { get; set; }
        public string sharedBankAccountDataPdfSummaryArchiveKey { get; set; }

        public int? customerSecureMessageId { get; set; }

        public CreditApplicationCommentAttachmentTypeCode GetAttachmentType()
        {
            return Enums.Parse<CreditApplicationCommentAttachmentTypeCode>(attachmentType) ?? CreditApplicationCommentAttachmentTypeCode.singleFile;
        }

        public static CreditApplicationCommentAttachmentStorageModel CreateMetadataOnly(MetadataOnlyCommentAttachment attachment)
        {
            if (attachment == null)
                return null;

            return new CreditApplicationCommentAttachmentStorageModel
            {
                attachmentType = CreditApplicationCommentAttachmentTypeCode.metadataOnly.ToString(),
                requestIpAddress = attachment.RequestIpAddress
            };
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static CreditApplicationCommentAttachmentStorageModel Deserialize(string attachment)
        {
            if (attachment == null)
                return null;
            return JsonConvert.DeserializeObject<CreditApplicationCommentAttachmentStorageModel>(attachment);
        }
    }
}

