using NTech.Services.Infrastructure;
using System;
using System.IO;

namespace nTest.Code.Credit
{
    public class DocumentClient
    {
        private NHttp.NHttpCall Begin(TimeSpan? timeout = null)
        {
            return NHttp.Begin(NEnv.ServiceRegistry.Internal.ServiceRootUri("nDocument"), NEnv.AutomationBearerToken(), timeout: timeout);
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
                .ParseJsonAsAnonymousType(new { Key = "" })
                ?.Key;
        }

        public byte[] FetchRawWithFilename(string key, out string contentType, out string filename, bool allowHtml = false)
        {
            using (var ms = new MemoryStream())
            {
                Begin()
                    .Get("Archive/Fetch?key=" + key)
                    .DownloadFile(ms, out contentType, out filename, allowHtml: allowHtml);
                return ms.ToArray();
            }
        }

        public GetProfileResult GetExportProfile(string profileName)
        {
            return Begin().PostJson("FileExport/GetProfile", new { profileName }).ParseJsonAs<GetProfileResult>();
        }

        public class GetProfileResult
        {
            public string Name { get; set; }
            public string ProfileCode { get; set; }
            public string LocalFolderPath { get; set; }

            public bool IsLocalFolderProfile()
            {
                return ProfileCode == "LocalFolder";
            }
        }
    }
}