using Microsoft.Extensions.Configuration;
using NDesk.Options;
using NTechDbTool;
using System.Diagnostics;

try
{
    var showHelp = false;
    string? backupFile = null;
    string? dbName = null;
    string? createDbNames = null;
    string? dbFolder = null;
    string? masterDbConnectionString = null;
    bool allowOverwrite = false;
    string? operation = null;
    string? backupDbNames = null;
    string? backupTargetFolder = null;

#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
    var builder = new ConfigurationBuilder()
        .SetBasePath(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName))
        .AddJsonFile("NTechDbTool.config.json", optional: true);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8604 // Possible null reference argument.

    IConfiguration config = builder.Build();

    var pPre = new OptionSet()
                {
                    { "operation=", "backup | restore | create-empty-databases | create-system-user-script | create-azure-user-script | test-connection", v => operation = v },
                    { "allow-overwrite", "if the database or backup should be overwritten if it exists", v => allowOverwrite = v != null},
                    { "master-db-connectionstring=", "connection string to the master database", v => masterDbConnectionString = v },

                    //Create and restore
                    { "db-folder=", "folder to keep the database files in", v => dbFolder = v },

                    //Restore only
                    { "backup-file=", ".bak file to restore from", v => backupFile = v },
                    { "db-name=", "name to restore the database as", v => dbName = v },                    

                    //Backup only
                    { "backup-db-names=", "Databases to backup. For example: user,customer", v => backupDbNames = v },
                    { "backup-target-folder", "Databases will be backed up to this folder to files names <db-name>.bak", v => backupTargetFolder = v },

                    //Create only
                    { "create-db-names=", "Databases to create. For example: user,customer", v => createDbNames = v },

                    { "h|help",  "show this message and exit", v => showHelp = v != null },
                };

    pPre.Parse(args);

    if (showHelp && operation == null)
    {
        CommandLineUtils.ShowHelp(pPre, operation);
        return 0;
    }

    if (string.IsNullOrWhiteSpace(operation))
    {
        Console.WriteLine("Missing required parameter operation");
        CommandLineUtils.ShowHelp(pPre, operation);
        return 0;
    }

    string[] derviedArgs = args;

    if (string.IsNullOrWhiteSpace(masterDbConnectionString))
    {
        var settingMasterDbConnectionString = config.GetConnectionString("master");
        if (!string.IsNullOrWhiteSpace(settingMasterDbConnectionString))
        {
            Console.WriteLine($"Using connection string from NTechDbTool.config.json: {settingMasterDbConnectionString}");
            Console.WriteLine();
            derviedArgs = derviedArgs.Concat(new[] { "--master-db-connectionstring=" + settingMasterDbConnectionString }).ToArray();
        }
    }

    return Main.Handle(pPre, operation, derviedArgs);

}
catch (Exception ex)
{
    Console.WriteLine(ex);
    return -1;
}


