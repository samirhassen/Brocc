using System;
using System.IO;

namespace nCustomer.Code
{
    public class DocumentClient : AbstractServiceClient
    {
        protected override string ServiceName => "nDocument";

        public string ArchiveStore(byte[] fileData, string mimeType, string filename)
        {
            return Begin()
                .PostJson("Archive/Store", new
                {
                    MimeType = mimeType,
                    FileName = filename,
                    Base64EncodedFileData = Convert.ToBase64String(fileData)
                })
                .ParseJsonAs<ArchiveStoreResult>()
                .Key;
        }

        public string ArchiveStoreFile(FileInfo file, string mimeType, string fileName)
        {
            using (var fs = file.OpenRead())
            {
                return Begin().UploadFile("Archive/StoreFile", fs, fileName, mimeType).ParseJsonAs<ArchiveStoreResult>().Key;
            }
        }

        private class ArchiveStoreResult
        {
            public string Key { get; set; }
        }

        public byte[] FetchRawWithFilename(string key, out string contentType, out string filename)
        {
            using (var ms = new MemoryStream())
            {
                Begin()
                    .Get("Archive/Fetch?key=" + key)
                    .DownloadFile(ms, out contentType, out filename);
                return ms.ToArray();
            }
        }

        public DocumentMetadata FetchMetadata(string key) =>
            Begin()
            .PostJson("Archive/FetchMetadata", new { key })
            .ParseJsonAs<DocumentMetadata>();

        public class DocumentMetadata
        {
            public string ContentType { get; set; }
            public string FileName { get; set; }
        }
    }
}