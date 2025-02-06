using NDesk.Options;

namespace NTechDbTool
{
    internal static class CommandLineUtils
    {
        public static void ShowHelp(OptionSet p, string? operation)
        {
            Console.WriteLine("Usage: NTechDbTool [OPTIONS]+");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
            if (operation == "restore")
            {
                Console.WriteLine();
                Console.WriteLine("Example of restore:");
                Console.WriteLine(@"Will restore the backup restore-from-me.bak to a database called test1 placing the data files and logfiles in the c:\temp\databases folder. Use -allow-overwrite if the restore should overwrite test1 if it exists.");
                Console.WriteLine($"NDevDbTool --operation==restore --backup-file=\"c:\\temp\\restore-from-me.bak\" --db-name=\"test1\" --db-folder=\"c:\\temp\\databases\" --master-db-connectionstring=\"Server=localhost;Database=master;Integrated Security=True\"");
            }
            else if (operation == "backup")
            {
                Console.WriteLine();
                Console.WriteLine("Example of backup:");
                Console.WriteLine(@"Will backup the databases user and customer to the folder c:\temp\mybackups. Use -allow-overwrite if existing backups in the same folder should be overwritten.");
                Console.WriteLine($"NDevDbTool --operation=backup --backup-db-names=\"user,customer\" --backup-target-folder=\"c:\\temp\\mybackups\" --master-db-connectionstring=\"Server=localhost;Database=master;Integrated Security=True\"");
            }
            else if (operation == "batchrestore")
            {
                Console.WriteLine();
                Console.WriteLine("Example of batchrestore:");
                Console.WriteLine(@"Will restore the databases db1 and db2 from the backups db1.bak and db2 placing the data files and logfiles in the c:\temp\databases folder. Use -allow-overwrite if the restore should overwrite the db if it exists.");
                Console.WriteLine($"NDevDbTool --operation==batchrestore --db-names=\"db1,db2\" --db-folder=\"c:\\temp\\databases\" --backup-folder=\"c:\\temp\\backups\" --master-db-connectionstring=\"Server=localhost;Database=master;Integrated Security=True\"");
            }
            else if (operation == "import-bookkeeping-rules")
            {
                Console.WriteLine();
                Console.WriteLine("Example of import-bookkeeping-rules:");
                Console.WriteLine(@"Will create bookkeeping-rules file in a xml format ");
                Console.WriteLine($"NDevDbTool --operation=\"import - bookkeeping - rules\" --pathImportBookKeepingRules=\"C:\\Bokföring Draft Kontoplan.xlsx\" --pathImportBookKeepingRulesFile=\"C:\\bookkeeping.xml\"");
            }
            else if (operation == "create-empty-databases")
            {
                Console.WriteLine();
                Console.WriteLine("Example of create-empty-databases:");
                Console.WriteLine(@"Will create new empty databases db1 and db2 placing the data files and logfiles in the c:\temp\databases folder.");
                Console.WriteLine($">> NDevDbTool --operation==create-empty-databases --create-db-names=\"db1,db2\" --db-folder=\"c:\\temp\\databases\" --master-db-connectionstring=\"Server=localhost;Database=master;Integrated Security=True\"");
                Console.WriteLine("Use -allow-overwrite if existing databases should be overwritten.");
                Console.WriteLine("Use -for-test if creating a test database. Beware that this sets simple recovery mode so dont transfer these to production.");
            }
        }
    }
}
