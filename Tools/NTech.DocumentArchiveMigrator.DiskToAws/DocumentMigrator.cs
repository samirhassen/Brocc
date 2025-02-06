using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System.Diagnostics;
using System.Net;
using System.Xml.Linq;

namespace NTech.DocumentArchiveMigrator.DiskToAws
{
    public class DocumentMigrator : IDisposable
    {
        private readonly string archiveDirectory;
        private readonly string backupDirectory;
        private AmazonS3Client s3Client;
        private readonly string bucketName;
        private string bucketPathPrefix;
        public Stopwatch AwsTimer;

        public DocumentMigrator(string archiveDirectory, string backupDirectory, Func<string, string> getRequiredAppSetting)
        {
            this.archiveDirectory = archiveDirectory;
            this.backupDirectory = backupDirectory;

            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(getRequiredAppSetting("Region"))
            };

            var credentials = new Amazon.Runtime.BasicAWSCredentials(getRequiredAppSetting("AccessKey"), getRequiredAppSetting("SecretKey"));
            s3Client = new AmazonS3Client(credentials, config);
            bucketName = getRequiredAppSetting("Bucket");
            var bucketPathPrefixSetting = getRequiredAppSetting("BucketPathPrefix");
            bucketPathPrefix = bucketPathPrefixSetting == "/" ? "" : bucketPathPrefixSetting;
            AwsTimer = Stopwatch.StartNew();
            AwsTimer.Stop();
        }

        public async Task MigrateDocumentAsync(string metadataFileName)
        {            
            XDocument metadataFile = DiskArchiveProvider.LoadXDocument(metadataFileName);
            var key = metadataFile.Element("meta")?.Element("key")?.Value;
            if (key == null) 
                throw new Exception($"Missing key for: {metadataFileName}");

            var dataFileName = Path.Combine(archiveDirectory, key);
            if(!File.Exists(dataFileName))
                throw new Exception($"Missing data file for: {metadataFileName}");

            var metaData = DiskArchiveProvider.ArchiveMetadataFetchResult.CreateFromXml(metadataFile);

            AwsTimer.Start();
            using (var dataFileReadStream = File.OpenRead(dataFileName))
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = bucketPathPrefix + key,
                    ContentType = metaData.ContentType,
                    InputStream = dataFileReadStream
                };
                var awsMetadata = new MetadataHelper(putRequest.Metadata);
                awsMetadata.SetFilename(metaData.FileName);                

                putRequest.Metadata.Add("ntechfilename", Uri.EscapeDataString(metaData.FileName));
                metaData.OptionalData?.SetOptionalData((x, y) => awsMetadata.SetValue(x, y));
                var response = await s3Client.PutObjectAsync(putRequest);
                if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                    throw new Exception($"Failed to upload: {metadataFileName}");
            }
            AwsTimer.Stop();

            MoveFileToDirectory(metadataFileName, backupDirectory);
            MoveFileToDirectory(dataFileName, backupDirectory);
        }

        private void MoveFileToDirectory(string fullFileName, string targetDirectory)
        {
            File.Move(fullFileName, Path.Combine(targetDirectory, Path.GetFileName(fullFileName)));
        }

        public void Dispose()
        {
            if (s3Client != null)
                s3Client.Dispose();
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
}
