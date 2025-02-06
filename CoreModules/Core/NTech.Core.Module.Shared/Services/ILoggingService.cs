using System;
using System.Collections.Generic;

namespace NTech.Core.Module.Shared.Services
{
    public interface ILoggingService
    {
        void Warning(Exception ex, string message);
        void Warning(string message);
        void Error(string message);
        void Error(string template, string value);
        void Error(Exception ex, string message);
        void Information(string message);
        void AppendExceptionData(Exception ex, Dictionary<string, string> properties);
    }
}
