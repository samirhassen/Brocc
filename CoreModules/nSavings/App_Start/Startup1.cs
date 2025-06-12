using System;
using System.IO;
using System.Reflection;
using Microsoft.Owin;
using nSavings;
using nSavings.Code;
using NTech;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.Eventing;
using Owin;
using Serilog;
using Serilog.Core.Enrichers;
using Serilog.Events;

[assembly: OwinStartup(typeof(Startup1))]

namespace nSavings
{
    public class Startup1
    {
        public void Configuration(IAppBuilder app)
        {
            var automationUser = new Lazy<NTechSelfRefreshingBearerToken>(() =>
                NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry,
                    NEnv.ApplicationAutomationUsernameAndPassword));
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithMachineName()
                .Enrich.FromLogContext()
                .Enrich.With(
                    new PropertyEnricher("ServiceName", "nSavings"),
                    new PropertyEnricher("ServiceVersion",
                        Assembly.GetExecutingAssembly().GetName().Version?.ToString())
                )
                //.WriteTo.Console()
                .WriteTo.Sink(new NTechSerilogSink(n => NEnv.ServiceRegistry.Internal[n], bearerToken: automationUser),
                    NEnv.IsVerboseLoggingEnabled
                        ? LogEventLevel.Debug
                        : LogEventLevel.Information)
                .CreateLogger();

            NLog.Information("{EventType}: {environment} mode", "ServiceStarting", NEnv.IsProduction ? "prod" : "dev");

            if (NEnv.IsVerboseLoggingEnabled)
            {
                var logFolder = NEnv.LogFolder;
                if (logFolder != null)
                    app.Use<NTechVerboseRequestLogMiddleware>(
                        new DirectoryInfo(Path.Combine(logFolder.FullName, "RawRequests")),
                        "nSavings");
            }

            app.Use<NTechLoggingMiddleware>("nSavings");

            LoginSetupSupport.SetupLogin(app, "nSavings", LoginSetupSupport.LoginMode.BothUsersAndApi,
                NEnv.IsProduction, NEnv.ServiceRegistry, NEnv.ClientCfg);

            //Start the background worker
            NTechEventHandler.CreateAndLoadSubscribers(
                typeof(Global).Assembly, []);

            ClockFactory.Init();

            Global.Application_Start(null, null);
        }
    }
}