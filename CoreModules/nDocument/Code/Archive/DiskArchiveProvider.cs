using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace nDocument.Code.Archive
{
    public class DiskArchiveProvider : IArchiveProvider
    {
        public ArchiveFetchResult Fetch(string key)
        {
            var r = GetFileNames(key);
            var dataFilename = r.Item1;
            var metaDataFilename = r.Item2;

            if (!File.Exists(dataFilename) || !File.Exists(metaDataFilename))
                return null;

            var meta = XDocuments.Load(metaDataFilename);

            var md = ArchiveMetadataFetchResult.CreateFromXml(meta);

            var contentStream = new MemoryStream(File.ReadAllBytes(dataFilename));
            contentStream.Position = 0;

            return new ArchiveFetchResult
            {
                FileName = md.FileName,
                ContentType = md.ContentType,
                Content = contentStream,
                OptionalData = md.OptionalData
            };
        }

        public ArchiveMetadataFetchResult FetchMetadata(string key)
        {
            var r = GetFileNames(key);
            var dataFilename = r.Item1;
            var metaDataFilename = r.Item2;

            if (!File.Exists(metaDataFilename))
                return null;
            var meta = XDocuments.Load(metaDataFilename);

            return ArchiveMetadataFetchResult.CreateFromXml(meta);
        }

        public Dictionary<string, ArchiveMetadataFetchResult> FetchMetadataBulk(ISet<string> keys)
        {
            return keys.ToDictionary(x => x, FetchMetadata);
        }

        private Tuple<string, string> GetFileNames(string key)
        {
            var rootFolder = NEnv.DiskStorageProviderRootFolder.FullName;
            return Tuple.Create(
                Path.Combine(rootFolder, $"{key}"),
                Path.Combine(rootFolder, $"{key}.metadata.xml"));
        }

        public bool TryStore(byte[] fileBytes, string mimeType, string filename, out string key, out string errorMessage, ArchiveOptionalData optionalData = null)
        {
            if (fileBytes == null)
            {
                key = null;
                errorMessage = "Missing filesBytes";
                return false;
            }
            return TryStoreI(n => File.WriteAllBytes(n, fileBytes), mimeType, filename, out key, out errorMessage, optionalData);
        }

        public bool TryStore(Stream stream, string mimeType, string filename, out string key, out string errorMessage, ArchiveOptionalData optionalData = null)
        {
            if (stream == null)
            {
                key = null;
                errorMessage = "Missing stream";
                return false;
            }
            return TryStoreI(n =>
                {
                    using (var fs = File.OpenWrite(n))
                    {
                        stream.CopyTo(fs);
                        fs.Flush();
                    }
                }, mimeType, filename, out key, out errorMessage, optionalData);
        }

        private bool TryStoreI(Action<string> saveBytes, string mimeType, string filename, out string key, out string errorMessage, ArchiveOptionalData optionalData)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                key = null;
                errorMessage = "Missing filename";
                return false;
            }
            if (string.IsNullOrWhiteSpace(mimeType))
            {
                key = null;
                errorMessage = "Missing mimeType";
                return false;
            }
            var innerKey = Guid.NewGuid().ToString() + Path.GetExtension(filename);

            key = innerKey;
            errorMessage = null;

            var r = GetFileNames(innerKey);
            var dataFilename = r.Item1;
            var metaDataFilename = r.Item2;
            var meta = ArchiveMetadataFetchResult.CreateMetadataXml(innerKey, mimeType, filename, optionalData);

            NEnv.DiskStorageProviderRootFolder.Create();

            meta.Save(metaDataFilename);
            File.SetAttributes(metaDataFilename, FileAttributes.NotContentIndexed);

            saveBytes(dataFilename);
            File.SetAttributes(dataFilename, FileAttributes.NotContentIndexed);

            return true;
        }

        public bool Delete(string key)
        {
            var r = GetFileNames(key);
            if (!File.Exists(r.Item1))
            {
                return false;
            }

            File.Delete(r.Item1);
            File.Delete(r.Item2);

            return true;
        }
    }
}