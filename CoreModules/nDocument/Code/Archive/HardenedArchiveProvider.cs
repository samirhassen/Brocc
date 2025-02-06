using System.Collections.Generic;
using System.IO;

namespace nDocument.Code.Archive
{
    public class HardenedArchiveProvider : IArchiveProvider
    {
        private readonly IArchiveProvider realProvider;

        public HardenedArchiveProvider(IArchiveProvider realProvider)
        {
            this.realProvider = realProvider;
        }

        public ArchiveFetchResult Fetch(string key)
        {
            return realProvider.Fetch(key);
        }

        public ArchiveMetadataFetchResult FetchMetadata(string key)
        {
            return realProvider.FetchMetadata(key);
        }

        public Dictionary<string, ArchiveMetadataFetchResult> FetchMetadataBulk(ISet<string> keys)
        {
            return realProvider.FetchMetadataBulk(keys);
        }

        public static bool IsFileTypeAllowed(string mimeType, string filename)
        {
            try
            {
                var ext = System.IO.Path.GetExtension(filename);
                return ext.IsOneOfIgnoreCase(
                    //Text
                    ".txt", ".csv", ".xml", ".json",
                    //Microsoft
                    ".xls", ".xlsx", ".doc", ".docx", ".msg",
                    //Images
                    ".bmp", ".gif", ".jpg", ".jpeg", ".png", ".tif",
                    //Other
                    ".pdf", ".rtf",
                    //Bookkeeping
                    ".si", ".sie",
                    //Archives (note: this would be better to keep as an exception per use so move to that when possible)
                    ".zip"
                    );
            }
            catch (System.ArgumentException)
            {
                //Happens when the filename has invalid characters
                return false;
            }
        }

        public const string FileTypeNotAllowedCode = "fileTypeNotAllowed";

        public bool TryStore(byte[] fileBytes, string mimeType, string filename, out string key, out string errorMessage, ArchiveOptionalData optionalData = null)
        {
            if (!IsFileTypeAllowed(mimeType, filename))
            {
                key = null;
                errorMessage = FileTypeNotAllowedCode;
                return false;
            }
            return realProvider.TryStore(fileBytes, mimeType, filename, out key, out errorMessage, optionalData: optionalData);
        }

        public bool TryStore(Stream file, string mimeType, string filename, out string key, out string errorMessage, ArchiveOptionalData optionalData = null)
        {
            if (!IsFileTypeAllowed(mimeType, filename))
            {
                key = null;
                errorMessage = FileTypeNotAllowedCode;
                return false;
            }
            return realProvider.TryStore(file, mimeType, filename, out key, out errorMessage, optionalData: optionalData);
        }

        public bool Delete(string key)
        {
            return realProvider.Delete(key);
        }
    }
}