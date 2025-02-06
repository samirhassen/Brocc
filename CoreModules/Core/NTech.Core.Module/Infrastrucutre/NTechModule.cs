using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace NTech.Core.Module
{
    public abstract class NTechModule
    {
        /// <summary>
        /// Name used to refer to this service in the Service registry
        /// </summary>
        public abstract string ServiceName { get; }

        /// <summary>
        /// Used to load the controllers as application parts
        /// </summary>
        public Assembly SourceAssembly => this.GetType().Assembly;
        public virtual List<Assembly> ExtraDocumentationAssemblies => new List<Assembly>();
        public string PartName => SourceAssembly.GetName().Name;

        /// <summary>
        /// Register services for dependancy injection
        /// </summary>
        public abstract void AddServices(IServiceCollection services, NEnv env);

        public bool IsActive(NEnv env) => env.ServiceRegistry.ContainsService(ServiceName);

        public abstract void OnApplicationStarted(ILogger logger);
    }
}
