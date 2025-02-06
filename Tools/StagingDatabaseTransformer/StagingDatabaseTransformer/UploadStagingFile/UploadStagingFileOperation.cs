using Microsoft.WindowsAzure.Storage.Blob;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StagingDatabaseTransformer.UploadStagingFile
{
    public class UploadStagingFileOperation
    {
        public static void Run(IDictionary<string, string> parameters)
        {
            var localFilePath = parameters["localFilePath"];
            var remoteFilePath = parameters["remoteFilePath"];
            var azureSasUrl = new Uri(parameters["azureSasUrl"]);
            //TODO: Rename the last one if it exists so we always have the current and the one before but no more

            var c = new CloudBlobContainer(azureSasUrl);
            var blockBlob = c.GetBlockBlobReference(remoteFilePath);
            blockBlob.Properties.ContentType = "application/zip";
            blockBlob.Metadata["ntechdate"] = DateTimeOffset.Now.ToString("o");
            blockBlob.Metadata["ntechtype"] = "stagingdata";
            using (var fs = File.OpenRead(localFilePath))
            {
                blockBlob.UploadFromStream(fs);
            }
        } 
    }
}
