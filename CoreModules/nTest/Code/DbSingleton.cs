using nTest.RandomDataSource;
using System.Web.Configuration;
using System.Web.Hosting;

namespace nTest.Code
{
    public class DbSingleton : IRegisteredObject
    {
        private static DbSingleton instance;
        private static object instanceLock = new object();
        private IDocumentDatabase db;

        private static string DbConnectionString
        {
            get
            {
                return WebConfigurationManager.ConnectionStrings[NEnv.UseSqlServerDocumentDb ? "TestSqlServerDb" : "TestSqliteDb"]?.ConnectionString;
            }
        }

        private DbSingleton()
        {
            if (NEnv.UseSqlServerDocumentDb)
            {
                db = new SqlDocumentDatabase(DbConnectionString);
            }
            else
            {
                var f = SqliteDocumentDatabase.ParseFileFromConnectionString(DbConnectionString);
                if (!f.Exists)
                {
                    System.IO.Directory.CreateDirectory(f.Directory.FullName);
                }
                db = SqliteDocumentDatabase.FromFile(f);
            }
        }

        public IDocumentDatabase Db
        {
            get
            {
                return db;
            }
        }

        public static DbSingleton SharedInstance
        {
            get
            {
                if (instance == null)
                {
                    lock (instanceLock)
                    {
                        if (instance == null)
                        {
                            instance = new DbSingleton();
                            HostingEnvironment.RegisterObject(instance);
                        }
                    }
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
                if (instance != null)
                    instance.Db.Dispose();
            }
            catch { /* Ignored */ }
        }
    }
}