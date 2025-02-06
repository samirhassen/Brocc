using Dapper;
using Microsoft.Data.SqlClient;

namespace NTechDbTool
{
    public class DevelopmentDatabaseBackupManager
    {
        private readonly string masterDbConnectionString;

        public DevelopmentDatabaseBackupManager(string masterDbConnectionString)
        {
            this.masterDbConnectionString = masterDbConnectionString;
        }

        public void WithTemporaryDatabase(string backupFilePath, Action<SqlConnection, string> withTemporaryDatabaseConnectionAndName, string restoreToDataFilesPath, string? debugTempNamePrefix = null)
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

        public void CreateEmptyDatabase(DirectoryInfo? dbFolder, string databaseName, bool isForTest, bool allowOverwrite, bool isManaged)
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

                CreateEmptyDatabase(masterDbConnection, dbFolder, databaseName, isForTest, isManaged);
            }
        }

        private void CreateEmptyDatabase(SqlConnection masterDbConnection, DirectoryInfo? dbFolder, string databaseName, bool isForTest, bool isManaged)
        {
            if (!isManaged && dbFolder == null)
            {
                throw new Exception("Missing dbFolder. Required except for managed databases.");
            }

            var dataFileName = dbFolder == null ? "" : Path.Combine(dbFolder.FullName, $"{databaseName}.mdf");
            var logFileName = dbFolder == null ? "" : Path.Combine(dbFolder.FullName, $"{databaseName}.ldf");

            Func<string, string> r = s => s
                .Replace("{{DATABASE_NAME}}", databaseName)
                .Replace("{{DATA_FILENAME}}", dataFileName)
                .Replace("{{LOG_FILENAME}}", logFileName);

            SqlTransaction? tr = null;

            Action<string> execute = q =>
            {
                try
                {
                    var query = r(q);
                    Console.WriteLine(query);
                    masterDbConnection.Execute(query, transaction: tr, commandTimeout: isManaged ? 180 : 60);
                }
                catch
                {
                    Console.WriteLine($"Exception caused by query: {r(q)}");
                    throw;
                }
            };

            Action<List<string>> alterSet = s => { foreach (var n in s.Where(x => !string.IsNullOrWhiteSpace(x))) execute("ALTER DATABASE [{{DATABASE_NAME}}] SET " + n); };

            execute("USE MASTER");

            execute("CREATE DATABASE [{{DATABASE_NAME}}]"
                + (isManaged
                ? " "
                : @"ON PRIMARY (NAME = N'PrimaryRowData', FILENAME = N'{{DATA_FILENAME}}' , MAXSIZE = UNLIMITED, FILEGROWTH = 10%) 
                    LOG ON (NAME = N'PrimaryLogData', FILENAME = N'{{LOG_FILENAME}}', MAXSIZE = 2048GB , FILEGROWTH = 10%)")
                + "COLLATE Finnish_Swedish_CI_AS");

            if (!isManaged)
            {
                alterSet(new List<string> { @"SINGLE_USER" });
            }

            //These we know matter
            alterSet(new List<string>
                {
                    "COMPATIBILITY_LEVEL = 120",
                    isManaged ? "" : "AUTO_SHRINK OFF",
                    isManaged ? "" : "AUTO_UPDATE_STATISTICS ON",
                    "ALLOW_SNAPSHOT_ISOLATION ON",
                    "READ_COMMITTED_SNAPSHOT ON",
                    isManaged ? "" :( isForTest ? "RECOVERY SIMPLE" : "RECOVERY FULL")
                });

            //These are copied from what we know works but we dont know if they actually matter
            alterSet(new List<string>
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
                    isManaged ? "" : "AUTO_CLOSE OFF",
                    isManaged ? "" : "RECURSIVE_TRIGGERS OFF",
                    isManaged ? "" : "DISABLE_BROKER",
                    isManaged ? "" : "AUTO_UPDATE_STATISTICS_ASYNC OFF",
                    isManaged ? "" : "DATE_CORRELATION_OPTIMIZATION OFF",
                    isManaged ? "" : "TRUSTWORTHY OFF",
                    isManaged ? "" : "PARAMETERIZATION SIMPLE",
                    isManaged ? "" : "HONOR_BROKER_PRIORITY OFF",
                    isManaged ? "" : "PAGE_VERIFY CHECKSUM",
                    isManaged ? "" : "DB_CHAINING OFF",
                    isManaged ? "" : "FILESTREAM( NON_TRANSACTED_ACCESS = OFF )",
                    isManaged ? "" : "TARGET_RECOVERY_TIME = 0 SECONDS",
                    isManaged ? "" : "DELAYED_DURABILITY = DISABLED",
                });

            if (!isManaged)
            {
                alterSet(new List<string> { @"MULTI_USER" });
            }
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
            public string? LogicalName { get; set; }
            public string? PhysicalName { get; set; }
        }
    }
}
