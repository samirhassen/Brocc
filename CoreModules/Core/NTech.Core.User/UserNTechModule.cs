using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.User.Database;
using NTech.Core.User.Shared;
using NTech.Core.User.Shared.Services;

namespace NTech.Core.User
{
    public class UserNTechModule : NTechModule
    {
        public override string ServiceName => "nUser";

        public override void AddServices(IServiceCollection services, NEnv env)
        {
            services.AddScoped(x =>
            {
                var user = x.GetRequiredService<INTechCurrentUserMetadata>();
                var clock = x.GetRequiredService<ICoreClock>();
                return new UserContextFactory(() => new UserContextExtended(user, clock));
            });
            services.AddScoped<ApiKeyService>();
        }

        public override void OnApplicationStarted(ILogger logger)
        {

        }
    }
}