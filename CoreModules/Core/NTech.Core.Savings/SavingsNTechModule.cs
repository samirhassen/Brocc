using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NTech.Core.Module;

namespace NTech.Core.Savings
{
    public class SavingsNTechModule : NTechModule
    {
        public override string ServiceName => "nSavings";

        public override void AddServices(IServiceCollection services, NEnv env)
        {

        }

        public override void OnApplicationStarted(ILogger logger)
        {

        }
    }
}