using System;
using System.Linq;
using System.Reflection;
using Microsoft.Owin;
using nScheduler.App_Start;
using NTech;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.Eventing;
using Owin;
using Serilog;
using Serilog.Core.Enrichers;
using Serilog.Events;

[assembly: OwinStartup(typeof(Startup1))]

namespace nScheduler.App_Start;

public class Startup1
{
    public void Configuration(IAppBuilder app)
    {
        var automationUser = new Lazy<NTechSelfRefreshingBearerToken>(() =>
            NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(
                NEnv.ServiceRegistryNormal,
                Tuple.Create(NEnv.AutomationUser.Username, NEnv.AutomationUser.Password)));
        Log.Logger = new LoggerConfiguration()
            .Enrich.WithMachineName()
            .Enrich.FromLogContext()
            .Enrich.With(
                new PropertyEnricher("ServiceName", "nScheduler"),
                new PropertyEnricher("ServiceVersion",
                    Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString())
            )
            .WriteTo.Sink(
                new NTechSerilogSink(n => NEnv.ServiceRegistryNormal.Internal.ServiceRootUri(n).ToString(),
                    bearerToken: automationUser), NEnv.IsVerboseLoggingEnabled
                    ? LogEventLevel.Debug
                    : LogEventLevel.Information)
            .CreateLogger();

        NLog.Information("{EventType}: in {environment} mode", "ServiceStarting",
            NEnv.IsProduction ? "prod" : "dev");

        app.Use<NTechLoggingMiddleware>("nScheduler");

        LoginSetupSupport.SetupLogin(app, "nScheduler", LoginSetupSupport.LoginMode.BothUsersAndApi,
            NEnv.IsProduction, NEnv.ServiceRegistryNormal, NEnv.ClientCfg);

        NTechEventHandler.CreateAndLoadSubscribers(
            typeof(Global).Assembly,
            Enumerable.Empty<string>().ToList());

        ClockFactory.Init();
    }
}