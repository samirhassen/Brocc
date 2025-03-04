using Microsoft.Owin;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using Owin;
using Serilog;
using Serilog.Core.Enrichers;
using System;
using System.Web.Mvc;
using System.Web.Routing;

[assembly: OwinStartup(typeof(nUser.App_Start.Startup1))]

namespace nUser.App_Start
{
    public class Startup1
    {
        public void Configuration(IAppBuilder app)
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            NTechHardenedMvcModelBinder.Register(NEnv.CurrentServiceName);
            GlobalFilters.Filters.Add(new NTechHandleErrorAttribute());
            GlobalFilters.Filters.Add(new NTechAuthorizeAndPermissionsAttribute());
            GlobalFilters.Filters.Add(new ConvertJsonToCamelCaseActionFilterAttribute());
            GlobalContentSecurityPolicyFilters.RegisterGlobalFilters(GlobalFilters.Filters);

            var automationUser = new Lazy<NTechSelfRefreshingBearerToken>(() => NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, NEnv.AutomationUsernameAndPassword));
            var cfg = new LoggerConfiguration();
            if (NEnv.IsVerboseLoggingEnabled)
            {
                cfg.MinimumLevel.Debug();
            }
            Log.Logger = cfg
                .Enrich.WithMachineName()
                .Enrich.FromLogContext()
                .Enrich.With(
                    new PropertyEnricher("ServiceName", "nUser"),
                    new PropertyEnricher("ServiceVersion", System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString())
                )
                .WriteTo.Sink(new NTechSerilogSink(n => NEnv.ServiceRegistry.Internal[n], bearerToken: automationUser), NEnv.IsVerboseLoggingEnabled
                    ? Serilog.Events.LogEventLevel.Information
                    : Serilog.Events.LogEventLevel.Warning)
                .CreateLogger();

            NLog.Information("{EventType}: in {environment} mode", "ServiceStarting", NEnv.IsProduction ? "prod" : "dev");

            if (NEnv.IsVerboseLoggingEnabled)
            {
                var logFolder = NEnv.LogFolder;
                if (logFolder != null)
                    app.Use<NTechVerboseRequestLogMiddleware>(new System.IO.DirectoryInfo(System.IO.Path.Combine(logFolder.FullName, "RawRequests")), "nUser");
            }

            app.Use<NTechLoggingMiddleware>("nUser");

            Code.IdentityServerSetup.RegisterStartup(app);

            LoginSetupSupport.SetupLogin(app, "nUser", LoginSetupSupport.LoginMode.OnlyApi, NEnv.IsProduction, NEnv.ServiceRegistry, NEnv.ClientCfg);

            NTech.ClockFactory.Init();
        }
    }
}
