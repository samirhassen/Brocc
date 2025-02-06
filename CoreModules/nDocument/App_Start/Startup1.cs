using Microsoft.Owin;
using NTech.Services.Infrastructure;
using Owin;
using Serilog;
using Serilog.Core.Enrichers;
using System;

[assembly: OwinStartup(typeof(nDocument.App_Start.Startup1))]

namespace nDocument.App_Start
{
    public class Startup1
    {
        public void Configuration(IAppBuilder app)
        {
            Log.Logger = new LoggerConfiguration()
                                       .Enrich.WithMachineName()
                                       .Enrich.FromLogContext()
                                       .Enrich.With(
                                           new PropertyEnricher("ServiceName", "nDocument"),
                                           new PropertyEnricher("ServiceVersion", System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString())
                                       )
                                       .WriteTo.Sink(new NTechSerilogSink(n => NEnv.ServiceRegistry.Internal[n], bearerToken: NEnv.AutomationUserBearerToken), NEnv.IsVerboseLoggingEnabled
                                           ? Serilog.Events.LogEventLevel.Debug
                                           : Serilog.Events.LogEventLevel.Information)
                                       .CreateLogger();

            NLog.Information("{EventType}: {environment} mode", "ServiceStarting", NEnv.IsProduction ? "prod" : "dev");

            app.Use<NTechLoggingMiddleware>("nDocument");

            LoginSetupSupport.SetupLogin(app, "nDocument", LoginSetupSupport.LoginMode.BothUsersAndApi, NEnv.IsProduction, NEnv.ServiceRegistry, NEnv.ClientCfg);

            NTech.ClockFactory.Init();
        }
    }
}
