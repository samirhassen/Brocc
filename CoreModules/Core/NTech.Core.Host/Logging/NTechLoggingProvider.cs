using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using NTech.Core.Host.Infrastructure;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;

namespace NTech.Core.Host.Logging
{
    public class NTechLoggerConfiguration
    {
        public bool IsVerboseLoggingEnabled { get; init; }
        public bool IsHttpRequestLoggingEnabled { get; init; }
    }

    public class NTechLoggerProvider : ILoggerProvider
    {
        private readonly NTechLoggerConfiguration config;
        private readonly ConcurrentDictionary<string, ILogger> loggers = new();
        private readonly NTechAuditSystemLogBatchingService auditService;
        private readonly NEnv nEnv;

        public NTechLoggerProvider(NTechAuditSystemLogBatchingService auditService, NEnv nEnv)
        {
            config = new NTechLoggerConfiguration
            {
                IsVerboseLoggingEnabled = nEnv.IsProduction,
                IsHttpRequestLoggingEnabled = nEnv.IsHttpRequestLoggingEnabled
            };
            this.nEnv = nEnv;
            this.auditService = auditService;
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (categoryName == "Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware")
            {
                return loggers.GetOrAdd(categoryName,
                    _ => new NTechHttpRequestLogger(config.IsHttpRequestLoggingEnabled, nEnv));
            }

            return loggers.GetOrAdd(categoryName, name => new NTechLogger(name, config, auditService));
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
        private readonly NTechAuditSystemLogBatchingService auditService;

        public NTechLogger(string name, NTechLoggerConfiguration config,
            NTechAuditSystemLogBatchingService auditService)
        {
            this.name = name;
            this.config = config;
            this.auditService = auditService;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel switch
            {
                >= LogLevel.Warning => true,
                >= LogLevel.Information => config.IsVerboseLoggingEnabled,
                _ => false
            };
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            string ntechLogLevel;
            switch (logLevel)
            {
                case LogLevel.Critical:
                    ntechLogLevel = "Error";
                    break;

                case LogLevel.Information:
                case LogLevel.Error:
                case LogLevel.Warning:
                    ntechLogLevel = logLevel.ToString();
                    break;

                default:
                    return;
            }

            var p = new Dictionary<string, string>
            {
                { "ServiceName", "NTechHost" }, //TODO: Can this be contextualized somehow?
                {
                    "ServiceVersion",
                    AssemblyName
                        .GetAssemblyName(Assembly.GetExecutingAssembly().Location).Version?.ToString()
                }
            };

            if (exception is NTechLoggerException loggerException)
            {
                string ClipLeft(string s, int maxLength) => s.Length > maxLength ? s.Substring(0, maxLength) : s;

                if (loggerException.RemoteIp != null)
                    p["RemoteIp"] = loggerException.RemoteIp;

                if (loggerException.RequestUri != null)
                    p["RequestUri"] = ClipLeft(loggerException.RequestUri, 128);

                if (loggerException.UserId != null)
                    p["UserId"] = loggerException.UserId;

                if (loggerException.EventType != null)
                    p["EventType"] = ClipLeft(loggerException.EventType, 128);

                exception = loggerException.InnerException;
            }

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

        public static NTechLoggerException WrapException(Exception ex, string remoteIp = null, string requestUri = null,
            string userId = null, string eventType = null)
        {
            var result = new NTechLoggerException(ex.Message, ex)
            {
                RemoteIp = NormalizeWhitespace(remoteIp),
                RequestUri = NormalizeWhitespace(requestUri),
                UserId = NormalizeWhitespace(userId),
                EventType = NormalizeWhitespace(eventType)
            };

            return result;

            string NormalizeWhitespace(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }
    }

    public class
        NTechAuditSystemLogBatchingService : HostingAwareBackgroundBatchedServiceRequestQueueService<
        AuditClientSystemLogItem>
    {
        private readonly NEnv nEnv;
        private readonly ILogger<NTechAuditSystemLogBatchingService> logger;
        private readonly SystemLogService systemLogService;

        public NTechAuditSystemLogBatchingService(NEnv nEnv, ILogger<NTechAuditSystemLogBatchingService> logger,
            SystemLogService systemLogService)
        {
            this.nEnv = nEnv;
            this.logger = logger;
            this.systemLogService = systemLogService;
        }

        protected override string Name => "NTechLogginProviderAuditSink";

        protected override async Task HandleBatch(List<AuditClientSystemLogItem> items,
            CancellationToken cancellationToken)
        {
            await systemLogService.LogBatchAsync(items);
        }
    }

    /// <summary>
    /// NOTE:
    /// Never actually thrown but rather used with NTechLogger.WrapException to help pass state to the logger.
    /// So an exception could be logged like this
    /// logger.LogError(NTechLogger.WrapException(ex, remoteIp = "::1"));
    /// Which will cause the ip to be included in the logs
    /// </summary>
    public class NTechLoggerException : Exception
    {
        public NTechLoggerException()
        {
        }

        public NTechLoggerException(string message) : base(message)
        {
        }

        public NTechLoggerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NTechLoggerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public string RemoteIp { get; set; }
        public string RequestUri { get; set; }
        public string UserId { get; set; }
        public string EventType { get; set; }
    }
}