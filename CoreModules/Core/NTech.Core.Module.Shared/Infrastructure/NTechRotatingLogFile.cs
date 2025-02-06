using System;
using System.IO;

namespace NTech.Core.Module.Shared.Infrastructure
{
    public class NTechRotatingLogFile
    {
        private readonly string folderName;
        private readonly Func<DateTime> getNow;
        private readonly object writeLock = new object();
        private readonly string filenamePrefix;
        private readonly IFileSystem fileSystem;
        private readonly Func<string, string> formatLogEntry;

        public NTechRotatingLogFile(string folderName, string filenamePrefix, Func<DateTime> getNow, IFileSystem fileSystem, Func<string, string> formatLogEntry = null)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentException("folderName");
            if (string.IsNullOrWhiteSpace(filenamePrefix))
                throw new ArgumentException("filenamePrefix");
            this.folderName = folderName;
            this.getNow = getNow;
            this.filenamePrefix = filenamePrefix;
            this.fileSystem = fileSystem;
            this.formatLogEntry = formatLogEntry;
        }

        public NTechRotatingLogFile(string folderName, string filenamePrefix, Func<string, string> formatLogEntry = null) : this(
            folderName, filenamePrefix,
            () => DateTime.Now, new NTechRotatingLogFile.FileSystem(), formatLogEntry: formatLogEntry)
        {

        }

        private string FormatLogEntry(string logText)
        {
            if (this.formatLogEntry != null)
                return this.formatLogEntry(logText);

            const string s = "--------------------------------------------------";
            var n = Environment.NewLine;
            return $"{s}{n}Date={getNow().ToString("o")}{n}{logText}{n}";
        }

        public void Log(string logText, string contextPrefix = null)
        {
            contextPrefix = (contextPrefix ?? "default").Trim();
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                contextPrefix = contextPrefix.Replace(c, '_');
            }
            var now = getNow();
            var day = now.Day;
            var suffix = day <= 8 ? 1 : (day <= 15 ? 2 : (day <= 22 ? 3 : 4));
            var fileName = Path.Combine(this.folderName, $"{filenamePrefix}-{contextPrefix}-{suffix}.txt");
            try
            {
                lock (writeLock)
                {
                    fileSystem.CreateDirectory(folderName);
                    if (fileSystem.ExistsFile(fileName) && fileSystem.GetLastFileWriteTime(fileName) < now.AddDays(-15))
                    {
                        //Coming back around one month later. Clear the log
                        fileSystem.DeleteFile(fileName);
                    }
                    fileSystem.AppendAllTextToFile(fileName, FormatLogEntry(logText));
                }
            }
            catch
            {
                /*Ignored*/
            }
        }

        public class FileSystem : IFileSystem
        {
            public void AppendAllTextToFile(string path, string contents)
            {
                File.AppendAllText(path, contents);
            }

            public void CreateDirectory(string directoryName)
            {
                Directory.CreateDirectory(directoryName);
            }

            public void DeleteFile(string filename)
            {
                File.Delete(filename);
            }

            public bool ExistsFile(string filename)
            {
                return File.Exists(filename);
            }

            public DateTime GetLastFileWriteTime(string filename)
            {
                return new FileInfo(filename).LastWriteTime;
            }
        }

        public interface IFileSystem
        {
            void AppendAllTextToFile(string path, string contents);
            void DeleteFile(string filename);
            bool ExistsFile(string filename);
            DateTime GetLastFileWriteTime(string filename);
            void CreateDirectory(string directoryName);
        }
    }
}
