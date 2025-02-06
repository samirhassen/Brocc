using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace nDocument.Code.Archive
{
    public class AwsArchiveProvider : IArchiveProvider
    {
        private readonly AwsBucketCredentials awsCredentials;
        private readonly string bucketPathPrefix;
        private readonly RegionEndpoint regionEndpoint;

        public AwsArchiveProvider()
        {
            awsCredentials = AwsBucketCredentials.ParseFromEnvironment(NTechEnvironmentLegacy.SharedInstance);
            regionEndpoint = RegionEndpoint.GetBySystemName(awsCredentials.RegionName ?? "eu-north-1");
            bucketPathPrefix = awsCredentials.BucketPathPrefix ?? "archive1/";
            if (!bucketPathPrefix.EndsWith("/"))
                bucketPathPrefix += "/";
        }

        private string GetAwsFileName(string key)
        {
            return bucketPathPrefix + key;
        }

        private T WithS3Client<T>(Func<AmazonS3Client, T> f)
        {
            var config = new AmazonS3Config
            {
                RegionEndpoint = regionEndpoint
            };

            var credentials = new Amazon.Runtime.BasicAWSCredentials(awsCredentials.AccessKey, awsCredentials.SecretKey);
            using (var s3Client = new AmazonS3Client(credentials, config))
            {
                return f(s3Client);
            }
        }

        public ArchiveFetchResult Fetch(string key)
        {
            return WithS3Client(s3Client =>
            {
                var getObjectRequest = new GetObjectRequest
                {
                    BucketName = awsCredentials.BucketName,
                    Key = GetAwsFileName(key)
                };

                try
                {
                    using (var getObjectResponse = s3Client.GetObject(getObjectRequest))
                    {
                        var ms = new MemoryStream();
                        getObjectResponse.ResponseStream.CopyTo(ms);

                        ms.Position = 0;

                        var metadata = new MetadataHelper(getObjectResponse.Metadata);

                        return new ArchiveFetchResult
                        {
                            Content = ms,
                            ContentType = getObjectResponse.Headers.ContentType,
                            FileName = metadata.GetFilename(),
                            OptionalData = ParseOptionalData(metadata)
                        };
                    }
                }
                catch (AmazonS3Exception ex)
                {
                    if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                        return null; // Object doesn't exist. We don't throw error, as it may exist in backup provider.

                    NLog.Error(ex, $"Failed to fetch document with key '{key}'");
                    throw new Exception("Failed to fetch document.");
                }
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

        private ArchiveOptionalData ParseOptionalData(MetadataHelper metadata) =>
            ArchiveOptionalData.Parse(name => metadata.GetValue(name));

        public ArchiveMetadataFetchResult FetchMetadata(string key)
        {
            return WithS3Client(client =>
            {
                var request = new GetObjectMetadataRequest
                {
                    BucketName = awsCredentials.BucketName,
                    Key = GetAwsFileName(key)
                };

                try
                {
                    GetObjectMetadataResponse response = client.GetObjectMetadata(request);

                    var metadata = new MetadataHelper(response.Metadata);

                    if (response != null)
                    {
                        return new ArchiveMetadataFetchResult
                        {
                            ContentType = response.Headers.ContentType,
                            FileName = metadata.GetFilename(),
                            OptionalData = ParseOptionalData(metadata)
                        };
                    }
                }
                catch (AmazonS3Exception ex)
                {
                    NLog.Error(ex, $"Failed to fetch document metadata with key '{key}'");
                    throw new Exception("Failed to fetch document metadata.");
                }

                return null;
            });
        }

        public bool Delete(string key)
        {
            return WithS3Client(s3Client =>
            {
                var deleteObjectRequest = new DeleteObjectRequest
                {
                    BucketName = awsCredentials.BucketName,
                    Key = GetAwsFileName(key)
                };

                try
                {
                    s3Client.DeleteObject(deleteObjectRequest);
                    return true;
                }
                catch (AmazonS3Exception ex)
                {
                    if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        NLog.Warning(ex, "Failed to delete a document that does not exist.");
                        return false;
                    }
                    else
                    {
                        NLog.Error(ex, "Failed to delete document.");
                        throw new Exception("Failed to delete document.");
                    }
                }
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
                catch (Exception ex)
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

            var (Success, Key, ErrorMessage) = DoTry(1);
            key = Key;
            errorMessage = ErrorMessage;
            return Success;
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

            try
            {
                return WithS3Client<(bool, string, string)>(client =>
                {
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = awsCredentials.BucketName,
                        Key = GetAwsFileName(innerKey),
                        ContentType = mimeType,
                        InputStream = file
                    };

                    var metadata = new MetadataHelper(putRequest.Metadata);

                    metadata.SetFilename(filename);
                    optionalData?.SetOptionalData((x, y) => metadata.SetValue(x, y));

                    var response = client.PutObject(putRequest);

                    if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return (true, innerKey, null);
                    }
                    else
                    {
                        return (false, null, "Failed to upload file to AWS");
                    }
                });
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        class MetadataHelper
        {
            public MetadataHelper(MetadataCollection metadata)
            {
                Metaddata = metadata;
            }

            public MetadataCollection Metaddata { get; }

            public void SetFilename(string filename) =>
                SetValue("ntechfilename", filename);

            public string GetFilename() =>
                GetValue("ntechfilename");

            public void SetValue(string name, string value) =>
                Metaddata[name] = WebUtility.UrlEncode(value);            

            public string GetValue(string name)
            {
                //Aws ignores the case provided for metadata properties and just converts everything silently to lowercase
                var v = Metaddata[name.ToLowerInvariant()];

                if (v == null)
                    return null;

                v = WebUtility.UrlDecode(v);

                return v;
            }
        }
    }

    public class AwsBucketCredentials
    {
        public string BucketName { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string RegionName { get; set; }
        public string BucketPathPrefix { get; set; }

        public static AwsBucketCredentials ParseFromEnvironment(INTechEnvironment environment)
        {
            return new AwsBucketCredentials
            {
                BucketName = environment.RequiredSetting("ntech.archive.storageprovider.aws.bucketname"),
                AccessKey = environment.RequiredSetting("ntech.archive.storageprovider.aws.accesskey"),
                SecretKey = environment.RequiredSetting("ntech.archive.storageprovider.aws.secretkey"),
                //For example eu-north-1
                RegionName = environment.RequiredSetting("ntech.archive.storageprovider.aws.region"),
                //For example test/ or test which will cause the files to be stored in the subfolder test instead of the default subfolder archive1
                BucketPathPrefix = environment.OptionalSetting("ntech.archive.storageprovider.aws.bucketpathprefix")
            };
        }
    }
}