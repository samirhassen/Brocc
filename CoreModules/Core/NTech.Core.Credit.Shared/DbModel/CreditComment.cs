using Newtonsoft.Json;
using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit
{
    public class CreditComment : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public CreditHeader Credit { get; set; }
        public string CreditNr { get; set; }
        public string EventType { get; set; }
        public DateTimeOffset CommentDate { get; set; }
        public string Attachment { get; set; }
        public int CommentById { get; set; }
        public string CommentText { get; set; }
        public BusinessEvent CreatedByEvent { get; set; }
        public int? CreatedByEventId { get; set; }
    }

    public class CreditCommentAttachmentModel
    {
        public string archiveKey { get; set; }
        public string filename { get; set; }
        public string mimeType { get; set; }
        public List<string> archiveKeys { get; set; }
        public int? customerSecureMessageId { get; set; }
        public string rawData { get; set; }

        public T ParseRawData<T>() where T : class
            => rawData == null ? null : JsonConvert.DeserializeObject<T>(rawData);

        public static CreditCommentAttachmentModel ArchiveKeysOnly(List<string> archiveKeys)
        {
            return new CreditCommentAttachmentModel { archiveKeys = archiveKeys };
        }
        public static CreditCommentAttachmentModel ArchiveKeysOnly(params string[] archiveKeys)
        {
            return ArchiveKeysOnly(archiveKeys?.ToList());
        }
        public static CreditCommentAttachmentModel RawDataOnly<T>(T rawData) where T : class
        {
            return new CreditCommentAttachmentModel { rawData = JsonConvert.SerializeObject(rawData) };
        }

        public static CreditCommentAttachmentModel Parse(string attachment)
        {
            if (attachment == null)
                return new CreditCommentAttachmentModel();
            else
                return JsonConvert.DeserializeObject<CreditCommentAttachmentModel>(attachment);
        }

        public string Serialize()
        {
            if (archiveKeys == null && !customerSecureMessageId.HasValue && string.IsNullOrWhiteSpace(archiveKey) && string.IsNullOrWhiteSpace(filename) && string.IsNullOrWhiteSpace(mimeType) && rawData == null)
                return null;

            return JsonConvert.SerializeObject(this, NullIgnoringJsonSerializerSettings.Value);
        }


        private static Lazy<JsonSerializerSettings> NullIgnoringJsonSerializerSettings = new Lazy<JsonSerializerSettings>(() =>
        {
            var s = new JsonSerializerSettings();
            s.NullValueHandling = NullValueHandling.Ignore;
            return s;
        });
    }
}