using System;
using System.Reflection;
using Microsoft.Owin;
using nBackOffice.App_Start;
using NTech;
using NTech.Services.Infrastructure;
using Owin;
using Serilog;
using Serilog.Core.Enrichers;
using Serilog.Events;

[assembly: OwinStartup(typeof(Startup2))]

namespace nBackOffice.App_Start
{
    public class Startup2
    {
        public void Configuration(IAppBuilder app)
        {
            var automationUser = new Lazy<NTechSelfRefreshingBearerToken>(() =>
                NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry,
                    NEnv.AutomationUsernameAndPassword));
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithMachineName()
                .Enrich.FromLogContext()
                .Enrich.With(
                    new PropertyEnricher("ServiceName", "nBackOffice"),
                    new PropertyEnricher("ServiceVersion",
                        Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString())
                )
                .WriteTo.Sink(new NTechSerilogSink(n => NEnv.ServiceRegistry.Internal[n], bearerToken: automationUser),
                    NEnv.IsVerboseLoggingEnabled
                        ? LogEventLevel.Debug
                        : LogEventLevel.Information)
                .CreateLogger();

            NLog.Information("{EventType}: {environment} mode", "ServiceStarting", NEnv.IsProduction ? "prod" : "dev");

            app.Use<NTechLoggingMiddleware>("nBackOffice");

            LoginSetupSupport.SetupLogin(app, "nBackOffice", LoginSetupSupport.LoginMode.OnlyUsers, NEnv.IsProduction,
                NEnv.ServiceRegistry, NEnv.ClientCfg);

            ClockFactory.Init();
            
            Global.Application_Start(null, null);
        }
    }
}