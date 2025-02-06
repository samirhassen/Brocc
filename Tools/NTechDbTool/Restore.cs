using NDesk.Options;

namespace NTechDbTool
{
    public static class Restore
    {
        public static int HandleRestore(string[] args)
        {
            var showHelp = false;
            string? backupFile = null;
            string? dbName = null;
            string? dbFolder = null;
            string? masterDbConnectionString = null;
            bool allowOverwrite = false;
            string operation = "restore";

            var p = new OptionSet()
                {
                    { "allow-overwrite", "if the database or backup should be overwritten if it exists", v => allowOverwrite = v != null},
                    { "master-db-connectionstring=", "connection string to the master database", v => masterDbConnectionString = v },

                    { "backup-file=", ".bak file to restore from", v => backupFile = v },
                    { "db-name=", "name to restore the database as", v => dbName = v },
                    { "db-folder=", "folder to keep the database files in", v => dbFolder = v }
                };

            p.Parse(args);

            if (showHelp)
            {
                CommandLineUtils.ShowHelp(p, operation);
                return 0;
            }
            if (string.IsNullOrWhiteSpace(dbFolder) || string.IsNullOrWhiteSpace(backupFile) || string.IsNullOrWhiteSpace(masterDbConnectionString) || string.IsNullOrWhiteSpace(dbName))
            {
                Console.WriteLine("Missing required parameter");
                CommandLineUtils.ShowHelp(p, operation);
                return 0;
            }

            if (!File.Exists(backupFile))
            {
                throw new Exception($"File does not exist: {backupFile}");
            }

            var mgr = new DevelopmentDatabaseBackupManager(masterDbConnectionString);
            Console.WriteLine($"Restoring {dbName} from '{backupFile}' to '{dbFolder}");
            mgr.RestoreDatabase(backupFile, dbName, allowOverwrite, dbFolder);

            return 0;
        }

        public static int HandleBatchRestore(string[] args)
        {
            var showHelp = false;
            string? dbNames = null;
            string? dbFolder = null;
            string? backupFolder = null;
            string? masterDbConnectionString = null;
            bool allowOverwrite = false;
            string operation = "batchrestore";

            var p = new OptionSet()
                {
                    { "allow-overwrite", "if the database or backup should be overwritten if it exists", v => allowOverwrite = v != null},
                    { "master-db-connectionstring=", "connection string to the master database", v => masterDbConnectionString = v },
                    { "backup-folder=", "folder to keep the database files in", v => backupFolder = v },
                    { "db-names=", "name to restore the databases as. Comma separated list.", v => dbNames = v },
                    { "db-folder=", "folder to keep the database files in", v => dbFolder = v }
                };

            p.Parse(args);

            if (showHelp)
            {
                CommandLineUtils.ShowHelp(p, operation);
                return 0;
            }
            if (string.IsNullOrWhiteSpace(dbFolder) || string.IsNullOrWhiteSpace(backupFolder) || string.IsNullOrWhiteSpace(masterDbConnectionString) || string.IsNullOrWhiteSpace(dbNames))
            {
                Console.WriteLine("Missing required parameter");
                CommandLineUtils.ShowHelp(p, operation);
                return 0;
            }
            var databaseNames = dbNames.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();

            foreach (var dbName in databaseNames)
            {
                var backupFile = Path.Combine(backupFolder, $"{dbName}.bak");
                if (!File.Exists(backupFile))
                {
                    throw new Exception($"File does not exist: {backupFile}");
                }

                var mgr = new DevelopmentDatabaseBackupManager(masterDbConnectionString);
                Console.WriteLine($"Restoring {dbName} from '{backupFile}' to '{dbFolder}");
                mgr.RestoreDatabase(backupFile, dbName, allowOverwrite, dbFolder);
            }

            return 0;
        }
    }
}
