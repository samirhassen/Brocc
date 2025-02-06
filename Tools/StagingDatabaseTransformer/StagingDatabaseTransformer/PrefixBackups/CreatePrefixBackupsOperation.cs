using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.IO;
using nTest.RandomDataSource;

namespace StagingDatabaseTransformer
{
    public static class CreatePrefixBackupsOperation
    {
        /// <summary>
        /// Create a backup of all databases starting with a prefix and also of the nTest database
        /// The nTest backup is also set back to initial and a time snapshot is saved
        /// </summary>
        /// <param name="parameters"></param>
        public static void Run(IDictionary<string, string> parameters)
        {
            var prefix = parameters["prefix"];
            var backupFolder = parameters["backupFolder"];
            var masterDbConnectionString = parameters["masterDbConnectionString"];

            List<string> dbNames;
            using (var conn = new SqlConnection(masterDbConnectionString))
            {
                conn.Open();
                dbNames = conn.Query<string>("select distinct name from sys.databases where name like @prefix", new { prefix = $"{prefix}%" }).ToList();
            }

            var mgr = new DevelopmentDatabaseBackupManager(masterDbConnectionString);
            foreach (var dbName in dbNames)
            {
                var bakFile = Path.Combine(backupFolder, $"{dbName}.bak");
                Console.WriteLine($"Backing up: {dbName} to {bakFile}");
                mgr.BackupDatabase(dbName, bakFile, true);
            }

            if (parameters.ContainsKey("nTestDbFile"))
            {
                var systemUserName = parameters["systemUserName"];
                var systemUserPassword = parameters["systemUserPassword"];
                var serviceRegistry = ParameterFile.PaseServiceRegistry(parameters["serviceRegistryFilePath"]);
                var nTestDbFile = parameters["nTestDbFile"];

                Func<string, Uri> baseUrl = n => new Uri(serviceRegistry[n]);

                var systemUserAccessToken = NHttp.AquireSystemUserAccessTokenWithUsernamePassword(systemUserName, systemUserPassword, baseUrl("nUser"));

                var currentTime = NHttp
                    .Begin(baseUrl("nTest"), systemUserAccessToken)
                    .PostJson("Api/TimeMachine/GetCurrentTime", new { })
                    .ParseJsonAsAnonymousType(new { currentTime = (DateTimeOffset?)null }).currentTime.Value;

                var tmp = Path.GetTempFileName();

                Console.WriteLine("Backing up nTest database");
                File.Delete(tmp);
                File.Copy(nTestDbFile, tmp);

                using (var db = SqliteDocumentDatabase.FromFile(new FileInfo(tmp)))
                {
                    using (var dbTr = db.BeginTransaction())
                    {
                        dbTr.AddOrUpdate("transformStartDate", "stagingParams", currentTime.ToString("o"));
                        dbTr.AddOrUpdate("resetTimeMachineToDate", "nTestStartupInstruction", currentTime.ToString("o"));
                        dbTr.AddOrUpdate("setupState", "nTestStartupInstruction", "initial"); //Used by the test module to know if it should run the one time setup after a restore
                        dbTr.Commit();
                    }
                }

                File.Copy(tmp, Path.Combine(backupFolder, Path.GetFileName(nTestDbFile)), true);
                try
                {
                    File.Delete(tmp);
                }
                catch { /* Ignored */ }
            }
        }
    }
}