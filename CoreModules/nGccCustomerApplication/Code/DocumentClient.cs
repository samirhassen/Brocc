using System;
using System.IO;
using NTech.Services.Infrastructure;

namespace nGccCustomerApplication.Code
{
    public class DocumentClient
    {
        private class ArchiveStoreResult
        {
            public string Key { get; set; }
        }

        private NHttp.NHttpCall Begin(TimeSpan? timeout = null)
        {
            return NHttp.Begin(
                NEnv.ServiceRegistry.Internal.ServiceRootUri("nDocument"),
                NEnv.GetSelfCachingSystemUserBearerToken(),
                timeout: (timeout ?? TimeSpan.FromMinutes(1)));
        }

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

        public class ArchiveMetadataFetchResult
        {
            public string ContentType { get; set; }
            public string FileName { get; set; }
        }
    }
}