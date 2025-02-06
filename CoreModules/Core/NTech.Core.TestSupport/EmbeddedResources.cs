using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace nTest.RandomDataSource
{
    public static class EmbeddedResources
    {
        private static string GetResourceName(string filename)
        {
            return $"NTech.Core.TestSupport.Resources.{filename}";
        }

        public static T UsingStream<T>(string filename, Func<Stream, T> f)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = GetResourceName(filename);
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                return f(stream);
            }
        }

        public static string LoadFileAsString(string filename)
        {
            return UsingStream(filename, stream =>
            {
                using (var sr = new StreamReader(stream))
                {
                    return sr.ReadToEnd();
                }
            });
        }

        public static List<string> LoadAsLines(string filename)
        {
            return UsingStream(filename, stream =>
            {
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
            });
        }
    }
}
