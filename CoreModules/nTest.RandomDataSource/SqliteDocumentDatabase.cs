using Dapper;
using Newtonsoft.Json;
using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace nTest.RandomDataSource
{
    public interface IDocumentDatabase : IDisposable
    {
        IDocumentDatabaseUnitOfWork BeginTransaction();
        bool DeleteDatabaseIfExists();
        void ResetExistanceFlag();
    }

    public interface IDocumentDatabaseUnitOfWork : IDisposable
    {
        void AddOrUpdate<T>(string key, string collection, T value);
        T Get<T>(string key, string collection);
        T GetAnonymousType<T>(string key, string collection, T anonymousTypeObject);
        void Commit();
    }

    public class SqliteDocumentDatabase : IDocumentDatabase
    {
        private readonly string dbFilePath;
        private readonly object metaLock = new object();
        private bool existanceEnsured = false;

        private SqliteDocumentDatabase(FileInfo dbFilePath)
        {
            this.dbFilePath = dbFilePath.FullName;
        }

        public static FileInfo ParseFileFromConnectionString(string connectionString)
        {
            SQLiteConnectionStringBuilder a = new SQLiteConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(a.DataSource))
            {
                throw new Exception("Missing 'Data Source'");
            }
            return new FileInfo(a.DataSource);
        }

        public static SqliteDocumentDatabase FromConnectionString(string connectionString)
        {
            return FromFile(ParseFileFromConnectionString(connectionString));
        }

        public static SqliteDocumentDatabase FromFile(FileInfo dbFilePath)
        {
            return new SqliteDocumentDatabase(dbFilePath);
        }

        private SQLiteConnection CreateConnection(bool failIfMissing)
        {
            if (failIfMissing)
                EnsureExists();

            return new SQLiteConnection($"Data Source={dbFilePath};Version=3;FailIfMissing={failIfMissing};Pooling=False");
        }

        private void EnsureExists()
        {
            if (existanceEnsured)
                return;
            lock (metaLock)
            {
                if (existanceEnsured)
                    return;
                if (File.Exists(dbFilePath))
                {
                    existanceEnsured = true;
                    return;
                }

                CreateDatabase();
                existanceEnsured = true;
            }
        }

        private void CreateDatabase()
        {
            if (File.Exists(dbFilePath))
                throw new Exception($"Database already exists: {dbFilePath}");

            using (var c = CreateConnection(false))
            {
                c.Open();
                using (var tr = c.BeginTransaction())
                {
                    if (c.QueryFirst<int>("SELECT count(*) FROM sqlite_master WHERE type = 'table' and name = 'ntech_document_db_v1'") == 0)
                    {
                        c.Execute("create table ntech_document_db_v1 (key text not null primary key, collection text not null, value text null)", transaction: tr);
                        c.Execute("create index ntech_document_db_v1_idx1 on ntech_document_db_v1 (key, collection)", transaction: tr);
                        tr.Commit();
                    }
                }
            }
        }

        /// <summary>
        /// Use a copy of the included db file instead of the one currently in use
        /// </summary>
        public void ReplaceDatabase(FileInfo pathToNewDatabase)
        {
            lock (metaLock)
            {
                SQLiteConnection.ClearAllPools();
                if (File.Exists(dbFilePath))
                    File.Delete(dbFilePath);
                File.Copy(pathToNewDatabase.FullName, dbFilePath);
            }
        }

        public bool DeleteDatabaseIfExists()
        {
            bool exists;
            lock (metaLock)
            {
                SQLiteConnection.ClearAllPools();
                exists = File.Exists(dbFilePath);
                if (exists)
                    File.Delete(dbFilePath);
                existanceEnsured = false;
            }
            return exists;
        }

        public IDocumentDatabaseUnitOfWork BeginTransaction()
        {
            return new UnitOfWork(CreateConnection(true));
        }

        bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // On dispose but not finalize (for managed objects)
            }

            // On finalize or dispose

            SQLiteConnection.ClearAllPools(); //Trying to make sqlite let go of it's file lock

            disposed = true;
        }

        public static void TryReleaseLockedSqliteInteropDll()
        {
            System.Data.SQLite.SQLiteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void ResetExistanceFlag()
        {
            existanceEnsured = false;
        }

        ~SqliteDocumentDatabase()
        {
            Dispose(false);
        }

        public class UnitOfWork : IDocumentDatabaseUnitOfWork
        {
            private readonly SQLiteConnection conn;
            private readonly SQLiteTransaction tr;
            private bool isCompleted;

            public UnitOfWork(SQLiteConnection conn)
            {
                this.conn = conn;
                this.conn.Open();
                this.tr = conn.BeginTransaction();
                this.isCompleted = false;
            }

            private void EnsureState()
            {
                if (isCompleted)
                    throw new Exception("A single unit of work can only have one commit.");
            }

            public void Commit()
            {
                EnsureState();
                isCompleted = true;
                tr.Commit();
            }

            private void CheckKeyAndCollection(string key, string collection)
            {
                if (string.IsNullOrWhiteSpace(key) || key.Length > 128)
                    throw new ArgumentException("Invalid key", "key");
                if (string.IsNullOrWhiteSpace(collection) || collection.Length > 128)
                    throw new ArgumentException("Invalid key", "collection");
            }

            public void AddOrUpdate<T>(string key, string collection, T value)
            {
                CheckKeyAndCollection(key, collection);
                EnsureState();

                conn.Execute("INSERT OR REPLACE INTO ntech_document_db_v1 (key, collection, value) VALUES (@key, @collection, @value)", new
                {
                    key,
                    collection,
                    value = JsonConvert.SerializeObject(value)
                }, transaction: tr);
            }

            public T Get<T>(string key, string collection)
            {
                CheckKeyAndCollection(key, collection);
                EnsureState();

                var i = conn.Query<Item>("select value from ntech_document_db_v1 where key = @key and collection = @collection", new { key, collection }, transaction: tr).SingleOrDefault();
                if (i == null)
                    return default(T);
                else
                    return JsonConvert.DeserializeObject<T>(i.value);
            }

            public T GetAnonymousType<T>(string key, string collection, T anonymousTypeObject)
            {
                CheckKeyAndCollection(key, collection);
                EnsureState();

                var i = conn.Query<Item>("select value from ntech_document_db_v1 where key = @key and collection = @collection", new { key, collection }, transaction: tr).SingleOrDefault();
                if (i == null)
                    return default(T);
                else
                    return JsonConvert.DeserializeAnonymousType<T>(i.value, anonymousTypeObject);
            }

            public void Dispose()
            {
                if (!isCompleted)
                {
                    tr.Rollback();
                }
                tr.Dispose();
                conn.Dispose();
            }

            private class Item
            {
                public string value { get; set; }
            }
        }
    }
}