using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.Conversion
{
    public static class EmbeddedResources
    {
        /// <summary>
        /// Read an embedded resource.
        /// 
        /// Example:
        /// - You have a project nKitten with a file Resources/Claws.txt
        /// - You would then use namespace nKitten.Resources and fileName Claws.txt
        /// </summary>
        /// <typeparam name="T">Output type</typeparam>
        /// <param name="namespaceName">Namespace (see example)</param>
        /// <param name="fileName">Filename (see example)</param>
        /// <param name="f">Transform</param>
        /// <returns>Result of transform</returns>
        public static T WithEmbeddedStream<T>(string namespaceName, string fileName, Func<Stream, T> f)
        {
            var assembly = Assembly.GetCallingAssembly();
            var resourceName = $"{namespaceName}.{fileName}";
            using (var s = assembly.GetManifestResourceStream(resourceName))
            {
                if (s == null)
                    throw new Exception($"No embedded resource named '{resourceName}' found in assembly '{assembly.FullName}'");
                return f(s);
            }
        }
    }
}
