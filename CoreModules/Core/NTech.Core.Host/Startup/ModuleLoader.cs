using NTech.Core.Credit;
using NTech.Core.Customer;
using NTech.Core.Module;
using NTech.Core.PreCredit;
using NTech.Core.User;

namespace NTech.Core.Host.Startup
{
    public static class ModuleLoader
    {
        public static readonly Lazy<List<NTechModule>> AllModules = new(() =>
        {
            //TODO: Add dynamic loading of clients adaptation modules in addition to this
            var moduleTypes = new[]
            {
                typeof(CreditNTechModule),
                typeof(CustomerNTechModule),
                typeof(PreCreditNTechModule),
                typeof(UserNTechModule)
            };

            var allModules = new List<NTechModule>();

            foreach (var moduleType in moduleTypes)
            {
                try
                {
                    allModules.Add(
                        (NTechModule)moduleType.GetConstructor(Type.EmptyTypes).Invoke(Array.Empty<object>()));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Could not construct NTechModule for {moduleType.FullName}", ex);
                }
            }

            return allModules;
        });

        public static IMvcBuilder ConfigureActiveModules(this IMvcBuilder source, NEnv env)
        {
            var allModules = AllModules.Value;
            foreach (var module in allModules.Where(m => m.IsActive(env)))
            {
                source.AddApplicationPart(module.SourceAssembly);
            }

            source.ConfigureApplicationPartManager(partManager =>
            {
                foreach (var module in AllModules.Value.Where(m => !m.IsActive(env)))
                {
                    var part = partManager.ApplicationParts
                        .FirstOrDefault(x => x.Name == module.PartName);
                    if (part != null)
                    {
                        partManager.ApplicationParts.Remove(part);
                    }
                }
            });

            return source;
        }
    }
}