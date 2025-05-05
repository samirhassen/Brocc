
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace nDocument.Code.Archive
{
    public class AzureArchiveProvider : IArchiveProvider
    {
        private static T WithContainer<T>(Func<BlobContainerClient, T> a)
        {

           

            var settings = NEnv.AzureStorageContainernameAndConnectionstring;
            BlobContainerClient container = new BlobContainerClient(settings.Item2, settings.Item1);       
            return a(container);
        }

        private static string BlobNameFromKey(string key)
        {
            return "archive1/" + key;
        }

        public ArchiveFetchResult Fetch(string key)
        {
            return WithContainer(container =>
            {
               // var blockBlob = container.GetBlockBlobReference(BlobNameFromKey(key));
                BlobClient blockBlob = container.GetBlobClient(BlobNameFromKey(key));
                if (!blockBlob.Exists())
                    return null;

                
                var ms = new MemoryStream();
                blockBlob.DownloadTo(ms);

                ms.Position = 0;
                var properties =  blockBlob.GetProperties();
               

                return new ArchiveFetchResult
                {
                    Content = ms,
                    ContentType = properties.Value.ContentType,
                    FileName = Uri.UnescapeDataString(properties.Value.Metadata["ntechfilename"]),
                    OptionalData = ParseOptionalData(properties.Value.Metadata)
                };
            });
        }

        public bool TryStore(byte[] fileBytes, string mimeType, string filename, out string key, out string errorMessage, ArchiveOptionalData optionalData = null)
        {
            if (fileBytes == null)
            {
                key = null;
                errorMessage = "Missing filesBytes";
                return false;
            }
            return TryStore(new MemoryStream(fileBytes), mimeType, filename, out key, out errorMessage, optionalData: optionalData);
        }

        //private ArchiveOptionalData ParseOptionalData(BlobClient blockBlob)
        //{
        //    Func<IDictionary<string, string>, string, string> getOpt = (d, name) => d.ContainsKey(name) ? d[name] : null;

        //    return ArchiveOptionalData.Parse(name => getOpt(blockBlob.Metadata, name));
        //}

        private ArchiveOptionalData ParseOptionalData(IDictionary<string, string> metadata)
        {
            return ArchiveOptionalData.Parse(name => metadata.TryGetValue(name, out var value) ? value : null);
        }

        public ArchiveMetadataFetchResult FetchMetadata(string key)
        {
            return WithContainer(container =>
            {
                BlobClient blockBlob = container.GetBlobClient(BlobNameFromKey(key));

                //var blockBlob = container.GetBlockBlobReference(BlobNameFromKey(key));
                if (!blockBlob.Exists())
                    return null;

                var properties = blockBlob.GetProperties();

                return new ArchiveMetadataFetchResult
                {
                    ContentType = properties.Value.ContentType,
                    FileName = Uri.UnescapeDataString(properties.Value.Metadata["ntechfilename"]),
                    OptionalData = ParseOptionalData(properties.Value.Metadata)
                };
            });
        }

        public bool Delete(string key)
        {
            return WithContainer(container =>
            {
                BlobClient blockBlob = container.GetBlobClient(BlobNameFromKey(key));

                return blockBlob.DeleteIfExists();
            });
        }

        public Dictionary<string, ArchiveMetadataFetchResult> FetchMetadataBulk(ISet<string> keys)
        {
            return keys.ToDictionary(x => x, FetchMetadata);
        }

        public bool TryStore(Stream file, string mimeType, string filename, out string key, out string errorMessage, ArchiveOptionalData optionalData = null)
        {
            (bool Success, string Key, string ErrorMessage) DoTry(int tryNr)
            {
                try
                {
                    return TryStoreActual(file, mimeType, filename, optionalData);
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    if (tryNr == 1)
                    {
                        System.Threading.Thread.Sleep(100);
                        return DoTry(2);
                    }
                    else
                    {
                        string length = "unknown";
                        try
                        {
                            length = file.Length.ToString(); //If the stream is broken we dont want this error masking the actual error
                        }
                        catch {/* ignored */}
                        throw new Exception($"Failed to upload '{filename}' with type '{mimeType}' and length {length} bytes: {Environment.NewLine}{ex.ToString()}", ex);
                    }
                }
            }

            var result = DoTry(1);
            key = result.Key;
            errorMessage = result.ErrorMessage;
            return result.Success;
        }

        private (bool Success, string Key, string ErrorMessage) TryStoreActual(Stream file, string mimeType, string filename, ArchiveOptionalData optionalData)
        {
            if (file == null)
            {
                return (false, null, "Missing file");
            }
            if (string.IsNullOrWhiteSpace(filename))
            {
                return (false, null, "Missing filename");
            }
            if (string.IsNullOrWhiteSpace(mimeType))
            {
                return (false, null, "Missing mimeType");
            }
            var innerKey = Guid.NewGuid().ToString() + Path.GetExtension(filename).ToLowerInvariant();

            return WithContainer<(bool Success, string Key, string ErrorMessage)>(container =>
            {
                // var blockBlob = container.GetBlockBlobReference(BlobNameFromKey(innerKey));

                //blockBlob.Properties.ContentType = mimeType;
                // blockBlob.Metadata["ntechfilename"] = Uri.EscapeDataString(filename);
                // optionalData?.SetOptionalData((x, y) => blockBlob.Metadata[x] = y);
                // blockBlob.UploadFromStream(file);

             
                var blobClient = container.GetBlobClient(BlobNameFromKey(innerKey));

                // Build metadata dictionary
                var metadata = new Dictionary<string, string>
                {
                    ["ntechfilename"] = Uri.EscapeDataString(filename)
                };

                // Add optional metadata if provided
                optionalData?.SetOptionalData((key, value) => metadata[key] = value);

                // Upload with metadata and content type
                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = mimeType
                };

                 blobClient.Upload(file, new BlobUploadOptions
                {
                    HttpHeaders = blobHttpHeaders,
                    Metadata = metadata
                });

                return (true, innerKey, null);
            });
        }
    }
}