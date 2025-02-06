using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace NTechSignicat.Shared
{
    public static class NTechHttpHardening
    {
        /// <summary>
        /// This needs to be added to Startup.Configure
        ///
        /// In addition a call to .ConfigureKestrel(x => x.AddServerHeader = false) is needed in Program.CreateWebHostBuilder
        /// since the Server header is ninjad in by kestrel after the entire pipeline has run owtherwise.
        ///
        /// Also the X-Powered-By header needs to be removed by adding a web.Release.config file to the project root with this content:
        /// <?xml version="1.0" encoding="utf-8"?>
        /// <configuration xmlns:xdt="http://schemas.microsoft.com/XML-Document-Transform">
        ///  <location>
        ///
        ///    <!-- To customize the asp.net core module uncomment and edit the following section.
        ///    For more info see https://go.microsoft.com/fwlink/?linkid=838655 -->
        ///    <system.webServer>
        ///      <security xdt:Transform="InsertIfMissing">
        ///        <requestFiltering removeServerHeader="true" />
        ///      </security>
        ///      <httpProtocol xdt:Transform="InsertIfMissing">
        ///        <customHeaders>
        ///          <remove name="X-Powered-By" />
        ///        </customHeaders>
        ///      </httpProtocol>
        ///    </system.webServer>
        ///  </location>
        ///</configuration>
        ///
        /// </summary>
        /// <param name="app"></param>
        /// <param name="isPublicModule"></param>
        public static void HandleCachingAndInformationLeakHeader(IApplicationBuilder app, bool isPublicModule)
        {
            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    try
                    {
                        var response = context.Response;

                        void SetHeader(string x, string y)
                        {
                            response.Headers.Remove(x);
                            response.Headers.Add(x, y);
                        }

                        SetHeader("X-Content-Type-Options", "nosniff");
                        SetHeader("X-XSS-Protection", "1; mode=block");
                        response.Headers.Remove("X-Powered-By");
                        if (ContainsOneOfIgnoreCase(response.ContentType, "text/html", "application/json"))
                        {
                            SetHeader("X-Frame-Options", "DENY");
                            SetHeader("Cache-Control", "no-cache");
                            response.Headers.Add("Pragma", "no-cache");
                        }
                    }
                    catch
                    {
                    }
                    return Task.FromResult(0);
                });
                await next();
            });
        }

        private static bool ContainsOneOfIgnoreCase(this string source, params string[] args)
        {
            if (source == null)
                return false;
            var s = source.ToLowerInvariant();
            foreach (var a in args)
            {
                if (s.Contains(a.ToLowerInvariant()))
                    return true;
            }
            return false;
        }
    }
}