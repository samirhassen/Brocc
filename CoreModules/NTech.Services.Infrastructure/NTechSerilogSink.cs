using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace NTech.Services.Infrastructure
{
    public class NTechSerilogSink : PeriodicBatchingSink
    {
        private readonly CancellationTokenSource token = new CancellationTokenSource();
        private readonly Func<string, string> getServiceAddress;
        private readonly Lazy<NTechSelfRefreshingBearerToken> bearerToken;

        public NTechSerilogSink(Func<string, string> getServiceAddress,
            Lazy<NTechSelfRefreshingBearerToken> bearerToken = null)
            : base(50, TimeSpan.FromSeconds(5))
        {
            this.getServiceAddress = getServiceAddress;
            this.bearerToken = bearerToken;
        }

        private const string ExceptionDataName = "ntech.logproperties.v1";

        public static void AppendExceptionData(Exception ex, IDictionary<string, string> properties)
        {
            if (ex == null || properties == null) return;
            if (ex.Data[ExceptionDataName] is IDictionary<string, string> d)
            {
                ex.Data[ExceptionDataName] = MergeDicts(d, properties);
            }
            else
            {
                ex.Data[ExceptionDataName] = properties;
            }
        }

        private static Dictionary<string, string> MergeDicts(IDictionary<string, string> d,
            IDictionary<string, string> d2)
        {
            var tmp = new Dictionary<string, string>(d);
            d2.ToList().ForEach(x => tmp.Add(x.Key, x.Value));
            return tmp;
        }

        private static Dictionary<string, string> MergeDataProperties(Dictionary<string, string> properties,
            Exception ex)
        {
            if (ex?.Data == null || !ex.Data.Contains(ExceptionDataName)) return properties;
            if (ex.Data[ExceptionDataName] is IDictionary<string, string> d)
                return MergeDicts(properties, d);

            return properties;
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

        private static NLogItem ConvertToNLogItem(LogEvent e)
        {
            var properties = e.Properties.ToDictionary(x => x.Key, x => x.Value.ToString());

            properties = MergeDataProperties(properties, e.Exception);

            return new NLogItem
            {
                Level = e.Level.ToString(),
                EventDate = e.Timestamp,
                Exception = FormatException(e.Exception),
                Message = e.RenderMessage(),
                Properties = properties
            };
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            try
            {
                var items = events.Select(ConvertToNLogItem).ToList();

                using (var client = new HttpClient())
                {
                    var url = getServiceAddress("NTechHost");
                    if (string.IsNullOrWhiteSpace(url))
                        throw new Exception("Missing url for NTechHost");
                    client.BaseAddress = new Uri(url);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    string endpoint;
                    if (bearerToken != null)
                    {
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", bearerToken.Value.GetToken());
                        endpoint = "Api/SystemLog/Create-Batch";
                    }
                    else
                    {
                        //TODO: Make bearerToken mandatory and get rid of this branch and delete the api in NTechHost
                        endpoint = "Api/SystemLog/Create-Batch-Legacy";
                    }

                    await client.PostAsJsonAsync(endpoint, new { items }, token.Token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                //NOTE: Not much we can do if the logging fails
                Debug.WriteLine(ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            token.Cancel();

            base.Dispose(disposing);
        }
    }
}