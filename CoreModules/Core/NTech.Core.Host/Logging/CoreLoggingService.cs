using NTech.Core.Module.Shared.Services;

namespace NTech.Core.Host.Logging
{
    public class CoreLoggingService : ILoggingService
    {
        private readonly ILogger<CoreLoggingService> logger;

        public CoreLoggingService(ILogger<CoreLoggingService> logger)
        {
            this.logger = logger;
        }

        public void AppendExceptionData(Exception ex, Dictionary<string, string> properties)
        {
            if (ex != null && properties != null)
            {
                IDictionary<string, string> dictionary = ex.Data["ntech.logproperties.v1"] as IDictionary<string, string>;
                if (dictionary != null)
                {
                    ex.Data["ntech.logproperties.v1"] = MergeDicts(dictionary, properties);
                }
                else
                {
                    ex.Data["ntech.logproperties.v1"] = properties;
                }
            }
        }

        private static Dictionary<string, string> MergeDicts(IDictionary<string, string> d, IDictionary<string, string> d2)
        {
            Dictionary<string, string> tmp = new Dictionary<string, string>(d);
            d2.ToList().ForEach(delegate (KeyValuePair<string, string> x)
            {
                tmp.Add(x.Key, x.Value);
            });
            return tmp;
        }

        public void Error(string message) => logger.LogError(message);
        public void Error(string template, string value) => logger.LogError(template + ": " + value);
        public void Error(Exception ex, string message) => logger.LogError(ex, message);
        public void Information(string message) => logger.LogInformation(message);
        public void Warning(Exception ex, string message) => logger.LogWarning(ex, message);
        public void Warning(string message) => logger.LogWarning(message);
    }
}
