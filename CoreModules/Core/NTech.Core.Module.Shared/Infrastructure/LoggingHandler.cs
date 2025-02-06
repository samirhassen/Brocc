using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NTech.Services.Infrastructure
{
    public class LoggingHandler : DelegatingHandler
    {
        private readonly Action<string> log;

        public LoggingHandler(Action<string> log)
            : base(new HttpClientHandler())
        {
            this.log = log;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            StringBuilder b = new StringBuilder();

            var w = Stopwatch.StartNew();

            b.AppendLine("Request:");
            b.AppendLine(request.ToString());
            if (request.Content != null)
                b.AppendLine(await request.Content.ReadAsStringAsync().ConfigureAwait(false));

            b.AppendLine();

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            b.AppendLine("Response:");
            b.AppendLine(response.ToString());
            if (response.Content != null)
            {
                b.AppendLine(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
            b.AppendLine();
            
            w.Stop();

            try
            {
                log($"TotalTimeInMs={w.ElapsedMilliseconds}{Environment.NewLine}{b.ToString()}");
            }
            catch
            {
                //Ignored
            }

            return response;
        }
    }
}
