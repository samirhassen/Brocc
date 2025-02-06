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
    public static class RestorePrefixBackupsOperation
    {
        /// <summary>
        /// Create a backup of all databases starting with a prefix and also of the nTest database
        /// The nTest backup is also set back to initial and a time snapshot is saved
        /// </summary>
        /// <param name="parameters"></param>
        public static void Run(DictionaryIgnoreCaseWithKeyNameInErrorMessage parameters)
        {
            var prefix = parameters["prefix"];
            var backupFolder = parameters["backupFolder"];
            var masterDbConnectionString = parameters["masterDbConnectionString"];
            var databaseFilesFolderPath = parameters["databaseFilesFolderPath"];

            var mgr = new DevelopmentDatabaseBackupManager(masterDbConnectionString);       

            // Inputs. 
            var databaseNames = parameters.Opt("databaseNamesToRestore")?.Split(',')?.Select(x => x.Trim())?.ToList() ?? new List<string>();

            if (parameters.Opt("serviceNamesToSetup") != null)
                throw new Exception("Support for serviceNamesToSetup has been removed in favor of starting/stopping the app pools.");
            if (parameters.Opt("systemUserName") != null)
                throw new Exception("Support for systemUserName has been removed in favor of starting/stopping the app pools.");
            if (parameters.Opt("systemUserPassword") != null)
                throw new Exception("Support for systemUserPassword has been removed in favor of starting/stopping the app pools.");

            var isForTest = (parameters.Opt("isForTest")?.ToLowerInvariant() ?? "true") == "true";

            foreach (var databaseSuffix in databaseNames)
            {
                // Ex. "ReloanDev-" + "user"
                var databaseName = prefix + databaseSuffix;
                var backupFileName = Path.Combine(backupFolder, databaseName + ".bak");
                if(File.Exists(backupFileName))
                {
                    Console.WriteLine($" Restoring {backupFileName}");
                    mgr.RestoreDatabase(backupFileName, Path.GetFileNameWithoutExtension(backupFileName), true, databaseFilesFolderPath);
                }
                else
                {
                    Console.WriteLine($" Creating database {databaseName}");
                    mgr.CreateEmptyDatabase(new DirectoryInfo(databaseFilesFolderPath), databaseName, isForTest, true);
                }
            }     

            if (parameters.ContainsKey("nTestDbFile"))
            {
                var nTestDbFile = parameters["nTestDbFile"];

                Console.WriteLine("Restoring nTest database");

                File.Copy(Path.Combine(backupFolder, Path.GetFileName(nTestDbFile)), nTestDbFile, true);
            }
        }
    }
}