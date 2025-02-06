using Microsoft.Owin;
using nPreCredit.Code.AffiliateReporting;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.Eventing;
using Owin;
using Serilog;
using Serilog.Core.Enrichers;
using System;

[assembly: OwinStartup(typeof(nPreCredit.App_Start.Startup1))]

namespace nPreCredit.App_Start
{
    public class Startup1
    {
        public void Configuration(IAppBuilder app)
        {
            var cfg = new LoggerConfiguration();

            if (NEnv.IsVerboseLoggingEnabled)
            {
                cfg.MinimumLevel.Debug();
            }

            var automationUser = new Lazy<NTechSelfRefreshingBearerToken>(() => NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, NEnv.ApplicationAutomationUsernameAndPassword));
            Log.Logger = cfg
                .Enrich.WithMachineName()
                .Enrich.FromLogContext()
                .Enrich.With(
                    new PropertyEnricher("ServiceName", "nPreCredit"),
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
                    app.Use<NTechVerboseRequestLogMiddleware>(new System.IO.DirectoryInfo(System.IO.Path.Combine(logFolder.FullName, "RawRequests")), "nPreCredit");
            }

            DependancyInjectionConfig.Configure();

            LoginSetupSupport.SetupLogin(app, "nPreCredit", LoginSetupSupport.LoginMode.BothUsersAndApi, NEnv.IsProduction, NEnv.ServiceRegistry, NEnv.ClientCfg);

            //Start the background worker
            NTechEventHandler.CreateAndLoadSubscribers(
                typeof(nPreCredit.Global).Assembly,
                NEnv.EnabledPluginNames,
                additionalPluginFolders: NEnv.PluginSourceFolders,
                assemblyLoader: DependancyInjection.Services.Resolve<NTechExternalAssemblyLoader>());

            app.Use<NTechLoggingMiddleware>("nPreCredit");

            var affiliateReporting = new AffiliateReportingBackgroundTimer();
            affiliateReporting.Start(TimeSpan.FromSeconds(10));

            NTech.Services.Infrastructure.NTechWs.NTechWebserviceRequestValidator
                .InitializeValidationFramework(
                    NEnv.BaseCivicRegNumberParser.IsValid,
                    x => new NTech.Banking.BankAccounts.BankAccountNumberParser(NEnv.ClientCfg.Country.BaseCountry).TryParseFromStringWithDefaults(x, null, out _),
                    NEnv.BaseOrganisationNumberParser.IsValid);

            NTech.ClockFactory.Init();
        }
    }
}