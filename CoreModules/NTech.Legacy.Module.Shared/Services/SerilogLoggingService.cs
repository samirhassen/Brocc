using NTech.Core.Module.Shared.Services;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;

namespace NTech.Legacy.Module.Shared.Services
{
    public class SerilogLoggingService : ILoggingService
    {
        public static SerilogLoggingService SharedInstance { get; } = new SerilogLoggingService();

        public void Error(string message) => Log.Error(message);
        public void Error(Exception ex, string message) => Log.Error(ex, message);
        public void Error(string template, string value) => Log.Error(template, value);
        public void Information(string message) => Log.Information(message);
        public void Warning(Exception ex, string message) => Log.Warning(ex, message);
        public void Warning(string message) => Log.Warning(message);
        public void AppendExceptionData(Exception ex, Dictionary<string, string> properties) => NTechSerilogSink.AppendExceptionData(ex, properties);
    }
}
