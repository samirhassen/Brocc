using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace StagingDatabaseTransformer.RestoreDevelopmentDatabasesFromStagingFile
{
    public static class RestoreDevelopmentDatabasesFromStagingFileOperation
    {
        public static void Run(IDictionary<string, string> parameters)
        {
            var masterDbConnectionString = parameters["masterDbConnectionString"];
            var stagingZipFilePath = parameters["stagingZipFilePath"];
            var customerDatabaseName = parameters["customerDatabaseName"];
            var precreditDatabaseName = parameters["precreditDatabaseName"];
            var creditDatabaseName = parameters["creditDatabaseName"];
            var dataWarehouseDatabaseName = parameters["dataWarehouseDatabaseName"];
            var auditDatabaseName = parameters["auditDatabaseName"];
            var creditReportDatabaseName = parameters["creditReportDatabaseName"];
            var databaseFilesFolderPath = parameters["databaseFilesFolderPath"];
            var tempFolder = parameters["tempFolder"];

            if (!File.Exists(stagingZipFilePath))
                throw new Exception($"Staging file does not exist: {stagingZipFilePath}");

            var tempStagingRoot = Path.Combine(tempFolder, $"staging-restore-{Guid.NewGuid().ToString()}");
            Directory.CreateDirectory(tempStagingRoot);
            try
            {
                using (var zip = new ZipFile(stagingZipFilePath))
                {
                    zip.ExtractAll(tempStagingRoot);
                }

                var restorer = new DevelopmentDatabaseBackupManager(masterDbConnectionString);

                Console.WriteLine("Restoring nCustomer");
                restorer.RestoreDatabase(
                    Path.Combine(tempStagingRoot, "nCustomer.bak"), 
                    customerDatabaseName,
                    true, databaseFilesFolderPath);

                Console.WriteLine("Restoring nPreCredit");
                restorer.RestoreDatabase(
                    Path.Combine(tempStagingRoot, "nPreCredit.bak"),
                    precreditDatabaseName,
                    true, databaseFilesFolderPath);

                Console.WriteLine("Restoring nCredit");
                restorer.RestoreDatabase(
                    Path.Combine(tempStagingRoot, "nCredit.bak"),
                    creditDatabaseName,
                    true, databaseFilesFolderPath);

                Console.WriteLine("Restoring nDataWarehouse");
                restorer.RestoreDatabase(
                    Path.Combine(tempStagingRoot, "nDataWarehouse.bak"),
                    dataWarehouseDatabaseName,
                    true, databaseFilesFolderPath);

                Console.WriteLine("Restoring nAudit");
                restorer.RestoreDatabase(
                    Path.Combine(tempStagingRoot, "nAudit.bak"),
                    auditDatabaseName,
                    true, databaseFilesFolderPath);

                Console.WriteLine("Restoring nCreditReport");
                restorer.RestoreDatabase(
                    Path.Combine(tempStagingRoot, "nCreditReport.bak"),
                    creditReportDatabaseName,
                    true, databaseFilesFolderPath);
            }
            finally
            {
                Directory.Delete(tempStagingRoot, true);
            }
        }
    }
}
