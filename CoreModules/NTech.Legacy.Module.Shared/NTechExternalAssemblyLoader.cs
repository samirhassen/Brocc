using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NTech.Services.Infrastructure
{
    public class NTechExternalAssemblyLoader
    {
        private System.Collections.Concurrent.ConcurrentDictionary<string, Assembly> loadedAssemblies = new System.Collections.Concurrent.ConcurrentDictionary<string, Assembly>();
        private object loadLock = new object();

        private Assembly LoadFromFileInternal(string assemblyFile)
        {
            lock (loadLock)
            {
                var p = Assembly.LoadFrom(assemblyFile);

                if (p == null)
                    throw new Exception($"Could not load assembly: '{assemblyFile}'");

                return p;
            }
        }

        public Assembly LoadFromFile(FileInfo assemblyFile)
        {
            return loadedAssemblies.GetOrAdd(assemblyFile.FullName, LoadFromFileInternal);
        }

        public List<Assembly> GetLoadedAssemblies()
        {
            return loadedAssemblies.Values.ToList();
        }

        public List<Tuple<FileInfo, Assembly>> LoadPlugins(List<string> pluginFolders, List<string> enabledPluginNames)
        {
            var pluginAssemblies = new List<Tuple<FileInfo, Assembly>>();
            var loadedPluginNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (pluginFolders == null || enabledPluginNames == null)
                return pluginAssemblies;

            foreach (var pluginFolder in pluginFolders)
            {
                foreach (var confirmedPlugin in new DirectoryInfo(pluginFolder).GetFiles().Where(x => enabledPluginNames.Contains(x.Name, StringComparer.InvariantCultureIgnoreCase)))
                {
                    if (!loadedPluginNames.Contains(confirmedPlugin.Name))
                    {
                        pluginAssemblies.Add(Tuple.Create(confirmedPlugin, LoadFromFile(confirmedPlugin)));
                        loadedPluginNames.Add(confirmedPlugin.Name);
                    }
                }
            }

            return pluginAssemblies;
        }
    }
}
