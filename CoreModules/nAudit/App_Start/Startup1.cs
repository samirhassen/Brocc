using System;
using System.IO;
using System.Reflection;
using Microsoft.Owin;
using nAudit.App_Start;
using NTech;
using NTech.Services.Infrastructure;
using Owin;
using Serilog;
using Serilog.Core.Enrichers;
using Serilog.Events;

[assembly: OwinStartup(typeof(Startup1))]

namespace nAudit.App_Start
{
    public class Startup1
    {
        public void Configuration(IAppBuilder app)
        {
            var automationUser = new Lazy<NTechSelfRefreshingBearerToken>(() =>
                NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry,
                    NEnv.AutomationUsernameAndPassword));
            var cfg = new LoggerConfiguration();
            if (NEnv.IsVerboseLoggingEnabled)
            {
                cfg.MinimumLevel.Debug();
            }

            Log.Logger = cfg
                .Enrich.WithMachineName()
                .Enrich.FromLogContext()
                .Enrich.With(
                    new PropertyEnricher("ServiceName", "nAudit"),
                    new PropertyEnricher("ServiceVersion",
                        Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString())
                )
                .WriteTo.Sink(new NTechSerilogSink(n => NEnv.ServiceRegistry.Internal[n], bearerToken: automationUser),
                    NEnv.IsVerboseLoggingEnabled
                        ? LogEventLevel.Debug
                        : LogEventLevel.Information)
                .CreateLogger();

            NLog.Information("{EventType}: in {environment} mode", "ServiceStarting",
                NEnv.IsProduction ? "prod" : "dev");

            if (NEnv.IsVerboseLoggingEnabled)
            {
                var logFolder = NEnv.LogFolder;
                if (logFolder != null)
                    app.Use<NTechVerboseRequestLogMiddleware>(
                        new DirectoryInfo(Path.Combine(logFolder.FullName, "RawRequests")),
                        "nAudit");
            }

            app.Use<NTechLoggingMiddleware>("nAudit");

            LoginSetupSupport.SetupLogin(app, "nAudit", LoginSetupSupport.LoginMode.BothUsersAndApi, NEnv.IsProduction,
                NEnv.ServiceRegistry, NEnv.ClientCfg);

            ClockFactory.Init();
        }
    }
}