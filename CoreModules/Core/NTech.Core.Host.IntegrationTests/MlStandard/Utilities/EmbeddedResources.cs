using System.Xml.Linq;

namespace NTech.Core.Host.IntegrationTests.MlStandard.Utilities
{
    internal static class EmbeddedResources
    {
        private static T UsingStream<T>(string filename, Func<Stream, T> f)
        {
            var assembly = typeof(EmbeddedResources).Assembly;
            var resourceName = $"NTech.Core.Host.IntegrationTests.Resources.{filename}";
            using (Stream stream = assembly.GetManifestResourceStream(resourceName)!)
            {
                return f(stream);
            }
        }

        public static XDocument LoadEmbeddedXmlDocument(string filename) => UsingStream(filename, x => XDocument.Load(x));
    }
}
