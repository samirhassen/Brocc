using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace nCreditReport.RandomDataSource
{
    internal static class EmbeddedResources
    {
        private static string GetResourceName(string filename)
        {
            return $"nCreditReport.Resources.{filename}";
        }

        public static string LoadFileAsString(string filename)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = GetResourceName(filename);
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var sr = new StreamReader(stream))
            {
                return sr.ReadToEnd();
            }
        }

        public static List<string> LoadAsLines(string filename)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = GetResourceName(filename);
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var sr = new StreamReader(stream))
            {
                var lines = new List<string>();
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    lines.Add(line);
                }
                return lines;
            }
        }
    }
}
