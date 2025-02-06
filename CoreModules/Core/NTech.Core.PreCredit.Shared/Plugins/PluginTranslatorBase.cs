using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace nPreCredit.Code.Plugins
{
    public abstract class PluginTranslatorBase
    {
        protected static Type FindPluginType(Type t)
        {
            var types = FindPluginTypes(t);

            if (types.Count > 1)
                throw new Exception($"Multiple implementations of {t.Name} found!");

            return types[0];
        }

        protected static Type FindPluginTypeImplementingInterface(Type t, Type interfaceType)
        {
            var types = FindPluginTypes(t);

            types = types.Where(x => interfaceType.IsAssignableFrom(x)).ToList();

            if (types.Count > 1)
                throw new Exception($"Multiple implementations of {t.Name} found!");

            return types[0];
        }

        protected static List<Type> FindPluginTypes(Type t)
        {
            List<Type> types = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes().Where(p => !p.IsAbstract && p.IsSubclassOfRawGeneric(t)))
                    {
                        types.Add(type);
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    throw new Exception("Type loading error in assembly " + assembly.FullName, ex);
                }
            }

            if (types.Count == 0)
                throw new Exception($"No implementations of {t.Name} found!");

            return types;
        }

        protected static Type GetRequestType(Type pluginType, object instance = null)
        {
            var t = pluginType;
            instance = instance ?? t.GetConstructors().Single().Invoke(null);
            return (Type)t.GetProperty("RequestType").GetValue(instance, null);
        }
    }
}