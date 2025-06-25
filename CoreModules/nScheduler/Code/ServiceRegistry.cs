using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace nScheduler.Code
{
    public class ServiceRegistry
    {
        private readonly IDictionary<string, Uri> _serviceUris;

        private ServiceRegistry(IDictionary<string, Uri> serviceUris)
        {
            _serviceUris = serviceUris;
        }

        public Uri CreateServiceUri(string serviceName, string relativeUri, Dictionary<string, string> parameters)
        {
            if (!_serviceUris.TryGetValue(serviceName, out var uri))
                throw new Exception($"The service {serviceName} does not exist in the registry");
            try
            {
                var builder = new UriBuilder(new Uri(uri, relativeUri));

                if (parameters is { Count: 0 }) return builder.Uri;

                var query = HttpUtility.ParseQueryString("");
                foreach (var parameter in parameters)
                {
                    query.Add(parameter.Key, parameter.Value);
                }

                builder.Query = query.ToString();

                return new Uri(builder.ToString());
            }
            catch
            {
                throw new Exception($"Invalid relativeUri '{relativeUri}'");
            }
        }

        public Uri GetBaseServiceUri(string serviceName)
        {
            if (!_serviceUris.TryGetValue(serviceName, out var uri))
                throw new Exception($"The service {serviceName} does not exist in the registry");
            return uri;
        }

        public bool ContainsService(string serviceName)
        {
            return _serviceUris.ContainsKey(serviceName);
        }

        public static ServiceRegistry CreateFromDict(IDictionary<string, string> source)
        {
            return new ServiceRegistry(source.ToDictionary(x => x.Key, x => new Uri(x.Value),
                StringComparer.OrdinalIgnoreCase));
        }

        public static ServiceRegistry ParseFile(string filename)
        {
            return new ServiceRegistry(File.ReadAllLines(filename)
                .Where(x => !x.StartsWith("#") || string.IsNullOrWhiteSpace(x)).Select(x =>
                {
                    var ss = x.Split('=');
                    return new { K = ss[0].Trim(), V = ss[1].Trim() };
                }).ToDictionary(x => x.K, x => new Uri(x.V), StringComparer.InvariantCultureIgnoreCase));
        }

        public IDictionary<string, string> AsDictionary()
        {
            return _serviceUris.ToDictionary(x => x.Key, x => x.Value.ToString());
        }
    }
}