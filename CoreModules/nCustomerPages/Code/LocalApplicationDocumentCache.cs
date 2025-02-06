using System.IO;
using System.Linq;

namespace nCustomerPages.Code
{
    public class LocalApplicationDocumentCache
    {
        private readonly string documentCacheFolder;

        public LocalApplicationDocumentCache()
        {
            this.documentCacheFolder = Path.Combine(Path.GetTempPath(), "nCustomerPages-DocumentCache");
        }

        public void AddApplicationDocument(string documentType, string applicationNr, string key, byte[] data, string filename)
        {
            Directory.CreateDirectory(this.documentCacheFolder);
            var fn = $"{applicationNr}_{key}_{documentType}_{filename}";
            File.WriteAllBytes(Path.Combine(documentCacheFolder, fn), data);
        }

        public bool TryGetApplicationDocument(string documentType, string applicationNr, string key, out byte[] data, out string filename)
        {
            data = null;
            filename = null;

            var d = new DirectoryInfo(documentCacheFolder);
            if (!d.Exists)
                return false;

            var filenamePrefix = $"{applicationNr}_{key}_{documentType}_";
            var file = new DirectoryInfo(documentCacheFolder).GetFiles(filenamePrefix + "*").OrderByDescending(x => x.CreationTimeUtc).FirstOrDefault();
            if (file == null)
                return false;

            data = File.ReadAllBytes(file.FullName);
            filename = file.Name.Substring(filenamePrefix.Length);

            return true;
        }

        public string InferMimeTypeFromFilename(string filename)
        {
            if (filename.EndsWith(".txt"))
                return "text/plain";
            else if (filename.EndsWith(".pdf"))
                return "application/pdf";
            else
                return "application/octet-stream";
        }
    }
}