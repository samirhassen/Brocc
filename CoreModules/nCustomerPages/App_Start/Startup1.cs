using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using NTech.Services.Infrastructure;
using Owin;
using Serilog;
using Serilog.Core.Enrichers;
using System;

[assembly: OwinStartup(typeof(nCustomerPages.App_Start.Startup1))]

namespace nCustomerPages.App_Start
{
    public class Startup1
    {
        public void Configuration(IAppBuilder app)
        {
            var automationUser = new Lazy<NTechSelfRefreshingBearerToken>(() => NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, NEnv.SystemUserUserNameAndPassword));
            Log.Logger = new LoggerConfiguration()
                            .Enrich.WithMachineName()
                            .Enrich.FromLogContext()
                            .Enrich.With(
                                new PropertyEnricher("ServiceName", "nCustomerPages"),
                                new PropertyEnricher("ServiceVersion", System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString())
                            )
                            .WriteTo.Sink(new NTechSerilogSink(n => NEnv.ServiceRegistry.Internal[n], bearerToken: automationUser), NEnv.IsVerboseLoggingEnabled
                                ? Serilog.Events.LogEventLevel.Debug
                                : Serilog.Events.LogEventLevel.Information)
                            .CreateLogger();

            NLog.Information("{EventType}: {environment} mode", "ServiceStarting", NEnv.IsProduction ? "prod" : "dev");

            app.Use<NTechLoggingMiddleware>("nCustomerPages");

            if (NEnv.IsCreditTokenAuthenticationModeEnabled)
            {
                app.UseCookieAuthentication(new CookieAuthenticationOptions
                {
                    AuthenticationType = Controllers.CreditTokenAuthenticationController.AuthType,
                    CookieName = string.Format(".AuthCookie.{0}.{1}", Controllers.CreditTokenAuthenticationController.AuthType, NEnv.IsProduction ? "P" : "T"),
                    ExpireTimeSpan = TimeSpan.FromHours(1),
                    SlidingExpiration = true,
                    LoginPath = new PathString("/access-denied")
                });
            }
            if (NEnv.IsDirectEidAuthenticationModeEnabled && Controllers.CommonElectronicIdLoginProvider.IsProviderEnabled)
            {
                app.UseCookieAuthentication(new CookieAuthenticationOptions
                {
                    AuthenticationType = Controllers.CommonElectronicIdLoginProvider.AuthTypeNameShared,
                    CookieName = string.Format(".AuthCookie.{0}.{1}", Controllers.CommonElectronicIdLoginProvider.AuthTypeNameShared, NEnv.IsProduction ? "P" : "T"),
                    ExpireTimeSpan = TimeSpan.FromHours(1),
                    SlidingExpiration = true,
                    LoginPath = new PathString("/access-denied")
                });
            }

            NTech.ClockFactory.Init();
        }
    }
}