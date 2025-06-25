using System.IO;
using System.Web.Configuration;
using System.Web.Hosting;
using nTest.RandomDataSource;

namespace nTest.Code
{
    public class DbSingleton : IRegisteredObject
    {
        private static DbSingleton instance;
        private static object instanceLock = new object();
        public IDocumentDatabase Db { get; }

        private static string DbConnectionString =>
            WebConfigurationManager
                .ConnectionStrings[NEnv.UseSqlServerDocumentDb ? "TestSqlServerDb" : "TestSqliteDb"]
                ?.ConnectionString;

        private DbSingleton()
        {
            if (NEnv.UseSqlServerDocumentDb)
            {
                Db = new SqlDocumentDatabase(DbConnectionString);
            }
            else
            {
                var f = SqliteDocumentDatabase.ParseFileFromConnectionString(DbConnectionString);
                if (!f.Exists)
                {
                    Directory.CreateDirectory(f.Directory.FullName);
                }

                Db = SqliteDocumentDatabase.FromFile(f);
            }
        }

        public static DbSingleton SharedInstance
        {
            get
            {
                if (instance != null) return instance;
                lock (instanceLock)
                {
                    if (instance != null) return instance;
                    instance = new DbSingleton();
                    HostingEnvironment.RegisterObject(instance);
                }

                return instance;
            }
        }

        public static void DeleteDatabase()
        {
            SharedInstance.Db.DeleteDatabaseIfExists();
        }

        public void Stop(bool immediate)
        {
            try
            {
                instance?.Db.Dispose();
            }
            catch
            {
                /* Ignored */
            }
        }
    }
}