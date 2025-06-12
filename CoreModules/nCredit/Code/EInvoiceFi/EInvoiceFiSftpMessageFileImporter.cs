using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace nCredit.Code.EInvoiceFi
{
    public class EInvoiceFiSftpMessageFileImporter : IDisposable
    {
        private IFtpSource ftpSource;
        private int skipRecentlyWrittenMinutes;

        private EInvoiceFiSftpMessageFileImporter(IFtpSource ftpSource, int skipRecentlyWrittenMinutes)
        {
            this.ftpSource = ftpSource;
            this.skipRecentlyWrittenMinutes = skipRecentlyWrittenMinutes;
        }

        public static EInvoiceFiSftpMessageFileImporter CreateForTesting(IFtpSource source, int skipRecentlyWrittenMinutes)
        {
            return new EInvoiceFiSftpMessageFileImporter(source, skipRecentlyWrittenMinutes);
        }

        public static EInvoiceFiSftpMessageFileImporter Create(string host, string username, string password, int? port, int skipRecentlyWrittenMinutes)
        {
            return new EInvoiceFiSftpMessageFileImporter(new ActualFtpSource(host, username, password, port), skipRecentlyWrittenMinutes);
        }

        public interface IFtpSource
        {
            List<IFtpItem> ListDirectory(string directoryPath);
        }

        public interface IFtpItem
        {
            string FilenameWithoutPath { get; }
            DateTime LastWriteTimeUtc { get; }
            long LengthInBytes { get; }
            bool IsDirectory { get; }

            void Download(Stream target);
            void Delete();
        }

        private class ActualFtpSource : IFtpSource, IDisposable
        {
            private string host;
            private string username;
            private string password;
            private int? port;
            private Renci.SshNet.SftpClient client;

            public ActualFtpSource(string host, string username, string password, int? port)
            {
                this.host = host;
                this.username = username;
                this.password = password;
                this.port = port;
                var connectionInfo = new Renci.SshNet.ConnectionInfo(host,
                                                        port ?? 22,
                                                        username,
                                                        new Renci.SshNet.PasswordAuthenticationMethod(username, password));
                this.client = new Renci.SshNet.SftpClient(connectionInfo);
            }

            private class FileItem : IFtpItem
            {
                private Renci.SshNet.Sftp.SftpFile f;
                private Renci.SshNet.SftpClient c;

                public FileItem(Renci.SshNet.Sftp.SftpFile f, Renci.SshNet.SftpClient c)
                {
                    this.f = f;
                    this.c = c;
                }

                public string FilenameWithoutPath { get { return f.Name; } }

                public DateTime LastWriteTimeUtc { get { return f.LastWriteTimeUtc; } }

                public long LengthInBytes { get { return f.Length; } }

                public bool IsDirectory { get { return f.IsDirectory; } }

                public void Delete()
                {
                    if (!c.IsConnected)
                        c.Connect();
                    c.Delete(f.FullName);
                }

                public void Download(Stream target)
                {
                    if (!c.IsConnected)
                        c.Connect();
                    c.DownloadFile(f.FullName, target);
                }
            }

            public void Dispose()
            {
                client.Dispose();
            }

            public List<IFtpItem> ListDirectory(string directoryPath)
            {
                if (!client.IsConnected)
                    client.Connect();
                return client.ListDirectory(directoryPath).ToList().Select(x => (IFtpItem)new FileItem(x, client)).ToList();
            }
        }

        /// <summary>
        /// Max 5 mb filesize
        /// Files changed within the last x minutes (default 10) are skipped (They will be processed on the next run)
        /// </summary>
        /// <param name="fileNamePattern"></param>
        /// <param name="tryProcessFileWithName"></param>
        public void ImportAndRemoveFiles(string directoryPath, Regex fileNamePattern, Func<MemoryStream, string, bool> tryProcessFileWithName)
        {
            var files = this.ftpSource.ListDirectory(directoryPath);

            files = files.Where(x => !x.IsDirectory).ToList();
            if (fileNamePattern != null)
            {
                files = files.Where(x => fileNamePattern.IsMatch(x.FilenameWithoutPath)).ToList();
            }

            var thresholdTime = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(this.skipRecentlyWrittenMinutes));
            files = files.Where(x => x.LastWriteTimeUtc < thresholdTime).ToList();
            foreach (var f in files)
            {
                if (f.LengthInBytes > 5242880L) //5mb
                    throw new Exception($"Filesize of '{f.FilenameWithoutPath}' exceeds the max allowed size 5mb. It's extremely unlikely that this file actually contains what it's supposed to.");

                using (var ms = new MemoryStream())
                {
                    f.Download(ms);

                    ms.Flush();

                    ms.Position = 0;

                    if (tryProcessFileWithName(ms, f.FilenameWithoutPath))
                    {
                        f.Delete();
                    }
                }
            }
        }

        public void Dispose()
        {
            var d1 = this.ftpSource as IDisposable;
            if (d1 != null) d1.Dispose();
        }
    }
}