using System;
using System.Collections.Generic;
using System.IO;

namespace nDocument.Code.Archive
{
    public interface IArchiveProvider
    {
        ArchiveFetchResult Fetch(string key);
        ArchiveMetadataFetchResult FetchMetadata(string key);
        Dictionary<string, ArchiveMetadataFetchResult> FetchMetadataBulk(ISet<string> keys);
        bool TryStore(byte[] fileBytes, string mimeType, string filename, out string key, out string errorMessage, ArchiveOptionalData optionalData = null);
        bool TryStore(Stream file, string mimeType, string filename, out string key, out string errorMessage, ArchiveOptionalData optionalData = null);
        bool Delete(string key);
    }

    public class ArchiveOptionalData
    {
        public string DelayedDocumentType { get; set; }
        public string DelayedDocumentTemplateArchiveKey { get; set; }
        /// <summary>
        /// Something that ties this to a purpose in the system.
        /// The intent is to allow figuring out if documents can be removed
        /// without looking up if anything points to it.
        /// Could for instance be CreditApplicationDocumentAttachment for all documents
        /// that have been attached to a credit application. In this case SourceId would be
        /// application nr.
        /// </summary>
        public string SourceType { get; set; }
        public string SourceId { get; set; }

        public static ArchiveOptionalData Parse(Func<string, string> getOptionalData)
        {
            return new ArchiveOptionalData
            {
                DelayedDocumentTemplateArchiveKey = getOptionalData("delayedDocumentTemplateArchiveKey"),
                DelayedDocumentType = getOptionalData("delayedDocumentType"),
                SourceType = getOptionalData("sourceType"),
                SourceId = getOptionalData("sourceId")
            };
        }

        public void SetOptionalData(Action<string, string> setOptionalData)
        {
            void Set(string name, string value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    setOptionalData(name, value);
                }
            }

            Set("delayedDocumentTemplateArchiveKey", DelayedDocumentTemplateArchiveKey);
            Set("delayedDocumentType", DelayedDocumentType);
            Set("sourceType", SourceType);
            Set("sourceId", SourceId);
        }
    }
}