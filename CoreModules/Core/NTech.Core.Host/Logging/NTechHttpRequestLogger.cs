using NTech.Core.Host.Infrastructure;
using NTech.Core.Module;

namespace NTech.Core.Host.Logging
{
    public class NTechHttpRequestLogger : ILogger
    {
        private readonly bool isEnabled;
        private RotatingLogFile rotatingLogFile;

        public NTechHttpRequestLogger(bool isEnabled, NEnv env)
        {
            this.isEnabled = isEnabled;
            rotatingLogFile = isEnabled ? new RotatingLogFile(Path.Combine(env.LogFolder.FullName, "NTechHostApiLogs"), "ntech-host-api-log-") : null;
        }

        public IDisposable BeginScope<TState>(TState state) => default!;

        public bool IsEnabled(LogLevel logLevel) => isEnabled;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!isEnabled) return;

            rotatingLogFile.Log(formatter(state, exception));
        }
    }
}
