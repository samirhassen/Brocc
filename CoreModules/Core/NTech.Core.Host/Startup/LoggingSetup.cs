using Microsoft.AspNetCore.HttpLogging;
using NTech.Core.Host.Infrastructure;
using NTech.Core.Host.Logging;
using NTech.Core.Module;
using NTech.Core.Module.Infrastrucutre;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Services;
using NTech.Services.Infrastructure;

namespace NTech.Core.Host.Startup
{
    internal static class LoggingSetup
    {
        public static void AddServices(IServiceCollection services)
        {
            services.AddSingleton<SystemLogService>(x =>
            {
                var env = x.GetRequiredService<NEnv>();
                //TODO: Make sure this setting is in place everywhere we want to keep using nAudit and then swap this to default to nCustomer instead
                var connectionStringName = env.OptionalSetting("ntech.core.systemlog.connectionstringname") ?? "AuditContext";

                var connectionString = env.GetConnectionString(connectionStringName);
                if (connectionString == null)
                    throw new Exception($"Missing connection string {connectionStringName} required for logging");
                var service = new SystemLogService(connectionString);
                service.IsPendingStartup = true;
                return service;
            });
            services.AddSingleton<NTechAuditSystemLogBatchingService>(x =>
            {
                var systemUser = x.GetRequiredService<NHttpServiceSystemUser>();
                var serviceClientFactory = x.GetRequiredService<ServiceClientFactory>();
                var env = x.GetRequiredService<NEnv>();
                var logger = x.GetRequiredService<ILogger<NTechAuditSystemLogBatchingService>>();
                var loggingAuditClient = new AuditClient(systemUser, serviceClientFactory);
                return new NTechAuditSystemLogBatchingService(env, logger, x.GetRequiredService<SystemLogService>());
            });
            services.AddHostedService<BackgroundServiceStarter<NTechAuditSystemLogBatchingService>>();
            services.AddTransient<ILoggingService, CoreLoggingService>();
        }

        public static void UseApplication(WebApplication app, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, NEnv env)
        {
            app.UseMiddleware<NTechErrorLoggingMiddleware>();
            var auditService = serviceProvider.GetRequiredService<NTechAuditSystemLogBatchingService>();
            loggerFactory.AddProvider(new NTechLoggerProvider(auditService, env));
        }

        public static void AddApiRequestLogging(IServiceCollection services, NEnv env)
        {
            if (env.IsHttpRequestLoggingEnabled)
            {
                services.AddHttpLogging(options =>
                {
                    options.LoggingFields = HttpLoggingFields.All;
                });
            }
        }

        public static void UseApiRequestLogging(WebApplication app, NEnv env)
        {
            if (env.IsHttpRequestLoggingEnabled)
            {
                app.UseHttpLogging();
            }
        }
    }
}
