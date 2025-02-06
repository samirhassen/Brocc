using Microsoft.EntityFrameworkCore;
using NTech.Core.Credit.Database;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Customer.Database;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Services;
using NTech.Core.PreCredit.Database;
using NTech.Core.Savings.Database;
using NTech.Core.User.Database;

namespace NTech.Core.Host.IntegrationTests.Shared
{
    internal class TestDatabases
    {
        /// <summary>
        /// This will create empty databases for each service
        /// </summary>
        private static void SetupDatabases(string databasePrefix)
        {
            SetConnectionStrings(databasePrefix);

            EnsureDatabaseEmpty<PreCreditContext>();
            EnsureDatabaseEmpty<CustomerContext>();
            EnsureDatabaseEmpty<CreditContext>();
            EnsureDatabaseEmpty<UserContext>();
            using (var creditContext = new CreditContext())
            {
                CreditContext.TempMigrate(creditContext);
                CreditContextSetup.AfterInitialize(creditContext.Database.GetDbConnection());
            }
            EnsureDatabaseEmpty<SavingsContext>();
            using (var savingsContext = new SavingsContext())
            {
                SavingsContext.TempMigrate(savingsContext);
            }
        }

        private static void EnsureDatabaseEmpty<T>() where T : DbContext, new()
        {
            using var context = new T();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated(); //If we swap to using migrations here use context.Database.Migrate(); instead
        }

        // TODO: This is sketchy. Can we get rid of NEnv.SharedInstance somehow?
        private static void SetConnectionStrings(string databasePrefix)
        {
            NEnv.SharedInstance = new NEnv(
                settingName => throw new Exception("Missing appsetting " + settingName),
                connectionStringName =>
                {
                    if (connectionStringName.EndsWith("Context"))
                    {
                        return $"Server=localhost;Database=IntegrationTest-{databasePrefix}-{connectionStringName};Integrated Security=True;MultipleActiveResultSets=true;Encrypt=False";
                    }
                    throw new Exception("Missing connection string " + connectionStringName);
                });
        }

        private static Mutex databasesMutex = new Mutex();

        /*
         * Use the prefix override when a particular test needs to have it's result kept after running all the tests
         */
        public static void RunTestUsingDatabases(Action a, string? overrideDatabasePrefix = null)
        {
            var mutex = AquireLockAndCreateTestDatabase(overrideDatabasePrefix);
            try
            {
                a();
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        public static Mutex AquireLockAndCreateTestDatabase(string? overrideDatabasePrefix = null)
        {
            databasesMutex.WaitOne();
            SetupDatabases(overrideDatabasePrefix ?? "default");
            CachedSettingsService.ClearCache();
            return databasesMutex;
        }
    }
}
