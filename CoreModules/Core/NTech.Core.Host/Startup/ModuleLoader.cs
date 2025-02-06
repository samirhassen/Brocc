using NTech.Core.Module;

namespace NTech.Core.Host.Startup
{
    public static class ModuleLoader
    {
        public static readonly Lazy<List<NTechModule>> AllModules = new Lazy<List<NTechModule>>(() =>
        {
            //TODO: Add dynamic loading of clients adaptation modules in addition to this
            var moduleTypes = new Type[]
            {
                typeof(Credit.CreditNTechModule),
                typeof(Customer.CustomerNTechModule),
                typeof(PreCredit.PreCreditNTechModule),
                typeof(User.UserNTechModule)
            };

            var allModules = new List<NTechModule>();

            foreach (var moduleType in moduleTypes)
            {
                try
                {
                    allModules.Add((NTechModule)moduleType.GetConstructor(Type.EmptyTypes).Invoke(new object[] { }));
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
            var allModules = ModuleLoader.AllModules.Value;
            foreach (var module in allModules)
            {
                var isModuleActive = module.IsActive(env);
                if (isModuleActive)
                {
                    source.AddApplicationPart(module.SourceAssembly);
                }
            }
            source
                .ConfigureApplicationPartManager(partManager =>
                 {
                     var allModules = ModuleLoader.AllModules.Value;
                     foreach (var module in allModules)
                     {
                         if (!module.IsActive(env))
                         {
                             var part = partManager.ApplicationParts.Where(x => x.Name == module.PartName).FirstOrDefault();
                             if (part != null)
                             {
                                 partManager.ApplicationParts.Remove(part);
                             }
                         }
                     }
                 });

            return source;
        }
    }    
}
