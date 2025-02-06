using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using Serilog;
using Serilog.Core.Enrichers;
using nGccCustomerApplication.Code;
using NTech.Services.Infrastructure;

[assembly: OwinStartup(typeof(nGccCustomerApplication.App_Start.Startup1))]

namespace nGccCustomerApplication.App_Start
{
    public class Startup1
    {
        public void Configuration(IAppBuilder app)
        {
            var automationUser = new Lazy<NTechSelfRefreshingBearerToken>(() => NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, NEnv.SystemUserCredentials));
            Log.Logger = new LoggerConfiguration()
                            .Enrich.WithMachineName()
                            .Enrich.FromLogContext()
                            .Enrich.With(
                                new PropertyEnricher("ServiceName", "nGccCustomerApplication"),
                                new PropertyEnricher("ServiceVersion", System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString())
                            )
                            .WriteTo.Sink(new NTechSerilogSink(n => NEnv.ServiceRegistry.Internal[n], bearerToken: automationUser), NEnv.IsVerboseLoggingEnabled
                                ? Serilog.Events.LogEventLevel.Debug
                                : Serilog.Events.LogEventLevel.Information)
                            .CreateLogger();

            NLog.Information("{EventType}: {environment} mode", "ServiceStarting", NEnv.IsProduction ? "prod" : "dev");

            app.Use<NTechLoggingMiddleware>("nGccCustomerApplication");
        }
    }
}
