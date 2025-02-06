using Microsoft.Owin;
using Serilog;
using Serilog.Core.Enrichers;
using System;
using System.Threading.Tasks;
using System.Linq;
using Serilog.Core;
using System.Collections.Generic;
using System.Security.Claims;

namespace NTech.Services.Infrastructure
{
    public class NTechLoggingMiddleware : OwinMiddleware
    {
        private string serviceName;

        public NTechLoggingMiddleware(OwinMiddleware next, string serviceName) : base(next)
        {
            this.serviceName = serviceName;
        }

        public static IEnumerable<ILogEventEnricher> GetProperties(IOwinContext context)
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString();
            if (version != null)
                yield return new PropertyEnricher("ServiceVersion", version);

            var requestUri = context?.Request?.Uri;
            if (requestUri != null)
                yield return new PropertyEnricher("RequestUri", requestUri?.PathAndQuery);

            var remoteIp = context?.Request?.RemoteIpAddress;
            if (remoteIp != null)
                yield return new PropertyEnricher("RemoteIp", remoteIp);

            var user = (context?.Authentication?.User?.Identity) as ClaimsIdentity;
            if (user != null)
            {
                var userId = user?.FindFirst("ntech.userid")?.Value;
                if (!string.IsNullOrWhiteSpace(userId))
                    yield return new PropertyEnricher("UserId", userId);
            }
        }

        private IEnumerable<ILogEventEnricher> GetPropertiesI(IOwinContext context)
        {
            var props = GetProperties(context);

            if (serviceName == null)
                return props;
            else
                return props.Concat(new[] { new PropertyEnricher("ServiceName", serviceName) });
        }

        public override async Task Invoke(IOwinContext context)
        {
            using (Serilog.Context.LogContext.PushProperties(GetPropertiesI(context).ToArray()))
            {
                var timer = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    await Next.Invoke(context);
                    timer.Stop();
                }
                catch (System.Threading.ThreadAbortException)
                {
                    throw;
                }
                catch (TaskCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception outside action");
                    throw;
                }
                finally
                {
                    timer.Stop();
                }
            }
        }
    }
}
