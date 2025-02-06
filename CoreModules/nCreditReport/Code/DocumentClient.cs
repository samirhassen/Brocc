using NTech.Services.Infrastructure;
using System;
using System.IO;
using System.Text;

namespace nCreditReport.Code
{
    public class DocumentClient : IDocumentClient
    {
        private class ArchiveStoreResult
        {
            public string Key { get; set; }
        }

        protected NHttp.NHttpCall Begin(TimeSpan? timeout = null)
        {
            return NHttp.Begin(new Uri(NEnv.ServiceRegistry.Internal["nDocument"]), NHttp.GetCurrentAccessToken(), timeout: timeout);
        }

        public string ArchiveStore(byte[] fileData, string mimeType, string filename)
        {
            return Begin(timeout: TimeSpan.FromSeconds(30)).PostJson("Archive/Store", new
            {
                MimeType = mimeType,
                FileName = filename,
                Base64EncodedFileData = Convert.ToBase64String(fileData)
            }).ParseJsonAs<ArchiveStoreResult>().Key;
        }

        public bool TryFetchRaw(string key, out string contentType, out string fileName, out byte[] fileBytes)
        {
            using (var ms = new MemoryStream())
            {
                var r = Begin().Get("Archive/Fetch?key=" + key);

                if (r.IsNotFoundStatusCode)
                {
                    fileName = null;
                    fileBytes = null;
                    contentType = null;
                    return false;
                }

                r.DownloadFile(ms, out contentType, out fileName);

                fileBytes = ms.ToArray();

                return true;
            }
        }

        public byte[] FetchRaw(string key, out string contentType, out string fileName)
        {
            byte[] b;
            if (TryFetchRaw(key, out contentType, out fileName, out b))
            {
                return b;
            }
            else
                throw new Exception($"Missing document {key} in the archive");
        }

        public string FetchRawString(string key, Encoding encoding)
        {
            var result = FetchRaw(key, out _, out _);
            return encoding.GetString(result, 0, result.Length);
        }

    }
}