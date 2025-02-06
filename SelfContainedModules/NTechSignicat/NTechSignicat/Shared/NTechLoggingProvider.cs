using Microsoft.Extensions.Logging;
using NTech.Services.Infrastructure;
using NTechSignicat.Clients;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Threading;

namespace NTechSignicat.Shared
{
    public class NTechLoggerConfiguration
    {
        public bool IsVerboseLoggingEnabled { get; set; }
    }

    public class NTechLoggerProvider : ILoggerProvider
    {
        private readonly NTechLoggerConfiguration config;
        private readonly ConcurrentDictionary<string, NTechLogger> loggers = new ConcurrentDictionary<string, NTechLogger>();
        private readonly NTechAuditSystemLogBatchingService auditService;
        private readonly IServiceProvider serviceProvider;
        private readonly INEnv nEnv;

        public NTechLoggerProvider(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.nEnv = serviceProvider.GetService<INEnv>();
            this.auditService = serviceProvider.GetService<NTechAuditSystemLogBatchingService>();
            this.config = new NTechLoggerConfiguration
            {
                IsVerboseLoggingEnabled = this.nEnv.IsProduction
            };
        }

        public ILogger CreateLogger(string categoryName)
        {
            return loggers.GetOrAdd(categoryName, name => new NTechLogger(name, config, auditService, nEnv));
        }

        public void Dispose()
        {
            loggers.Clear();
        }
    }

    public class NTechLogger : ILogger
    {
        private readonly string name;
        private readonly NTechLoggerConfiguration config;
        private NTechAuditSystemLogBatchingService auditService;
        private readonly INEnv nEnv;

        public NTechLogger(string name, NTechLoggerConfiguration config, NTechAuditSystemLogBatchingService auditService, INEnv nEnv)
        {
            this.name = name;
            this.config = config;
            this.auditService = auditService;
            this.nEnv = nEnv;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            if (logLevel >= LogLevel.Warning)
                return true;
            else if (logLevel >= LogLevel.Information)
                return config.IsVerboseLoggingEnabled;
            else
                return false;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            string ntechLogLevel;
            switch (logLevel)
            {
                case LogLevel.Critical:
                    {
                        ntechLogLevel = "Error";
                    };
                    break;

                case LogLevel.Information:
                case LogLevel.Error:
                case LogLevel.Warning:
                    {
                        ntechLogLevel = logLevel.ToString();
                    };
                    break;

                default:
                    return;
            }

            /*
             *
                RemoteIp = prop(x.Properties, "RemoteIp"),
                RequestUri = clipLeft(StripRequestUri(prop(x.Properties, "RequestUri")), 128),
                UserId = prop(x.Properties, "UserId"),
                EventType = clipLeft(prop(x.Properties, "EventType"), 128),
             */
            var p = new Dictionary<string, string>()
            {
                { "ServiceName", nEnv.ServiceName },
                { "ServiceVersion", System.Reflection.AssemblyName.GetAssemblyName(System.Reflection.Assembly.GetExecutingAssembly().Location).Version.ToString() }
            };
            auditService.AddItem(new AuditClientSystemLogItem
            {
                EventDate = DateTimeOffset.Now,
                Exception = FormatException(exception),
                Level = ntechLogLevel,
                Message = formatter(state, exception),
                Properties = p
            });
        }

        private static string FormatException(Exception ex)
        {
            if (ex == null)
                return null;

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
    }

    public class NTechAuditSystemLogBatchingService : HostingAwareBackgroundBatchedServiceRequestQueueService<AuditClientSystemLogItem>
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly NTechServiceRegistry nTechServiceRegistry;
        private readonly IServiceProvider serviceProvider;
        private readonly INEnv nEnv;
        private readonly ILogger<NTechAuditSystemLogBatchingService> logger;

        public NTechAuditSystemLogBatchingService(IHttpClientFactory httpClientFactory, NTechServiceRegistry nTechServiceRegistry, IServiceProvider serviceProvider, INEnv nEnv, ILogger<NTechAuditSystemLogBatchingService> logger)
        {
            this.httpClientFactory = httpClientFactory;
            this.nTechServiceRegistry = nTechServiceRegistry;
            this.serviceProvider = serviceProvider;
            this.nEnv = nEnv;
            this.logger = logger;
        }

        protected override string Name => "NTechLogginProviderAuditSink";

        protected override void HandleBatch(List<AuditClientSystemLogItem> items, CancellationToken cancellationToken)
        {
            if (nEnv.ForceLocalLogging || !nTechServiceRegistry.ContainsService("NTechHost"))
            {
                foreach (var i in items)
                {
                    var msg = $"[Audit] {i.EventDate}: {i.Message}";
                    if (i.Exception != null)
                        msg += Environment.NewLine + $"Exception: {i.Exception}";
                    if (i.Properties != null)
                    {
                        foreach (var p in i.Properties)
                            msg += Environment.NewLine + $"{p.Key}={p.Value}";
                    }

                    if (i.Level == "Error")
                        logger.LogError(msg);
                    else if (i.Level == "Warning")
                        logger.LogWarning(msg);
                    else
                        logger.LogInformation(msg);
                }
            }
            else
            {
                var c = new AuditClient(httpClientFactory, nTechServiceRegistry, serviceProvider, nEnv);
                c.CreateSystemLogBatch(items).ConfigureAwait(false).GetAwaiter().GetResult(); //TODO: Await
            }
        }
    }

    /*

    public class NTechSerilogSink : PeriodicBatchingSink
    {
        private readonly CancellationTokenSource token = new CancellationTokenSource();
        private readonly Func<string, string> getServiceAddress;

        public NTechSerilogSink(Func<string, string> getServiceAddress)
            : base(50, TimeSpan.FromSeconds(5))
        {
            this.getServiceAddress = getServiceAddress;
        }

        private const string ExceptionDataName = "ntech.logproperties.v1";

        public static void AppendExceptionData(Exception ex, IDictionary<string, string> properties)
        {
            if (ex != null && properties != null)
            {
                var d = ex.Data[ExceptionDataName] as IDictionary<string, string>;
                if (d != null)
                {
                    ex.Data[ExceptionDataName] = MergeDicts(d, properties);
                }
                else
                {
                    ex.Data[ExceptionDataName] = properties;
                }
            }
        }

        private static Dictionary<string, string> MergeDicts(IDictionary<string, string> d, IDictionary<string, string> d2)
        {
            var tmp = new Dictionary<string, string>(d);
            d2.ToList().ForEach(x => tmp.Add(x.Key, x.Value));
            return tmp;
        }

        private static Dictionary<string, string> MergeDataProperties(Dictionary<string, string> properties, Exception ex)
        {
            if (ex?.Data != null && ex.Data.Contains(ExceptionDataName))
            {
                var d = ex.Data[ExceptionDataName] as IDictionary<string, string>;
                if (d != null)
                    return MergeDicts(properties, d);
            }
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

            properties = MergeDataProperties(properties, e?.Exception);

            var item = new NLogItem
            {
                Level = e.Level.ToString(),
                EventDate = e.Timestamp,
                Exception = FormatException(e.Exception),
                Message = e.RenderMessage(),
                Properties = properties
            };
            return item;
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            try
            {
                var items = events.Select(ConvertToNLogItem).ToList();

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(getServiceAddress("nAudit"));
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    await client.PostAsJsonAsync("SystemLog/CreateBatch", new { items = items }, token.Token).ConfigureAwait(false);
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
            token.Cancel();

            base.Dispose(disposing);
        }
    }
    */
}