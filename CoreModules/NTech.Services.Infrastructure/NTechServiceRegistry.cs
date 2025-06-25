using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace NTech.Services.Infrastructure
{
    public class NTechServiceRegistry
    {
        /// <summary>
        /// Use when making calls from one service to another (these addresses may be things like customer.company.local and only in the internal dns)
        /// </summary>
        public AccessPath Internal { get; }

        /// <summary>
        /// Use when sharing a link to a resource with an exteral party. That is when you need an address that is resolvable on the public internet.
        /// </summary>
        public AccessPath External { get; }

        public NTechServiceRegistry(IDictionary<string, string> standardSource,
            IDictionary<string, string> internalOverridesSource)
        {
            var d = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var k in standardSource)
                d[k.Key] = k.Value;
            foreach (var k in internalOverridesSource)
                d[k.Key] = k.Value;

            External = new AccessPath(standardSource);
            Internal = new AccessPath(d);
        }

        public bool ContainsService(string name)
        {
            return Internal.ContainsKey(name) || External.ContainsKey(name);
        }

        private static IDictionary<string, string> ParseFile(FileInfo f)
        {
            return File.ReadAllLines(f.FullName)
                .Select(x => x?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#")).Select(x =>
                {
                    var ss = x.Split('=');
                    return new { K = ss[0].Trim(), V = ss[1].Trim() };
                }).ToDictionary(x => x.K, x => x.V, StringComparer.InvariantCultureIgnoreCase);
        }

        public static NTechServiceRegistry ParseFromFiles(FileInfo serviceRegistry, FileInfo internalServiceRegistry)
        {
            var standardRegistry = ParseFile(serviceRegistry);
            var internalOverrides = internalServiceRegistry == null || !internalServiceRegistry.Exists
                ? new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
                : ParseFile(internalServiceRegistry);

            return new NTechServiceRegistry(standardRegistry, internalOverrides);
        }

        /// <summary>
        /// Removes any leading or trailing forward slashes
        /// </summary>
        public static string NormalizePath(string path)
        {
            return path?.TrimStart('/').TrimEnd('/') ?? "";
        }

        public static Uri CreateUrl(Uri rootUrl, string relativeUrl,
            params Tuple<string, string>[] queryStringParameters)
        {
            var relativeUrlTrimmed = NormalizePath(relativeUrl);

            var queryParams = new List<Tuple<string, string>>();

            var i = relativeUrlTrimmed.IndexOf('?');
            if (i >= 0)
            {
                var queryString = relativeUrlTrimmed.Substring(i);
                var queryStringParsed = HttpUtility.ParseQueryString(queryString);
                foreach (var p in queryStringParsed.AllKeys)
                    queryParams.Add(Tuple.Create(p, queryStringParsed[p]));
                relativeUrlTrimmed = relativeUrlTrimmed.Substring(0, i);
            }

            queryParams.AddRange(queryStringParameters.Where(x =>
                !string.IsNullOrWhiteSpace(x.Item1) && !string.IsNullOrWhiteSpace(x.Item2)));

            var u = new UriBuilder(
                rootUrl.Scheme,
                rootUrl.Host,
                rootUrl.Port,
                rootUrl.Segments.Length == 1
                    ? relativeUrlTrimmed
                    : NormalizePath(rootUrl.LocalPath) + "/" + relativeUrlTrimmed);

            var query = HttpUtility.ParseQueryString(rootUrl.Query);
            foreach (var p in queryParams)
                query[p.Item1] = p.Item2;
            u.Query = query.ToString();

            return u.Uri;
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
                get => source[key];
                set => source[key] = value;
            }

            public ICollection<string> Keys => source.Keys;

            public ICollection<string> Values => source.Values;

            public int Count => source.Count;

            public bool IsReadOnly => source.IsReadOnly;

            public void Add(string key, string value)
            {
                source.Add(key, value);
            }

            public void Add(KeyValuePair<string, string> item)
            {
                source.Add(item);
            }

            public void Clear()
            {
                source.Clear();
            }

            public bool Contains(KeyValuePair<string, string> item)
            {
                return source.Contains(item);
            }

            public bool ContainsKey(string key)
            {
                return source.ContainsKey(key);
            }

            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
            {
                source.CopyTo(array, arrayIndex);
            }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                return source.GetEnumerator();
            }

            public bool Remove(string key)
            {
                return source.Remove(key);
            }

            public bool Remove(KeyValuePair<string, string> item)
            {
                return source.Remove(item);
            }

            public bool TryGetValue(string key, out string value)
            {
                return source.TryGetValue(key, out value);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return source.GetEnumerator();
            }

            public Uri ServiceRootUri(string serviceName)
            {
                if (!source.TryGetValue(serviceName, out var value))
                    throw new Exception($"Service '{serviceName}' is not in the service registry");
                var u = new Uri(value?.TrimEnd('/') ?? string.Empty);
                if (!u.IsAbsoluteUri)
                    throw new Exception($"Service '{serviceName}' has a relative path in the service registry");
                return u;
            }

            private const string FragmentToken = "__fragment_token__";

            public Uri ServiceUrl(string serviceName, string relativeUrl,
                params Tuple<string, string>[] queryStringParameters)
            {
                var rootUri = ServiceRootUri(serviceName);

                var relativeUrlTrimmed = relativeUrl?.TrimStart('/').TrimEnd('/') ?? "";

                var uri = rootUri.Segments.Length == 1
                    ? new Uri(rootUri, relativeUrlTrimmed)
                    : new Uri(rootUri, $"{rootUri.LocalPath.TrimStart('/')}/{relativeUrlTrimmed}");

                if (queryStringParameters == null || queryStringParameters.Length == 0)
                    return uri;

                var fragment = uri.Fragment;

                var ps = queryStringParameters.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Item2)).ToList();
                var u = new UriBuilder(
                    uri.Scheme, uri.Host, uri.Port,
                    uri.LocalPath + (string.IsNullOrWhiteSpace(fragment) ? "" : FragmentToken),
                    (string.IsNullOrWhiteSpace(uri.Query) ? "?" : uri.Query + "&") +
                    string.Join("&", ps.Select(x => $"{x.Item1}={x.Item2}")));
                uri = u.Uri;

                if (!string.IsNullOrWhiteSpace(fragment))
                    uri = new Uri(uri.ToString().Replace(FragmentToken, fragment));

                return uri;
            }
        }
    }
}