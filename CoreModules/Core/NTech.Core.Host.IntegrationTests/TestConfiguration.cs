using Microsoft.Extensions.Configuration;

namespace NTech.Core.Host.IntegrationTests
{
    internal static class TestConfiguration
    {
        private static Lazy<IConfiguration> configuration = new Lazy<IConfiguration>(() =>
        {
            var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false);
            return builder.Build();
        });
        public static string Opt(string name) => GetSetting(name, false);
        public static string Req(string name) => GetSetting(name, true);
        private static string GetSetting(string name, bool isRequired)
        {
            var value = configuration.Value[name];
            if (isRequired && value == null)
                throw new Exception($"Missing required setting {name}");
            return value;
        }
    }
}
