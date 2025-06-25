using System.Collections;

namespace NTech.Core.Module
{

    public class NTechServiceRegistry : INTechServiceRegistry
    {
        private readonly AccessPath externalSource;
        private readonly AccessPath internalSource;

        public NTechServiceRegistry(IDictionary<string, string> standardSource, IDictionary<string, string> internalOverridesSource)
        {
            var d = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var k in standardSource)
                d[k.Key] = k.Value;
            foreach (var k in internalOverridesSource)
                d[k.Key] = k.Value;

            this.externalSource = new AccessPath(standardSource);
            this.internalSource = new AccessPath(d);
        }

        public Uri InternalServiceUrl(string serviceName, string relativeUrl, params Tuple<string, string>[] queryStringParameters) =>
            this.Internal.ServiceUrl(serviceName, relativeUrl, queryStringParameters);

        public Uri ExternalServiceUrl(string serviceName, string relativeUrl, params Tuple<string, string>[] queryStringParameters) =>
            this.External.ServiceUrl(serviceName, relativeUrl, queryStringParameters);

        public bool ContainsService(string name)
        {
            return this.Internal.ContainsKey(name) || this.External.ContainsKey(name);
        }

        /// <summary>
        /// Use when making calls from one service to another (these addresses may be things like customer.company.local and only in the internal dns)
        /// </summary>
        public AccessPath Internal
        {
            get
            {
                return this.internalSource;
            }
        }

        /// <summary>
        /// Use when sharing a link to a resource with an exteral party. That is when you need an address that is resolvable on the public internet.
        /// </summary>
        public AccessPath External
        {
            get
            {
                return this.externalSource;
            }
        }

        private static IDictionary<string, string> parseFile(FileInfo f)
        {
            return File.ReadAllLines(f.FullName).Select(x => x?.Trim()).Where(x => !x.StartsWith("#") && !string.IsNullOrWhiteSpace(x)).Select(x =>
            {
                var ss = x.Split('=');
                return new { K = ss[0].Trim(), V = ss[1].Trim() };
            }).ToDictionary(x => x.K, x => x.V, StringComparer.InvariantCultureIgnoreCase);
        }

        public static NTechServiceRegistry ParseFromFiles(FileInfo serviceRegistry, FileInfo internalServiceRegistry)
        {
            var standardRegistry = parseFile(serviceRegistry);
            var internalOverrides = (internalServiceRegistry == null || !internalServiceRegistry.Exists)
                ? new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
                : parseFile(internalServiceRegistry);

            return new NTechServiceRegistry(standardRegistry, internalOverrides);
        }

        public static Uri CreateUrl(Uri rootUrl, string relativeUrl, params Tuple<string, string>[] queryStringParameters)
        {
            var relativeUrlTrimmed = relativeUrl?.TrimStart('/')?.TrimEnd('/');

            var uri = (rootUrl.Segments.Length == 1)
                ? new Uri(rootUrl, relativeUrlTrimmed)
                : new Uri(rootUrl, rootUrl.LocalPath.TrimStart('/') + '/' + relativeUrlTrimmed);

            if (queryStringParameters != null && queryStringParameters.Length > 0)
            {
                var u = new UriBuilder(
                    uri.Scheme, uri.Host, uri.Port, uri.LocalPath,
                    (string.IsNullOrWhiteSpace(uri.Query) ? "?" : uri.Query + "&") + string.Join("&", queryStringParameters.Select(x => $"{x.Item1}={x.Item2}")));
                uri = u.Uri;
            }

            return uri;
        }

        public class AccessPath : IDictionary<string, string>
        {
            private readonly IDictionary<string, string> source;

            public AccessPath(IDictionary<string, string> source)
            {
                this.source = source;
            }

            public string this[string key]
            {
                get { return this.source[key]; }
                set { this.source[key] = value; }
            }

            public ICollection<string> Keys { get { return this.source.Keys; } }

            public ICollection<string> Values { get { return this.source.Values; } }

            public int Count { get { return this.source.Count; } }

            public bool IsReadOnly { get { return this.source.IsReadOnly; } }

            public void Add(string key, string value)
            {
                this.source.Add(key, value);
            }

            public void Add(KeyValuePair<string, string> item)
            {
                this.source.Add(item);
            }

            public void Clear()
            {
                this.source.Clear();
            }

            public bool Contains(KeyValuePair<string, string> item)
            {
                return this.source.Contains(item);
            }

            public bool ContainsKey(string key)
            {
                return this.source.ContainsKey(key);
            }

            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
            {
                this.source.CopyTo(array, arrayIndex);
            }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                return this.source.GetEnumerator();
            }

            public bool Remove(string key)
            {
                return this.source.Remove(key);
            }

            public bool Remove(KeyValuePair<string, string> item)
            {
                return this.source.Remove(item);
            }

            public bool TryGetValue(string key, out string value)
            {
                return this.source.TryGetValue(key, out value);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.source.GetEnumerator();
            }

            public Uri ServiceRootUri(string serviceName)
            {
                if (!this.source.ContainsKey(serviceName))
                    throw new Exception($"Service '{serviceName}' is not in the service registry");
                var u = new Uri(this.source[serviceName]?.TrimEnd('/'));
                if (!u.IsAbsoluteUri)
                    throw new Exception($"Service '{serviceName}' has a relative path in the service registry");
                return u;
            }

            public Uri ServiceUrl(string serviceName, string relativeUrl, params Tuple<string, string>[] queryStringParameters)
            {
                var rootUri = ServiceRootUri(serviceName);

                var relativeUrlTrimmed = relativeUrl?.TrimStart('/')?.TrimEnd('/');

                var uri = (rootUri.Segments.Length == 1)
                    ? new Uri(rootUri, relativeUrlTrimmed)
                    : new Uri(rootUri, rootUri.LocalPath.TrimStart('/') + '/' + relativeUrlTrimmed);

                if (queryStringParameters != null && queryStringParameters.Length > 0)
                {
                    var ps = queryStringParameters.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Item2)).ToList();
                    var u = new UriBuilder(
                        uri.Scheme, uri.Host, uri.Port, uri.LocalPath,
                        (string.IsNullOrWhiteSpace(uri.Query) ? "?" : uri.Query + "&") + string.Join("&", ps.Select(x => $"{x.Item1}={x.Item2}")));
                    uri = u.Uri;
                }

                return uri;
            }
        }
    }
}
