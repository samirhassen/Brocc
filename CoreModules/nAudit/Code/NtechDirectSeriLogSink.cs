using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nAudit
{
    public class NtechDirectSeriLogSink : PeriodicBatchingSink
    {
        readonly CancellationTokenSource _token = new CancellationTokenSource();

        public NtechDirectSeriLogSink()
            : base(50, TimeSpan.FromSeconds(5))
        {

        }

        private class NLogItem
        {
            public DateTimeOffset EventDate { get; set; }
            public string Level { get; set; }
            public Dictionary<string, string> Properties { get; set; }
            public string Message { get; set; }
            public string Exception { get; set; }
        }


        private static string FormatException(Exception ex)
        {
            var b = new StringBuilder();
            var guard = 0;
            while (ex != null && guard++ < 10)
            {
                b.AppendLine(ex.GetType().Name);
                b.AppendLine(ex.Message);
                b.AppendLine(ex.StackTrace);
                ex = ex.InnerException;
            }
            return b.ToString();
        }

        private static Controllers.SystemLogItemModel ConvertToNLogItem(LogEvent e)
        {
            var item = new Controllers.SystemLogItemModel
            {
                Level = e.Level.ToString(),
                EventDate = e.Timestamp,
                Exception = FormatException(e.Exception),
                Message = e.RenderMessage(),
                Properties = e.Properties.ToDictionary(x => x.Key, x => x.Value.ToString())
            };
            return item;
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            if (!Global.IsInitialized)
                return;

            try
            {
                using (var context = new AuditContext())
                {
                    if (events != null)
                    {
                        var logItems = events.Select(ConvertToNLogItem).Select(Controllers.SystemLogController.ToSystemLogItem);
                        context.SystemLogItems.AddRange(logItems);
                    }
                    await context.SaveChangesAsync(_token.Token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                //NOTE: Not much we can do if the logging fails
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _token.Cancel();

            base.Dispose(disposing);
        }
    }
}