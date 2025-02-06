using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace NTech.Services.Infrastructure.NTechWs
{
    public class NTechWebserviceMethodRequestContext : INTechWebserviceCustomData
    {
        public HttpRequestBase HttpRequest { get; set; }
        public System.Security.Claims.ClaimsIdentity CurrentUserIdentity { get; set; }
        private Dictionary<string, Lazy<object>> customData = new Dictionary<string, Lazy<object>>();
        public bool IsExceptionLoggingDisabled { get; set; }
        public bool IsRequestLoggingDisabled { get; set; }

        public TValue GetCustomDataValueOrNull<TValue>(string name) where TValue : class
        {
            if (!customData.ContainsKey(name))
                return null;
            return customData[name].Value as TValue;
        }

        public void SetCustomData<TValue>(string name, Func<TValue> valueFactory)
        {
            customData[name] = new Lazy<object>(() => valueFactory());
        }

        private string requestJson = null;
        public string RequestJson
        {
            get
            {
                if (requestJson == null && IsJsonRequest)
                {
                    HttpRequest.InputStream.Position = 0;
                    using (var r = new StreamReader(HttpRequest.InputStream, HttpRequest.ContentEncoding))
                    {
                        requestJson = r.ReadToEnd();
                    }
                }
                return requestJson;
            }
        }

        public string GetRequestHeader(string name)
        {
            return this.HttpRequest.Headers.GetValues(name)?.FirstOrDefault();
        }

        public bool IsJsonRequest
        {
            get
            {
                return GetRequestHeader("Content-Type")?.ToLowerInvariant()?.Contains("json") ?? false;
            }
        }

        public TRequest ParseJsonRequest<TRequest>() where TRequest : class
        {
            try
            {
                var j = RequestJson;
                if (j == null)
                    return null;
                return JsonConvert.DeserializeObject<TRequest>(j);
            }
            catch (JsonReaderException ex)
            {
                throw new NTechWebserviceMethodException(ex.Message, ex) { ErrorCode = "invalidJson", IsUserFacing = true };
            }
        }

        public TRequest ParseJsonRequestAnonymous<TRequest>(TRequest anonymousTypeObject) where TRequest : class
        {
            try
            {
                return JsonConvert.DeserializeAnonymousType(RequestJson, anonymousTypeObject);
            }
            catch (JsonReaderException ex)
            {
                throw new NTechWebserviceMethodException(ex.Message, ex) { ErrorCode = "invalidJson", IsUserFacing = true };
            }
        }

        //Fake a json request to make the parser be exactly the same always. Performance shouldnt be an issue since we only gets for streaming binary requests
        public TRequest ParseQueryStringRequest<TRequest>(Action<string> observeAsJson = null) where TRequest : class
        {
            return ParseNameValueCollection<TRequest>(HttpRequest.QueryString, "invalidQueryString", observeAsJson: observeAsJson);
        }

        public TRequest ParseFormContent<TRequest>(Action<string> observeAsJson = null) where TRequest : class
        {
            return ParseNameValueCollection<TRequest>(HttpRequest.Form, "invalidForm", observeAsJson: observeAsJson);
        }

        public bool IsForwardedCustomerPagesApiCall() => this.HttpRequest.Headers.AllKeys.Contains("x-ntech-customerpages-forward");

        private TRequest ParseNameValueCollection<TRequest>(System.Collections.Specialized.NameValueCollection q, string errorCode, Action<string> observeAsJson = null) where TRequest : class
        {
            try
            {
                const string p = "\"";
                var items = q
                    .AllKeys
                    .Select(x => $"{p}{x}{p}: {p}{q.Get(x)}{p}")
                    .Where(x => x != null)
                    .ToList();

                var json = "{" + string.Join($",{Environment.NewLine}", items) + "}";
                observeAsJson?.Invoke(json);
                return JsonConvert.DeserializeObject<TRequest>(json);
            }
            catch (JsonReaderException ex)
            {
                throw new NTechWebserviceMethodException(ex.Message, ex) { ErrorCode = errorCode, IsUserFacing = true };
            }
            catch (Exception)
            {
                throw new NTechWebserviceMethodException(errorCode) { ErrorCode = errorCode, IsUserFacing = true };
            }
        }
    }

    public interface INTechWebserviceCustomData
    {
        TValue GetCustomDataValueOrNull<TValue>(string name) where TValue : class;
        void SetCustomData<TValue>(string name, Func<TValue> valueFactory);
    }
}
