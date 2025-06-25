using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Core.Enrichers;

namespace NTech.Services.Infrastructure
{
    public class NTechLoggingMiddleware : OwinMiddleware
    {
        private readonly string serviceName;

        public NTechLoggingMiddleware(OwinMiddleware next, string serviceName) : base(next)
        {
            this.serviceName = serviceName;
        }

        public static IEnumerable<ILogEventEnricher> GetProperties(IOwinContext context)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            if (version != null)
                yield return new PropertyEnricher("ServiceVersion", version);

            var requestUri = context?.Request?.Uri;
            if (requestUri != null)
                yield return new PropertyEnricher("RequestUri", requestUri?.PathAndQuery);

            var remoteIp = context?.Request?.RemoteIpAddress;
            if (remoteIp != null)
                yield return new PropertyEnricher("RemoteIp", remoteIp);

            if (!(context?.Authentication?.User?.Identity is ClaimsIdentity user)) yield break;

            var userId = user.FindFirst("ntech.userid")?.Value;
            if (!string.IsNullOrWhiteSpace(userId))
                yield return new PropertyEnricher("UserId", userId);
        }

        private IEnumerable<ILogEventEnricher> GetPropertiesI(IOwinContext context)
        {
            var props = GetProperties(context);

            return serviceName == null
                ? props
                : props.Concat(new[] { new PropertyEnricher("ServiceName", serviceName) });
        }

        public override async Task Invoke(IOwinContext context)
        {
            using (LogContext.PushProperties(GetPropertiesI(context).ToArray()))
            {
                var timer = Stopwatch.StartNew();
                try
                {
                    await Next.Invoke(context);
                    timer.Stop();
                }
                catch (ThreadAbortException)
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