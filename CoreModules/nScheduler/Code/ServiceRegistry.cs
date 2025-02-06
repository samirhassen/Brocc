using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace nScheduler.Code
{
    public class ServiceRegistry
    {
        private readonly IDictionary<string, Uri> serviceUris;

        private ServiceRegistry(IDictionary<string, Uri> serviceUris)
        {
            this.serviceUris = serviceUris;
        }

        public Uri CreateServiceUri(string serviceName, string relativeUri)
        {
            if (!serviceUris.ContainsKey(serviceName))
                throw new Exception($"The service {serviceName} does not exist in the registry");
            try
            {
                return new Uri(serviceUris[serviceName], relativeUri);
            }
            catch
            {
                throw new Exception($"Invalid relativeUri '{relativeUri}'");
            }
        }

        public Uri GetBaseServiceUri(string serviceName)
        {
            if (!serviceUris.ContainsKey(serviceName))
                throw new Exception($"The service {serviceName} does not exist in the registry");
            return serviceUris[serviceName];
        }

        public bool ContainsService(string serviceName)
        {
            return serviceUris.ContainsKey(serviceName);
        }

        public static ServiceRegistry CreateFromDict(IDictionary<string, string> source)
        {
            return new ServiceRegistry(source.ToDictionary(x => x.Key, x => new Uri(x.Value), StringComparer.OrdinalIgnoreCase));
        }

        public static ServiceRegistry ParseFile(string filename)
        {
            return new ServiceRegistry(File.ReadAllLines(filename).Where(x => !x.StartsWith("#") || string.IsNullOrWhiteSpace(x)).Select(x =>
            {
                var ss = x.Split('=');
                return new { K = ss[0].Trim(), V = ss[1].Trim() };
            }).ToDictionary(x => x.K, x => new Uri(x.V), StringComparer.InvariantCultureIgnoreCase));
        }

        public IDictionary<string, string> AsDictionary()
        {
            return this.serviceUris.ToDictionary(x => x.Key, x => x.Value.ToString());
        }
    }
}