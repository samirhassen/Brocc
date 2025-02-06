using Dapper;
using Microsoft.Data.SqlClient;
using NDesk.Options;

namespace NTechDbTool
{
    public class TestConnection
    {
        public static int HandleTestConnection(string[] args)
        {
            string? masterDbConnectionString = null;

            var p = new OptionSet()
                {
                    { "master-db-connectionstring=", "connection string to the master database", v => masterDbConnectionString = v }
                };

            p.Parse(args);

            if (string.IsNullOrWhiteSpace(masterDbConnectionString))
            {
                CommandLineUtils.ShowHelp(p, "test-connection");
                return 0;
            }

            try
            {
                using (var masterDbConnection = new SqlConnection(masterDbConnectionString))
                {
                    masterDbConnection.Open();
                    var dbNames = masterDbConnection.Query<string>(
                        "select [name] from sys.databases where name NOT IN ('master','model','msdb','tempdb')",
                        commandTimeout: 60).ToList(); ;
                    Console.WriteLine("Connection: ok");
                    Console.WriteLine($"Databases: {string.Join(", ", dbNames)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Connection: *failed*");
                Console.WriteLine(ex.ToString());
            }
            return 0;
        }
    }
}
