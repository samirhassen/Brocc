using System;
using System.IO;

namespace nCustomerPages.Code
{
    public class IncomingApiCallLog
    {
        private readonly string folderName;
        private readonly Func<DateTime> getNow;
        private readonly Func<FileInfo, DateTime> getLastWrittenDate;
        private readonly object writeLock = new object();

        public IncomingApiCallLog(string folderName, Func<DateTime> getNow, Func<FileInfo, DateTime> getLastWrittenDate)
        {
            this.folderName = folderName;
            this.getNow = getNow;
            this.getLastWrittenDate = getLastWrittenDate;
        }

        public IncomingApiCallLog(string folderName) : this(folderName, () => DateTime.UtcNow, f => f.LastWriteTimeUtc)
        {

        }

        private static Lazy<IncomingApiCallLog> sharedInstance = new Lazy<IncomingApiCallLog>(() => new IncomingApiCallLog(Path.Combine(NEnv.LogFolder, @"nCustomerPages\ProviderApplications")));

        public static IncomingApiCallLog SharedInstance
        {
            get
            {
                return sharedInstance.Value;
            }
        }

        private string FormatLogEntry(string logText)
        {
            const string s = "--------------------------------------------------";
            var n = Environment.NewLine;
            return $"{s}{n}Date={DateTimeOffset.Now.ToString("o")}{n}{logText}{n}";
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
            var fileName = Path.Combine(this.folderName, $"log-{contextPrefix}-{suffix}.txt");
            try
            {
                lock (writeLock)
                {
                    System.IO.Directory.CreateDirectory(folderName);
                    if (File.Exists(fileName) && getLastWrittenDate(new FileInfo(fileName)) < now.AddDays(-15))
                    {
                        //Coming back around one month later. Clear the log
                        File.Delete(fileName);
                    }
                    System.IO.File.AppendAllText(fileName, FormatLogEntry(logText));
                }
            }
            catch
            {
                /*Ignored*/
            }
        }
    }
}
