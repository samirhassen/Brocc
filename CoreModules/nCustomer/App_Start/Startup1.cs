using Microsoft.Owin;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.Eventing;
using Owin;
using Serilog;
using Serilog.Core.Enrichers;
using System;
using System.Collections.Generic;

[assembly: OwinStartup(typeof(nCustomer.App_Start.Startup1))]

namespace nCustomer.App_Start
{
    public class Startup1
    {
        public void Configuration(IAppBuilder app)
        {
            var automationUser = new Lazy<NTechSelfRefreshingBearerToken>(() => NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, NEnv.ApplicationAutomationUsernameAndPassword));
            Log.Logger = new LoggerConfiguration()
                           .Enrich.WithMachineName()
                           .Enrich.FromLogContext()
                           .Enrich.With(
                               new PropertyEnricher("ServiceName", "nCustomer"),
                               new PropertyEnricher("ServiceVersion", System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString())
                           )
                           .WriteTo.Sink(new NTechSerilogSink(n => NEnv.ServiceRegistry.Internal[n], bearerToken: automationUser), NEnv.IsVerboseLoggingEnabled
                               ? Serilog.Events.LogEventLevel.Debug
                               : Serilog.Events.LogEventLevel.Information)
                           .CreateLogger();

            NLog.Information("{EventType}: {environment} mode", "ServiceStarting", NEnv.IsProduction ? "prod" : "dev");

            if (NEnv.IsVerboseLoggingEnabled)
            {
                var logFolder = NEnv.LogFolder;
                if (logFolder != null)
                    app.Use<NTechVerboseRequestLogMiddleware>(new System.IO.DirectoryInfo(System.IO.Path.Combine(logFolder.FullName, "RawRequests")), "nCustomer");
            }

            app.Use<NTechLoggingMiddleware>("nCustomer");

            //Start the background worker
            NTechEventHandler.CreateAndLoadSubscribers(
                typeof(nCustomer.MvcApplication).Assembly,
                new List<string>());

            LoginSetupSupport.SetupLogin(app, "nCustomer", LoginSetupSupport.LoginMode.BothUsersAndApi, NEnv.IsProduction, NEnv.ServiceRegistry, NEnv.ClientCfg);

            NTech.ClockFactory.Init();
        }
    }
}
