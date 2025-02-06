using NTech.Core.Host.Infrastructure;
using NTech.Core.Module.Shared.Services;

namespace NTech.Core.Host.Startup
{
    public static class SettingsRegistration
    {
        public static void AddSettingsServices(IServiceCollection services)
        {
            services.AddScoped<CachedSettingsService>();
        }

        public static void UseSettings(WebApplication app)
        {
            app.Lifetime.ApplicationStarted.Register(() =>
            {
                var queue = app.Services.GetRequiredService<ICrossModuleEventQueue>();
                queue.AddEventHandlerIfNotPresent("f8a15c75-f733-487a-ab1c-9cb0ad5f0b5e", () => new SettingsChangedHandler());
            });
        }

        private class SettingsChangedHandler : ICrossModuleEventHandler
        {
            public string EventName => "SettingChanged";

            public Task HandleEvent(CrossModuleEvent evt, IServiceScope serviceScope, ILogger logger, CancellationToken cancellationToken)
            {
                logger.LogInformation($"Setting changed: {evt.EventData}");
                CachedSettingsService.OnSettingChanged(evt.EventData);
                return Task.CompletedTask;
            }
        }
    }
}
