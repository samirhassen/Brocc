using Microsoft.WindowsAzure.Storage.Blob;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StagingDatabaseTransformer.DownloadStagingFile
{
    public class DownloadStagingFileOperation
    {        
        public static void Run(IDictionary<string, string> parameters)
        {
            var localFilePath = parameters["localFilePath"];
            var azureSasUrl = new Uri(parameters["azureSasUrl"]);
            var blob = new CloudBlockBlob(azureSasUrl);
            CopyToOld(localFilePath);
            blob.DownloadToFile(localFilePath, FileMode.Create);
        }

        private static void CopyToOld(string localFilePath)
        {
            if (File.Exists(localFilePath))
            {
                //Keep the one before around to enable going back if something strange happens
                var oldFile = Path.Combine(Path.GetDirectoryName(localFilePath), Path.GetFileName(localFilePath) + ".old");
                if (File.Exists(oldFile))
                {
                    try
                    {
                        File.Delete(oldFile);
                        File.Copy(localFilePath, oldFile);
                    }
                    catch
                    {
                        /* Ignored */
                    }
                }
                else
                {
                    File.Copy(localFilePath, oldFile);
                }                    
            }
        }
    }
}
