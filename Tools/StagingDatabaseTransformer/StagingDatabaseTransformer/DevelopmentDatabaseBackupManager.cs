using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Dapper;

namespace StagingDatabaseTransformer
{
    public class DevelopmentDatabaseBackupManager
    {
        private readonly string masterDbConnectionString;

        public DevelopmentDatabaseBackupManager(string masterDbConnectionString)
        {
            this.masterDbConnectionString = masterDbConnectionString;
        }

        public void WithTemporaryDatabase(string backupFilePath, Action<SqlConnection, string> withTemporaryDatabaseConnectionAndName, string restoreToDataFilesPath, string debugTempNamePrefix = null)
        {
            var temporaryDatabaseName = "ntech-tempdb-" + (debugTempNamePrefix ?? "") + Guid.NewGuid().ToString();

            var temporaryDatabaseConnectionstring = new SqlConnectionStringBuilder(masterDbConnectionString);
            temporaryDatabaseConnectionstring.InitialCatalog = temporaryDatabaseName;

            RestoreDatabase(backupFilePath, temporaryDatabaseName, false, restoreToDataFilesPath);

            try
            {
                using (var conn = new SqlConnection(temporaryDatabaseConnectionstring.ToString()))
                {
                    conn.Open();
                    withTemporaryDatabaseConnectionAndName(conn, temporaryDatabaseName);
                }
            }
            finally
            {
                DropDatabase(temporaryDatabaseName);
            }
        }

        public void KillAllCurrentConnectionsToDatabase(string databaseName)
        {
            using (var masterDbConnection = new SqlConnection(masterDbConnectionString))
            {
                masterDbConnection.Open();
                KillAllCurrentConnectionsToDatabaseI(databaseName, masterDbConnection);
            }
        }

        private void KillAllCurrentConnectionsToDatabaseI(string databaseName, SqlConnection masterDbConnection)
        {
            if (ExistsDatabase(databaseName, masterDbConnection))
            {
                masterDbConnection.Execute($"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
                masterDbConnection.Execute($"ALTER DATABASE [{databaseName}] SET MULTI_USER");
            }
        }

        private bool ExistsDatabase(string databaseName, SqlConnection masterDbConnection)
        {
            return masterDbConnection.QueryFirst<int>($"select COUNT(*) from sys.databases where name = @name", new { name = databaseName }) > 0;
        }

        public void DropDatabase(string databaseName)
        {
            using (var masterDbConnection = new SqlConnection(masterDbConnectionString))
            {
                masterDbConnection.Open();
                DropDatabaseI(databaseName, masterDbConnection);
            }
        }

        private void DropDatabaseI(string databaseName, SqlConnection masterDbConnection)
        {
            if (!ExistsDatabase(databaseName, masterDbConnection))
                throw new Exception($"Database {databaseName} cannot be dropped since it does not exist");

            KillAllCurrentConnectionsToDatabaseI(databaseName, masterDbConnection);
            masterDbConnection.Execute($"drop database [{databaseName}]");
        }

        public void RestoreDatabase(string backupFilePath, string restoreToDatabaseName, bool allowOverwrite, string restoreToDataFilesPath)
        {
            using (var masterDbConnection = new SqlConnection(masterDbConnectionString))
            {
                masterDbConnection.Open();

                if (!File.Exists(backupFilePath))
                    throw new Exception($"Missing backup file: {backupFilePath}");

                var exists = ExistsDatabase(restoreToDatabaseName, masterDbConnection);
                if (!allowOverwrite && exists)
                    throw new Exception($"The database {restoreToDatabaseName} already exists and overwrite is not allowed");
                else if (exists)
                    KillAllCurrentConnectionsToDatabaseI(restoreToDatabaseName, masterDbConnection);

                var result = masterDbConnection.Query<BackupFileInfoItem>($"RESTORE FILELISTONLY FROM DISK='{backupFilePath}'").ToList();

                var declare = $"declare @name nvarchar(max) = '{restoreToDatabaseName}'";
                declare += $" declare @fileName nvarchar(max) = '{backupFilePath}'";
                var restore = "restore database @name from DISK = @filename WITH REPLACE ";
                foreach (var backupItem in result.Select((x, i) => new { Item = x, Index = i }))
                {
                    var extension = Path.GetExtension(backupItem.Item.PhysicalName);
                    var newDbSegmentPath = Path.Combine(restoreToDataFilesPath, restoreToDatabaseName + $"_{backupItem.Index}" + extension);
                    declare += $" declare @dbFileName{backupItem.Index} nvarchar(max) = '{newDbSegmentPath}'";
                    restore += $" , MOVE '{backupItem.Item.LogicalName}' TO @dbFileName{backupItem.Index}";
                }

                masterDbConnection.Execute(declare + " " + restore, commandTimeout: 60 * 30); //30 minutes
            }
        }

        public void BackupDatabase(string databaseName, string backupFilename, bool allowOverwrite)
        {
            if (File.Exists(backupFilename) && !allowOverwrite)
                throw new Exception($"The file '{backupFilename}' already exists and allow overwrite is not enabled.");
            if (!backupFilename.EndsWith(".bak"))
                throw new Exception("Backup file name must end with .bak"); //Just a guard to prevent mistakes like running everthing backwards and killing the data or logfiles

            using (var masterDbConnection = new SqlConnection(masterDbConnectionString))
            {
                masterDbConnection.Open();
                if (!ExistsDatabase(databaseName, masterDbConnection))
                    throw new Exception($"The database {databaseName} does not exist");

                masterDbConnection.Execute(
                    "BACKUP DATABASE @name TO DISK = @fileName WITH COPY_ONLY, COMPRESSION, INIT", param: new
                    {
                        fileName = backupFilename,
                        name = databaseName
                    }, commandTimeout: 60 * 5);
            }
        }

        public void CreateEmptyDatabase(DirectoryInfo dbFolder, string databaseName, bool isForTest, bool allowOverwrite)
        {
            using (var masterDbConnection = new SqlConnection(masterDbConnectionString))
            {
                masterDbConnection.Open();
                if (ExistsDatabase(databaseName, masterDbConnection))
                {
                    if (allowOverwrite)
                        DropDatabaseI(databaseName, masterDbConnection);
                    else
                        throw new Exception($"Database {databaseName} already exists and overwrite is not enabled");
                }

                CreateEmptyDatabase(masterDbConnection, dbFolder, databaseName, isForTest);
            }                    
        }

        private void CreateEmptyDatabase(SqlConnection masterDbConnection, DirectoryInfo dbFolder, string databaseName, bool isForTest)
        {
            var dataFileName = Path.Combine(dbFolder.FullName, $"{databaseName}.mdf");
            var logFileName = Path.Combine(dbFolder.FullName, $"{databaseName}.ldf");

            string ReplaceNames(string query) =>
                query.Replace("{{DATABASE_NAME}}", databaseName)
                     .Replace("{{DATA_FILENAME}}", dataFileName)
                     .Replace("{{LOG_FILENAME}}", logFileName);

            SqlTransaction tr = null;

            void Execute(string query)
            {
                try
                {
                    masterDbConnection.Execute(ReplaceNames(query), transaction: tr, commandTimeout: 60);
                }
                catch
                {
                    Console.WriteLine($"Exception caused by query: {ReplaceNames(query)}");
                    throw;
                }
            }

            void AlterSet(List<string> instructions)
            {
                foreach (var instruction in instructions) Execute("ALTER DATABASE [{{DATABASE_NAME}}] SET " + instruction);
            }

            //N'C:\Naktergal\Databases\{{DATABASE_NAME}}.mdf'
            Execute("USE MASTER");
            Execute(@"CREATE DATABASE [{{DATABASE_NAME}}] 
                ON PRIMARY (NAME = N'PrimaryRowData', FILENAME = N'{{DATA_FILENAME}}' , MAXSIZE = UNLIMITED, FILEGROWTH = 10%) 
                LOG ON (NAME = N'PrimaryLogData', FILENAME = N'{{LOG_FILENAME}}', MAXSIZE = 2048GB , FILEGROWTH = 10%)
                COLLATE Finnish_Swedish_CI_AS");

            AlterSet(new List<string> { @"SINGLE_USER" });

            //These we know matter
            AlterSet(new List<string>
                {
                    "COMPATIBILITY_LEVEL = 120",
                    "AUTO_SHRINK OFF",
                    "AUTO_UPDATE_STATISTICS ON",
                    "ALLOW_SNAPSHOT_ISOLATION ON",
                    "READ_COMMITTED_SNAPSHOT ON",
                    isForTest ? "RECOVERY SIMPLE" : "RECOVERY FULL"
                });

            //These are copied from what we know works but we dont know if they actually matter
            AlterSet(new List<string>
                {
                    "NUMERIC_ROUNDABORT OFF",
                    "QUOTED_IDENTIFIER OFF",
                    "CURSOR_CLOSE_ON_COMMIT OFF",
                    "CURSOR_DEFAULT GLOBAL",
                    "CONCAT_NULL_YIELDS_NULL OFF",
                    "ANSI_NULL_DEFAULT OFF",
                    "ANSI_NULLS OFF",
                    "ANSI_PADDING OFF",
                    "ANSI_WARNINGS OFF",
                    "ARITHABORT OFF",
                    "AUTO_CLOSE OFF",
                    "RECURSIVE_TRIGGERS OFF",
                    "DISABLE_BROKER",
                    "AUTO_UPDATE_STATISTICS_ASYNC OFF",
                    "DATE_CORRELATION_OPTIMIZATION OFF",
                    "TRUSTWORTHY OFF",
                    "PARAMETERIZATION SIMPLE",
                    "HONOR_BROKER_PRIORITY OFF",
                    "PAGE_VERIFY CHECKSUM",
                    "DB_CHAINING OFF",
                    "FILESTREAM( NON_TRANSACTED_ACCESS = OFF )",
                    "TARGET_RECOVERY_TIME = 0 SECONDS",
                    "DELAYED_DURABILITY = DISABLED",
                });
            

            AlterSet(new List<string> { @"MULTI_USER" });

         
        }

        private HashSet<string> SystemDatabaseNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "master", "model", "msdb", "tempdb" };
        public bool IsSystemDatabaseName(string dbName)
        {
            return SystemDatabaseNames.Contains(dbName.Replace("[", "").Replace("]", ""));
        }

        public List<string> GetAllNonSystemDatabaseNames()
        {
            using (var masterDbConnection = new SqlConnection(masterDbConnectionString))
            {
                masterDbConnection.Open();
                return masterDbConnection.Query<string>("select name from master.dbo.sysdatabases where name not in ('master','model','msdb','tempdb') order by name").ToList();
            }
        }

        private class BackupFileInfoItem
        {
            public string LogicalName { get; set; }
            public string PhysicalName { get; set; }
        }
    }
}
