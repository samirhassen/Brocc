using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace NTechSignicat.Services
{
    public class UrlBuilder
    {
        private Uri baseUrl;
        private List<Tuple<string, string>>  parameters = new List<Tuple<string, string>>();
        private UrlBuilder(Uri baseUrl)
        {
            this.baseUrl = baseUrl;
        }

        public static UrlBuilder Create(Uri baseUrl, string relativeUrl)
        {
            return new UrlBuilder(new Uri(baseUrl, relativeUrl));
        }

        public UrlBuilder AddParam(string name, string value, bool encode = false)
        {
            parameters.Add(Tuple.Create(name, encode ? Uri.EscapeDataString(value) : value));
            return this;
        }

        public Uri ToUri()
        {
            if (parameters.Count == 0)
                return baseUrl;

            var ps = string.Join("&", parameters.Select(x => $"{x.Item1}={x.Item2}"));
            return new Uri($"{baseUrl}?{ps}");
        }

        public static Uri AppendQueryStringParams(Uri uri, params Tuple<string, string>[] parameters)
        {
            var uriBuilder = new UriBuilder(uri);
            var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
            foreach(var p in parameters)
                query[p.Item1] = p.Item2;
            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri;
        }
    }
}
