using IdentityServer.WindowsAuthentication.Configuration;
using Microsoft.Owin;
using NTech.Services.Infrastructure;
using Owin;
using Serilog;
using Serilog.Core.Enrichers;
using System;

[assembly: OwinStartup(typeof(nWindowsAuthIdentityServer.App_Start.Startup1))]

namespace nWindowsAuthIdentityServer.App_Start
{
    public class Startup1
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
                    new PropertyEnricher("ServiceName", "nWindowsAuthIdentityServer"),
                    new PropertyEnricher("ServiceVersion",
                        System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString())
                )
                .WriteTo.Sink(new NTechSerilogSink(n => NEnv.ServiceRegistry.Internal[n], bearerToken: automationUser),
                    NEnv.IsVerboseLoggingEnabled
                        ? Serilog.Events.LogEventLevel.Information
                        : Serilog.Events.LogEventLevel.Warning)
                .CreateLogger();

            NLog.Information("{EventType}: in {environment} mode", "ServiceStarting",
                NEnv.IsProduction ? "prod" : "dev");

            app.Use<NTechLoggingMiddleware>("nWindowsAuthIdentityServer");

            //See this: https://github.com/IdentityServer/IdentityServer3.Samples/tree/master/source/WebHost%20(Windows%20Auth)
            app.UseWindowsAuthenticationService(new WindowsAuthenticationOptions
            {
                IdpRealm = "urn:idsrv3",
                IdpReplyUrl = NEnv.ServiceRegistry.External.ServiceUrl("nUser", "id/was").ToString(),
                SigningCertificate = NEnv.IdentityServerCertificate,
                EnableOAuth2Endpoint = false
            });

            NTech.ClockFactory.Init();
        }
    }
}