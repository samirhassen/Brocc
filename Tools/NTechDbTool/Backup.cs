using NDesk.Options;

namespace NTechDbTool
{
    public static class Backup
    {
        public static int HandleBackup(string[] args)
        {
            var showHelp = false;
            string? masterDbConnectionString = null;
            bool allowOverwrite = false;
            string? backupDbNames = null;
            string? backupTargetFolder = null;
            string operation = "backup";

            var p = new OptionSet()
                {
                    { "allow-overwrite", "if the database or backup should be overwritten if it exists", v => allowOverwrite = v != null},
                    { "master-db-connectionstring=", "connection string to the master database", v => masterDbConnectionString = v },

                    { "backup-db-names=", "Databases to backup. For example: user,customer or * to take all non system databases", v => backupDbNames = v },
                    { "backup-target-folder=", "Databases will be backed up to this folder to files names <db-name>.bak", v => backupTargetFolder = v },
                };

            p.Parse(args);
            Console.WriteLine(masterDbConnectionString);
            if (showHelp)
            {
                CommandLineUtils.ShowHelp(p, operation);
                return 0;
            }
            if (string.IsNullOrWhiteSpace(backupDbNames) || string.IsNullOrWhiteSpace(backupTargetFolder) || string.IsNullOrWhiteSpace(masterDbConnectionString))
            {
                Console.WriteLine("Missing required parameter");
                CommandLineUtils.ShowHelp(p, operation);
                return 0;
            }

            var mgr = new DevelopmentDatabaseBackupManager(masterDbConnectionString);

            List<string> names;

            if (backupDbNames?.Trim() == "*")
            {
                names = mgr.GetAllNonSystemDatabaseNames();
            }
            else
            {
                names = (backupDbNames ?? "").Split(',')
                    .Select(x => x?.Trim() ?? "")
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();
            }

            Directory.CreateDirectory(backupTargetFolder);
            foreach (var name in names)
            {
                var backupFilename = Path.Combine(backupTargetFolder, $"{name}.bak");
                Console.WriteLine($"Backing up {name} to '{backupFilename}'");
                mgr.BackupDatabase(name, backupFilename, allowOverwrite);
            }

            return 0;
        }
    }
}
