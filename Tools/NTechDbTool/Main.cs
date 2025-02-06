using NDesk.Options;

namespace NTechDbTool
{
    internal static class Main
    {
        public static int Handle(OptionSet options, string operation, string[] args)
        {

            if (operation == "test-db-connection")
            {
                return TestConnection.HandleTestConnection(args);
            }
            else if (operation == "create-system-user-script")
            {
                return SystemUserCreator.CreateScript();
            }
            else if (operation == "create-azure-user-script")
            {
                return AzureAdUserScriptCreator.CreateScript(args);
            }
            else if (operation == "restore")
            {
                return Restore.HandleRestore(args);
            }
            else if (operation == "backup")
            {
                return Backup.HandleBackup(args);
            }
            else if (operation == "batchrestore")
            {
                return Restore.HandleBatchRestore(args);
            }
            else if (operation == "create-empty-databases")
            {
                return CreateEmptyDatabases.HandleCreateEmptyDatabases(args);
            }
            else
            {
                Console.WriteLine("Invalid operation");
                CommandLineUtils.ShowHelp(options, null);
                return 0;
            }
        }
    }
}
