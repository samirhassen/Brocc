using Dapper;
using Newtonsoft.Json;
using System;
using System.Data.SqlClient;
using System.Linq;

namespace nTest.RandomDataSource
{
    public class SqlDocumentDatabase : IDocumentDatabase
    {
        private readonly Func<SqlConnection> createConnection;
        private readonly object metaLock = new object();
        private bool existanceEnsured = false;

        private const string DocumentTableName = "ntech_document_db_v1";

        public SqlDocumentDatabase(string connectionstring) : this(() => new SqlConnection(connectionstring))
        {

        }

        public SqlDocumentDatabase(Func<SqlConnection> createConnection)
        {
            this.createConnection = createConnection;
        }

        public void ResetExistanceFlag()
        {
            existanceEnsured = false;
        }

        public IDocumentDatabaseUnitOfWork BeginTransaction()
        {
            return new UnitOfWork(CreateConnection(true));
        }

        private SqlConnection CreateConnection(bool failIfMissing)
        {
            if (failIfMissing)
                EnsureExists();

            return createConnection();
        }

        private void EnsureExists()
        {
            //NOTE: For sql server as opposed to sqlite we assume that the actual database exists, this just drops and creates tables.
            if (existanceEnsured)
                return;
            lock (metaLock)
            {
                if (existanceEnsured)
                    return;

                using (var c = CreateConnection(false))
                {
                    c.Open();
                    if (ExistsDocumentTable(c, null))
                    {
                        existanceEnsured = true;
                        return;
                    }
                }

                CreateDatabase();
                existanceEnsured = true;
            }
        }

        public bool DeleteDatabaseIfExists()
        {
            bool exists;
            lock (metaLock)
            {
                using (var c = CreateConnection(false))
                {
                    exists = ExistsDocumentTable(c, null);
                    if (exists)
                    {
                        c.Execute($"drop table {DocumentTableName}");
                    }
                    existanceEnsured = false;
                }
            }
            return exists;
        }

        private bool ExistsDocumentTable(SqlConnection c, SqlTransaction tr)
        {
            return c.QueryFirst<int>($"SELECT count(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{DocumentTableName}'", transaction: tr) > 0;
        }

        private void CreateDatabase()
        {
            using (var c = CreateConnection(false))
            {
                c.Open();
                using (var tr = c.BeginTransaction())
                {
                    if (!ExistsDocumentTable(c, tr))
                    {
                        c.Execute($"create table {DocumentTableName} ([key] nvarchar(256) not null primary key, [collection] nvarchar(256) not null, value nvarchar(max) null)", transaction: tr);
                        c.Execute($"create index {DocumentTableName}_idx1 on {DocumentTableName} ([key], [collection])", transaction: tr);
                        tr.Commit();
                    }
                }
            }
        }



        public class UnitOfWork : IDocumentDatabaseUnitOfWork
        {
            private readonly SqlConnection conn;
            private readonly SqlTransaction tr;
            private bool isCompleted;

            public UnitOfWork(SqlConnection conn)
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

                conn.Execute($"merge dbo.{DocumentTableName} as [Target] using (select @key as [key], @collection as [collection], @value as value) as [Source] on [Target].[collection] = [Source].[collection] and [Target].[key] = [Source].[key] when matched then update set [Target].value = [Source].value when not matched then insert ([key], [collection], value) values ([Source].[key], [Source].[collection], [Source].value);", new
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

                var i = conn.Query<Item>($"select value from {DocumentTableName} where [key] = @key and [collection] = @collection", new { key, collection }, transaction: tr).SingleOrDefault();
                if (i == null)
                    return default(T);
                else
                    return JsonConvert.DeserializeObject<T>(i.value);
            }

            public T GetAnonymousType<T>(string key, string collection, T anonymousTypeObject)
            {
                CheckKeyAndCollection(key, collection);
                EnsureState();

                var i = conn.Query<Item>($"select value from {DocumentTableName} where [key] = @key and [collection] = @collection", new { key, collection }, transaction: tr).SingleOrDefault();
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

            disposed = true;
        }
        ~SqlDocumentDatabase()
        {
            Dispose(false);
        }
    }
}
