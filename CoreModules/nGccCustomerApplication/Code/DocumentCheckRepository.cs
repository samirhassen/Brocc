using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using nGccCustomerApplication.Code;
using Serilog;
using IdentityModel.Client;
using System.Collections.Generic;
using System.Linq;

namespace nGccCustomerApplication
{
    
    public class DocumentCheckRepository
    {
        private DocumentCheckRepository()
        {

        }

        private static Lazy<DocumentCheckRepository> sharedInstance = new Lazy<DocumentCheckRepository>(() => new DocumentCheckRepository());
        public static DocumentCheckRepository SharedInstance
        {
            get
            {
                return sharedInstance.Value;
            }
        }

        public bool TryParseDataUrl(string dataUrl, out string mimeType, out byte[] binaryData)
        {
            var result = System.Text.RegularExpressions.Regex.Match(dataUrl, @"data:(?<type>.+?);base64,(?<data>.+)");
            if (!result.Success)
            {
                mimeType = null;
                binaryData = null;
                return false;
            }
            else
            {
                mimeType = result.Groups["type"].Value.Trim();
                binaryData = Convert.FromBase64String(result.Groups["data"].Value.Trim());
                return true;
            }
        }

        private class DocumentSession
        {
            /// <summary>
            /// Used to prevent us from re-attaching the shared account data files if the user chooses to remove files
            /// </summary>
            public bool HasFileBeenManuallyRemoved { get; set; }
            public bool HasFileBeenManuallyAdded { get; set; }
            public List<FileModel> Files { get; set; }
        }

        public class FileModel
        {
            public string Id { get; set; }
            public int ApplicantNr { get; set; }
            public string FileName { get; set; }
            public string MimeType { get; set; }
            public string ArchiveKey { get; set; }
        }

        private PreCreditKeyValueClient preCreditKeyValueClient = new PreCreditKeyValueClient();

        private DocumentSession GetSessionRaw(string key)
        {
            var v = preCreditKeyValueClient.Get(key, PreCreditKeyValueClient.KeySpaceCode.CustomerApplicationDocumentCheckSession);
            if (v == null)
                return null;
            else
                return JsonConvert.DeserializeObject<DocumentSession>(v);
        }

        private void SetSessionRaw(string key, DocumentSession s)
        {
            preCreditKeyValueClient.Set(key, PreCreditKeyValueClient.KeySpaceCode.CustomerApplicationDocumentCheckSession, JsonConvert.SerializeObject(s));
        }

        private void RemoveSessionRaw(string key)
        {
            preCreditKeyValueClient.Remove(key, PreCreditKeyValueClient.KeySpaceCode.CustomerApplicationDocumentCheckSession);
        }

        private DocumentSession GetSession(string token, bool createIfNotExists)
        {
            var s = GetSessionRaw(token);
            if (s == null)
            {
                if (!createIfNotExists) return null;

                s = new DocumentSession
                {
                    Files = new List<FileModel>()
                };

                SetSessionRaw(token, s);
            }
            return s;
        }

        public class UserVisibleException : Exception
        {
            public UserVisibleException(string message) : base(message)
            {
            }
        }

        public string AddExistingFile(string token, int applicantNr, string archiveKey, bool isManual)
        {
            var dc = new DocumentClient();
            //TODO: Just get metadata 
            dc.FetchRawWithFilename(archiveKey, out var mimeType, out var filename);
            return AddExistingFileToSession(token, applicantNr, archiveKey, mimeType, filename, isManual);
        }

        public string AddFile(string token, int applicantNr, string filename, string dataUrl, bool isManual)
        {
            string mimeType;
            byte[] data;
            if (!TryParseDataUrl(dataUrl, out mimeType, out data))
                throw new UserVisibleException("Attached file is invalid");
            if (data.Length > 10000000)
                throw new UserVisibleException("Attached file is too big");

            var dc = new DocumentClient();
            var archiveKey = dc.ArchiveStore(data, mimeType, filename);

            return AddExistingFileToSession(token, applicantNr, archiveKey, mimeType, filename, isManual);
        }        

        private string AddExistingFileToSession(string token, int applicantNr, string archiveKey, string mimeType, string filename, bool isManual)
        {
            //TODO: Whitelist for mimetypes. Ideally also deep inspect the actual bytes to make sure the mime type isn't lying.                    
            var session = GetSession(token, true);
            var id = Guid.NewGuid().ToString();
            session.Files.Add(new FileModel
            {
                ApplicantNr = applicantNr,
                ArchiveKey = archiveKey,
                FileName = filename,
                Id = id,
                MimeType = mimeType
            });
            if(isManual)
            {
                session.HasFileBeenManuallyAdded = true;
            }
            SetSessionRaw(token, session);

            return id;
        }

        public void RemoveFile(string token, string id, bool isManual)
        {
            var session = GetSession(token, true);
            var f = session.Files.SingleOrDefault(x => x.Id == id);
            if (f == null)
                throw new UserVisibleException("No such file");
            session.Files.Remove(f);
            if(isManual)
            {
                session.HasFileBeenManuallyRemoved = true;
            }                
            SetSessionRaw(token, session);
        }
        
        public void RemoveSessionIfExists(string token)
        {
            RemoveSessionRaw(token);
        }

        public FileModel GetFile(string token, string id)
        {
            var session = GetSession(token, false);
            return session?.Files?.SingleOrDefault(x => x.Id == id);
        }

        public List<FileModel> GetFiles(string token, bool createSessionIfNotExists)
        {
            return GetFilesAndHasManualRemovals(token, createSessionIfNotExists).Item1;
        }

        public Tuple<List<FileModel>, bool> GetFilesAndHasManualRemovals(string token, bool createSessionIfNotExists)
        {
            var session = GetSession(token, createSessionIfNotExists);
            return Tuple.Create(session?.Files?.ToList(), session?.HasFileBeenManuallyRemoved ?? false);
        }
    }
}
