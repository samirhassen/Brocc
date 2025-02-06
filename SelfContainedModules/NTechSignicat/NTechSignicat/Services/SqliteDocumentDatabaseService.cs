using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using NTech.Services.Infrastructure;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace NTechSignicat.Services
{
    //Needs to be a singleton
    public class SqliteDocumentDatabaseService : IDocumentDatabaseService, IHostedService
    {
        private FileInfo activeDatabaseFileName;
        private readonly INEnv env;
        private readonly SignicatSettings signicatSettings;
        private readonly IServiceProvider serviceProvider;

        public SqliteDocumentDatabaseService(INEnv env, SignicatSettings signicatSettings, IServiceProvider serviceProvider)
        {
            this.env = env;
            this.signicatSettings = signicatSettings;
            this.serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var fileName = new FileInfo(this.signicatSettings.SqliteDocumentDbFile);
                Init(fileName);
                activeDatabaseFileName = fileName;

                DeleteAllExpired();

                return Task.CompletedTask;
            }
            catch(Exception ex)
            {
                FatalExit(ex);
                return Task.CompletedTask;
            }
        }

        private void FatalExit(Exception ex)
        {
            Console.WriteLine(ex);

            Console.WriteLine($"Process exiting due to SqliteDocumentDatabaseService start failed");

            ((IHostApplicationLifetime)serviceProvider.GetService(typeof(IHostApplicationLifetime)))
                                            .StopApplication();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            activeDatabaseFileName = null;
            try
            {
                SQLiteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch {  /* ignored */ }
            return Task.CompletedTask;
        }

        private T WithConnection<T>(Func<SQLiteConnection, T> f)
        {
            if (activeDatabaseFileName == null)
                throw new Exception("Sqlite database not active");
            return WithConnectionShared(activeDatabaseFileName, f);
        }

        private static void Init(FileInfo dbFile)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dbFile.FullName));
            WithConnectionShared<object>(dbFile, c =>
            {
                if (c.Query<int>("select count(*) from sqlite_master m where m.type = 'table' and m.name = 'NtechKeyValueItemV1'").Single() > 0)
                    return null;

                c.Execute(@"CREATE TABLE NtechKeyValueItemV1 (Key TEXT NOT NULL PRIMARY KEY, Value TEXT NULL, DeleteAfterTicks NUMBER NULL)");

                return null;
            }, skipInit: true);
        }

        private static T WithConnectionShared<T>(FileInfo dbFile, Func<SQLiteConnection, T> f, bool skipInit = false)
        {
            if (!skipInit)
                Init(dbFile);
            using (var connection = new SQLiteConnection($"Data Source={dbFile.FullName}"))
            {
                connection.Open();
                return f(connection);
            }
        }

        private static string ToCompositeKey(string keySpace, string key)
        {
            if (string.IsNullOrWhiteSpace(keySpace) || keySpace.Contains("#"))
                throw new ArgumentException("Must not be empty or containt '#'", "keySpace");
            if (string.IsNullOrWhiteSpace(key) || key.Contains("#"))
                throw new ArgumentException("Must not be empty or containt '#'", "key");
            return $"{keySpace.Trim()}#{key.Trim()}";
        }

        private void SetRaw(string keySpace, string key, string value, TimeSpan? deleteAfter)
        {
            WithConnection<object>(c =>
            {
                c.Execute(@"INSERT INTO NtechKeyValueItemV1(Key, Value, DeleteAfterTicks) VALUES(@Key, @Value, @DeleteAfterTicks) 
                            ON CONFLICT(Key) DO UPDATE SET Value=excluded.Value, DeleteAfterTicks=excluded.DeleteAfterTicks", param: new
                {
                    Key = ToCompositeKey(keySpace, key),
                    Value = value,
                    DeleteAfterTicks = deleteAfter.HasValue ?  DateTime.Now.Add(deleteAfter.Value).Ticks : new long?()
                });
                return null;
            });
        }

        private string GetRaw(string keySpace, string key)
        {
            return WithConnection(c =>
                       c.Query<string>($"select Value from NtechKeyValueItemV1 where Key = @Key", param: new { Key = ToCompositeKey(keySpace, key) }).FirstOrDefault()
            );
        }

        public void Set<T>(string keySpace, string key, T value, TimeSpan? deleteAfter) where T : class
        {
            SetRaw(keySpace, key, JsonConvert.SerializeObject(new StoredDocument<T>
            {
                Version = CurrentVersion,
                Data = value
            }), deleteAfter);
        }

        public const string CurrentVersion = "1";

        public T Get<T>(string keySpace, string key) where T : class
        {
            var v = GetRaw(keySpace, key);
            if (v == null)
                return null;
            var storedDocument = JsonConvert.DeserializeObject<StoredDocument<T>>(v);
            if (storedDocument?.Version != CurrentVersion)
                return null;
            return storedDocument.Data;
        }

        public bool Delete(string keySpace, string key)
        {
            return WithConnection<int>(c => c.Execute("delete from NtechKeyValueItemV1 where Key = @Key")) > 0;
        }

        public int DeleteAllExpired()
        {
            var nowTicks = DateTime.Now.Ticks;
            return WithConnection<int>(c => c.Execute("delete from NtechKeyValueItemV1 where DeleteAfterTicks < @NowTicks", param: new { NowTicks = nowTicks }));
        }

        public bool DeleteAll()
        {
            return WithConnection<int>(c => c.Execute("delete from NtechKeyValueItemV1")) > 0;
        }

        private class StoredDocument<T>
        {
            public string Version { get; set; }
            public T Data { get; set; }
        }
    }

    public interface IDocumentDatabaseService
    {
        void Set<T>(string keySpace, string key, T value, TimeSpan? deleteAfter) where T : class;
        T Get<T>(string keySpace, string key) where T : class;
        bool Delete(string keySpace, string key);
        int DeleteAllExpired();
        bool DeleteAll();
    }
}
