using Ionic.Zip;
using Ionic.Zlib;
using NTech.Services.Infrastructure;
using nTest.RandomDataSource;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Dapper;

namespace StagingDatabaseTransformer.CreateStagingFileFromProductionBackups
{
    public static class CreateStagingFileFromProductionBackupsOperation
    {
        public static void Run(ParameterFile p)
        {
            var parameters = p.Parameters;

            var randomSeed = int.Parse(parameters["randomSeed"]);
            var stagingFolder = parameters["stagingRootPath"];
            var masterStagingDbConnectionString = parameters["masterStagingDbConnectionString"];
            var nCustomerBackupFilePath = parameters["nCustomerBackupFilePath"];
            var nPreCreditBackupFilePath = parameters["nPreCreditBackupFilePath"];
            var nCreditBackupFilePath = parameters["nCreditBackupFilePath"];
            var nDataWarehouseBackupFilePath = parameters["nDataWarehouseBackupFilePath"];
            int? replacementUserId= null;
            if (parameters.ContainsKey("replacementUserId"))
                replacementUserId = int.Parse(parameters["replacementUserId"]);
            var disableMigrationHistoryCheck = parameters.ContainsKey("disableMigrationHistoryCheck")
                ? ((parameters["disableMigrationHistoryCheck"] ?? "false").Trim().ToLowerInvariant() == "true")
                : false;

            var clientConfigurationFilePath = parameters["clientConfigurationFilePath"];

            var clientCfg = ClientConfiguration.CreateUsingXDocument(XDocument.Load(clientConfigurationFilePath));
            var random = new RandomnessSource(randomSeed);

            var tempFolder = Path.Combine(stagingFolder, $"ntech-staging-output-{Guid.NewGuid().ToString()}");
            Directory.CreateDirectory(tempFolder);

            var tempDbManager = new DevelopmentDatabaseBackupManager(masterStagingDbConnectionString);
                        
            try
            {
                var testDbFile = Path.Combine(tempFolder, $"testdata.db");
                using (var db = SqliteDocumentDatabase.FromFile(new FileInfo(testDbFile)))
                {
                    var databaseTempFolder = Path.Combine(stagingFolder, "tempDatabases");
                    Directory.CreateDirectory(databaseTempFolder);
                    var settings = new SharedTransformSettings(clientCfg, random, db, new DirectoryInfo(tempFolder), tempDbManager, replacementUserId, disableMigrationHistoryCheck, databaseTempFolder);

                    using (var dbTr = db.BeginTransaction())
                    {
                        dbTr.AddOrUpdate("initialSeed", "stagingParams", randomSeed.ToString());
                        dbTr.AddOrUpdate("transformStartDate", "stagingParams", DateTimeOffset.Now.ToString("o"));
                        dbTr.AddOrUpdate("setupState", "nTestStartupInstruction", "initial"); //Used by the test module to know if it should run the one time setup after a restore
                        dbTr.Commit();
                    }

                    Console.WriteLine("Restoring nCustomer backup");
                    using (var customer = new CustomerStagingDatabaseTransform(settings, nCustomerBackupFilePath))
                    {
                        customer.Run();
                    }

                    Console.WriteLine("Restoring nPreCredit backup");
                    using (var precredit = new PreCreditStagingDatabaseTransform(settings, nPreCreditBackupFilePath))
                    {
                        precredit.Run();
                    }

                    Console.WriteLine("Restoring nCredit backup");
                    using (var credit = new CreditStagingDatabaseTransform(settings, nCreditBackupFilePath))
                    {
                        credit.Run();

                        if(credit.LatestTransactionDate.HasValue)
                        {
                            using (var dbTr = db.BeginTransaction())
                            {
                                dbTr.AddOrUpdate("resetTimeMachineToDate", "nTestStartupInstruction", credit.LatestTransactionDate);
                                dbTr.Commit();
                            }
                        }
                    }

                    Console.WriteLine("Restoring nDataWarehouse backup");
                    using (var dw = new DataWarehouseDatabaseTransform(settings, nDataWarehouseBackupFilePath))
                    {
                        dw.Run();
                    }

                    if(p.StaticIncludeFilesAndNames != null && p.StaticIncludeFilesAndNames.Count > 0)
                    {
                        Console.WriteLine("Adding static includes");
                        foreach(var f in p.StaticIncludeFilesAndNames)
                        {
                            f.Item1.CopyTo(Path.Combine(tempFolder, f.Item2));
                        }
                    }

                    Console.WriteLine($"***** Running stage Compression *****");
                    using (ZipFile zip = new ZipFile())
                    {
                        zip.CompressionLevel = CompressionLevel.BestSpeed;
                        zip.UseZip64WhenSaving = Zip64Option.Always;
                        zip.BufferSize = 65536 * 8; // Set the buffersize to 512k for better efficiency with large files
                        zip.AddDirectory(tempFolder);
                        zip.Save(Path.Combine(stagingFolder, "ntech-staging-data.zip"));
                    }
                }
            }
            finally
            {
                Directory.Delete(tempFolder, true);
            }
        }
    }
}
