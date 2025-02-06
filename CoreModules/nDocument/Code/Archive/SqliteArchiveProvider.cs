using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace nDocument.Code.Archive
{
    public class SqliteArchiveProvider : IArchiveProvider
    {
        private readonly Func<FileInfo> getDbFile;
        private static object initLock = new object();
        private static bool initDone = false;

        private class NTechArchiveDocumentV1
        {
            public string ArchiveKey { get; set; }
            public byte[] Data { get; set; }
            public string MetaData { get; set; }
            public string MimeType { get; set; }
            public string Filename { get; set; }
            public string CreationDate { get; set; }

            public void SetMetadata(string key, ArchiveMetadataFetchResult d)
            {
                MetaData = ArchiveMetadataFetchResult.CreateMetadataXml(key, d.ContentType, d.FileName, d.OptionalData).ToString();
            }

            public void SetCreationDate(DateTime d)
            {
                CreationDate = d.ToString("o", CultureInfo.InvariantCulture);
            }
        }

        public static void TryReleaseLockedSqliteInteropDll()
        {
            System.Data.SQLite.SQLiteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private static void Init(FileInfo dbFile)
        {
            //Possible way of adding encryption: https://stackoverflow.com/questions/1381264/password-protect-a-sqlite-db-is-it-possible
            if (initDone)
                return;
            lock (initLock)
            {
                if (initDone)
                    return;

                Directory.CreateDirectory(dbFile.Directory.FullName);
                WithConnection<object>(dbFile, c =>
                {
                    if (c.Query<int>("select count(*) from sqlite_master m where m.type = 'table' and m.name = 'NtechArchiveDocumentV1'").Single() > 0)
                        return null;

                    c.Execute(@"CREATE TABLE NtechArchiveDocumentV1 (ArchiveKey TEXT NOT NULL PRIMARY KEY, Data BLOB NULL, MetaData TEXT NULL, MimeType TEXT NULL, Filename TEXT NULL, CreationDate TEXT NULL)");

                    return null;
                }, skipInit: true);

                initDone = true;
            }
        }

        private static T WithConnection<T>(FileInfo dbFile, Func<SQLiteConnection, T> f, bool skipInit = false)
        {
            if (!skipInit)
                Init(dbFile);
            using (var connection = new SQLiteConnection($"Data Source={dbFile.FullName}"))
            {
                connection.Open();
                return f(connection);
            }
        }

        public SqliteArchiveProvider(Func<FileInfo> getDbFile)
        {
            this.getDbFile = getDbFile;
        }

        public ArchiveFetchResult Fetch(string key)
        {
            return WithConnection(this.getDbFile(), c =>
            {
                var d = c.Query<NTechArchiveDocumentV1>("select * from NtechArchiveDocumentV1 where ArchiveKey=@key", new { key = key }).SingleOrDefault();
                if (d == null)
                    return null;
                else
                {
                    var m = ArchiveMetadataFetchResult.CreateFromXml(XDocuments.Parse(d.MetaData));
                    return new ArchiveFetchResult
                    {
                        Content = new MemoryStream(d.Data),
                        FileName = d.Filename,
                        ContentType = d.MimeType,
                        OptionalData = m.OptionalData
                    };
                }
            });
        }

        public ArchiveMetadataFetchResult FetchMetadataI(string key, SQLiteConnection c)
        {
            var d = c.Query<string>("select MetaData from NtechArchiveDocumentV1 where ArchiveKey=@key", new { key = key }).SingleOrDefault();
            if (d == null)
                return null;
            else
            {
                return ArchiveMetadataFetchResult.CreateFromXml(XDocuments.Parse(d));
            }
        }

        public ArchiveMetadataFetchResult FetchMetadata(string key)
        {
            return WithConnection(this.getDbFile(), c =>
            {
                return FetchMetadataI(key, c);
            });
        }

        public Dictionary<string, ArchiveMetadataFetchResult> FetchMetadataBulk(ISet<string> keys)
        {
            return WithConnection(this.getDbFile(), c =>
            {
                //TODO: Can be optimized a single select if this ever becomes slow
                return keys.ToDictionary(x => x, x => FetchMetadataI(x, c));
            });
        }

        public bool TryStore(byte[] fileBytes, string mimeType, string filename, out string key, out string errorMessage, ArchiveOptionalData optionalData = null)
        {
            string innerkey = null;
            var result = WithConnection<bool>(this.getDbFile(), c =>
            {
                innerkey = Guid.NewGuid().ToString() + Path.GetExtension(filename);

                var a = new NTechArchiveDocumentV1
                {
                    ArchiveKey = innerkey,
                    Data = fileBytes,
                    Filename = filename,
                    MimeType = mimeType
                };
                a.SetCreationDate(DateTime.Now);
                a.SetMetadata(innerkey, new ArchiveMetadataFetchResult { ContentType = mimeType, FileName = filename, OptionalData = optionalData });
                c.Execute("insert into NtechArchiveDocumentV1 (ArchiveKey, Data, MetaData, MimeType, Filename, CreationDate) values (@ArchiveKey, @Data, @MetaData, @MimeType, @Filename, @CreationDate)", param: a);

                return true;
            });

            key = innerkey;
            errorMessage = null;

            return result;
        }

        public bool TryStore(Stream file, string mimeType, string filename, out string key, out string errorMessage, ArchiveOptionalData optionalData = null)
        {
            byte[] filesBytes;
            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                ms.Flush();
                file.Flush();
                filesBytes = ms.ToArray();
            }
            return TryStore(filesBytes, mimeType, filename, out key, out errorMessage, optionalData: optionalData);
        }

        public bool Delete(string key)
        {
            return WithConnection<bool>(this.getDbFile(), c =>
            {
                var deleteCount = c.Execute("delete from NtechArchiveDocumentV1 where ArchiveKey=@key", param: new { key });

                return deleteCount > 0;
            });
        }
    }
}