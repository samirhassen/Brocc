using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace NTechSignicat
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var preConfig = AddLocalAppSettingsSources(
                System.AppContext.BaseDirectory,
                new ConfigurationBuilder()).Build();
            var machineSettingsFile = preConfig.GetValue<string>("ntech.machinesettingsfile");
            IConfigurationSource machineSettingsSource = null;
            if (!string.IsNullOrWhiteSpace(machineSettingsFile) && File.Exists(machineSettingsFile))
            {
                machineSettingsSource = StaticDictionaryConfigurationProvider.CreateFromAppsettingsInClassicAppConfig(machineSettingsFile);
            }

            var builder = CreateWebHostBuilder(args, machineSettingsSource).Build();

            if(IsTestCompatibilityCall(args))
            {
                Console.WriteLine("Ok");
            }
            else
            {
                builder.Run();
            }            
        }

        private static bool IsTestCompatibilityCall(string[] args) => args != null && args.Any(x => x == "--test-compatibility");

        private static IConfigurationBuilder AddLocalAppSettingsSources(string basePath, IConfigurationBuilder b)
        {
            b.SetBasePath(basePath);
            b.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            return b;
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args, IConfigurationSource machineSettingsFile) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((builderContext, config) =>
                {
                    var env = builderContext.HostingEnvironment;
                    if (machineSettingsFile != null)
                        config.Add(machineSettingsFile);

                    AddLocalAppSettingsSources(env.ContentRootPath, config);

                    config.AddEnvironmentVariables();
                })
                .ConfigureKestrel(x => x.AddServerHeader = false)
                .UseStartup<Startup>();
    }
}