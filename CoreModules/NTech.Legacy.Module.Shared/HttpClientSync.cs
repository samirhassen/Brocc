using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace NTech.Services.Infrastructure
{
    public class HttpClientSync
    {
        private readonly HttpClient httpClient; //Keep a pool of these

        public HttpClientSync(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public HttpClientSync() : this(new HttpClient())
        {

        }

        public HttpResponseMessageSync Send(HttpRequestMessage request) =>
            new HttpResponseMessageSync(RunSync(httpClient.SendAsync(request)));

        public class HttpResponseMessageSync
        {
            private readonly HttpResponseMessage response;
            private readonly Lazy<HttpContentSync> content;

            public HttpResponseMessageSync(HttpResponseMessage response)
            {
                this.response = response;
                this.content = new Lazy<HttpContentSync>(() => new HttpContentSync(response.Content));
            }

            public bool IsSuccessStatusCode => response.IsSuccessStatusCode;
            public System.Net.HttpStatusCode StatusCode => response.StatusCode;

            public HttpContentSync Content => content.Value;
        }

        public class HttpContentSync
        {
            private readonly HttpContent content;
            public HttpContentSync(HttpContent content)
            {
                this.content = content;
            }

            public string ReadAsString() => RunSync(content.ReadAsStringAsync());
            public void CopyTo(System.IO.Stream stream) => RunSyncVoid(content.CopyToAsync(stream));
            public HttpContentHeaders Headers => content.Headers;

            public bool IsJson() =>
                content.Headers.ContentType.MediaType.Contains("application/json");

            public bool IsMultipartFormDataContent() =>
                (content as MultipartFormDataContent) != null;
        }

        private static T RunSync<T>(Task<T> asyncCall) => Task.Run(() => asyncCall).GetAwaiter().GetResult();
        private static void RunSyncVoid(Task asyncCall) => Task.Run(() => asyncCall).GetAwaiter().GetResult();
    }
}
