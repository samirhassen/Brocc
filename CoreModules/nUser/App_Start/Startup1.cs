using System;
using System.IO;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.Owin;
using NTech;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using nUser.App_Start;
using nUser.Code;
using Owin;
using Serilog;
using Serilog.Core.Enrichers;
using Serilog.Events;

[assembly: OwinStartup(typeof(Startup1))]

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

            var automationUser = new Lazy<NTechSelfRefreshingBearerToken>(() =>
                NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry,
                    NEnv.AutomationUsernameAndPassword));
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
                    new PropertyEnricher("ServiceVersion",
                        Assembly.GetExecutingAssembly().GetName().Version?.ToString())
                )
                .WriteTo.Sink(new NTechSerilogSink(n => NEnv.ServiceRegistry.Internal[n], bearerToken: automationUser),
                    NEnv.IsVerboseLoggingEnabled
                        ? LogEventLevel.Information
                        : LogEventLevel.Warning)
                .CreateLogger();

            NLog.Information("{EventType}: in {environment} mode", "ServiceStarting",
                NEnv.IsProduction ? "prod" : "dev");

            if (NEnv.IsVerboseLoggingEnabled)
            {
                var logFolder = NEnv.LogFolder;
                if (logFolder != null)
                    app.Use<NTechVerboseRequestLogMiddleware>(
                        new DirectoryInfo(Path.Combine(logFolder.FullName, "RawRequests")),
                        "nUser");
            }

            app.Use<NTechLoggingMiddleware>("nUser");

            IdentityServerSetup.RegisterStartup(app);

            LoginSetupSupport.SetupLogin(app, "nUser", LoginSetupSupport.LoginMode.OnlyApi, NEnv.IsProduction,
                NEnv.ServiceRegistry, NEnv.ClientCfg);

            ClockFactory.Init();
        }
    }
}