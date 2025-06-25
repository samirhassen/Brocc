using Microsoft.Owin;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.Eventing;
using Owin;
using Serilog;
using Serilog.Core.Enrichers;
using System;

[assembly: OwinStartup(typeof(nTest.App_Start.Startup1))]

namespace nTest.App_Start
{
    public class Startup1
    {
        public void Configuration(IAppBuilder app)
        {
            if (NEnv.IsProduction)
                throw new Exception("nTest is not allowed to run in production!");

            var automationUser = new Lazy<NTechSelfRefreshingBearerToken>(() => NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, NEnv.AutomationUsernameAndPassword));
            Log.Logger = new LoggerConfiguration()
                                       .Enrich.WithMachineName()
                                       .Enrich.FromLogContext()
                                       .Enrich.With(
                                           new PropertyEnricher("ServiceName", "nTest"),
                                           new PropertyEnricher("ServiceVersion", System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString())
                                       )
                                       .WriteTo.Sink(new NTechSerilogSink(n => NEnv.ServiceRegistry.Internal[n], bearerToken: automationUser), NEnv.IsVerboseLoggingEnabled
                                           ? Serilog.Events.LogEventLevel.Debug
                                           : Serilog.Events.LogEventLevel.Information)
                                       .CreateLogger();

            NLog.Information("{EventType}: {environment} mode", "ServiceStarting", NEnv.IsProduction ? "prod" : "dev");

            app.Use<NTechLoggingMiddleware>("nTest");

            LoginSetupSupport.SetupLogin(app, "nTest", LoginSetupSupport.LoginMode.BothUsersAndApi, NEnv.IsProduction, NEnv.ServiceRegistry, NEnv.ClientCfg);

            //Start the background worker
            NTechEventHandler.CreateAndLoadSubscribers(
                typeof(nTest.Global).Assembly,
                new System.Collections.Generic.List<string>());

            TimeMachine.SharedInstance.Init();
            
            Global.Application_Start(null, null);
        }
    }
}
