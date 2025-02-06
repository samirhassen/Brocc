using NDesk.Options;

namespace NTechDbTool
{
    public static class CreateEmptyDatabases
    {
        public const string ManagedMarker = "managed";

        public static int HandleCreateEmptyDatabases(string[] args)
        {
            var showHelp = false;
            string? dbNames = null;
            string? dbFolder = null;
            string? masterDbConnectionString = null;
            bool allowOverwrite = false;
            bool isForTest = false;
            string operation = "create-empty-databases";

            var p = new OptionSet()
                {
                    { "allow-overwrite", "if the database or backup should be overwritten if it exists", v => allowOverwrite = v != null},
                    { "master-db-connectionstring=", "connection string to the master database", v => masterDbConnectionString = v },

                    { "create-db-names=", "name to restore the database as", v => dbNames = v },
                    { "db-folder=", $"folder to keep the database files in. Use --db-folder={ManagedMarker} for cloud managed instances.", v => dbFolder = v },
                    { "for-test", "use if creating a test database. Beware that this sets simple recovery mode so dont transfer these to production.", v => isForTest = v != null }
                };

            p.Parse(args);

            if (showHelp)
            {
                CommandLineUtils.ShowHelp(p, operation);
                return 0;
            }

            if (string.IsNullOrWhiteSpace(dbFolder) || string.IsNullOrWhiteSpace(masterDbConnectionString) || string.IsNullOrWhiteSpace(dbNames))
            {
                Console.WriteLine("Missing required parameter");
                CommandLineUtils.ShowHelp(p, operation);
                return 0;
            }

            var isManaged = dbFolder == ManagedMarker;

            Console.WriteLine($"Database folder: {dbFolder}");

            if (!isManaged && !Directory.Exists(dbFolder))
            {
                Directory.CreateDirectory(dbFolder);
            }

            var mgr = new DevelopmentDatabaseBackupManager(masterDbConnectionString);

            var databaseNames = dbNames.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();

            var systemNames = databaseNames.Where(mgr.IsSystemDatabaseName).ToList();
            if (systemNames.Any())
            {
                Console.WriteLine("System database names are not allowed: " + string.Join(", ", systemNames));
                return -1;
            }

            if (!allowOverwrite)
            {
                var userDbs = mgr.GetAllNonSystemDatabaseNames().Intersect(databaseNames);

                if (userDbs.Any())
                {
                    Console.WriteLine("These databases already exist. Inclue -allow-overwrite if you want to overwrite them: " + string.Join(", ", userDbs));
                    return -1;
                }
            }

            var d = dbFolder == "default" ? null : new DirectoryInfo(dbFolder);
            foreach (var dbName in databaseNames)
            {
                Console.WriteLine($"Creating: {dbName}" + (isManaged ? " (managed)" : ""));
                mgr.CreateEmptyDatabase(d, dbName, isForTest, allowOverwrite, isManaged);
                Console.WriteLine("Connection string:");
                Console.WriteLine(masterDbConnectionString.Replace("master", dbName));
                Console.WriteLine("");
            }

            return 0;
        }
    }
}
