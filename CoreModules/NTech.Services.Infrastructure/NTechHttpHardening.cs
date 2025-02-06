using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace NTech.Services.Infrastructure
{
    public static class NTechHttpHardening
    {
        /// <summary>
        /// Usage:
        /// Go into Global.asax
        /// There change this:
        ///            public class Global : HttpApplication
        ///            {
        ///                private void Application_Start(object sender, EventArgs e)
        ///                {
        ///                   //Lots of code
        ///                }
        ///                // lots of code
        ///           }
        /// To this:
        ///            public class Global : HttpApplication
        ///            {
        ///                public override void Init()
        ///                {
        ///                    base.Init();
        ///                    NTechHttpHardening.HandleCachingAndInformationLeakHeader(this, false);
        ///                }
        ///                private void Application_Start(object sender, EventArgs e)
        ///                {
        ///                   //Lots of code
        ///                }
        ///                // lots of code
        ///            }
        /// The header X-Powered-By cannot be removed this way since it's added by iis. To remove that instead add a section like this to web.cofing:
        ///   <system.webServer>
        ///     <httpProtocol>
        ///       <customHeaders>
        ///         <remove name = "X-Powered-By" />
        ///      </ customHeaders >
        ///    </httpProtocol>
        ///
        /// The Server header cannot be removed from static content this way so make sure this is on for all modules:
        /// <modules runAllManagedModulesForAllRequests="true" />
        /// It should be anyway for other reasons
        public static void HandleCachingAndInformationLeakHeader(HttpApplication app, bool isPublicModule)
        {
            app.PreSendRequestHeaders += (object sender, EventArgs e) =>
            {
                try
                {
                    var response = app.Response;

                    void SetHeader(string x, string y)
                    {
                        response.Headers.Remove(x);
                        response.Headers.Add(x, y);
                    }
                    
                    // Explicitly remove these Response Headers for security purposes. 
                    response.Headers.Remove("Server");
                    response.Headers.Remove("X-AspNetMvc-Version");
                    response.Headers.Remove("X-AspNet-Version");

                    // Explicitly change the values of/set these Response Headers. 
                    SetHeader("X-Content-Type-Options", "nosniff");
                    SetHeader("X-XSS-Protection", "1; mode=block");
                    // Notice: Using NWebSec, we also set Content-Security-Policy in the response headers. 

                    if (!System.Web.Mvc.MvcHandler.DisableMvcResponseHeader)
                    {   //if here just to guard a bit in case the creators of mvc put expensive init code in the setter
                        System.Web.Mvc.MvcHandler.DisableMvcResponseHeader = true;
                    }
                    if (isPublicModule)
                    {
                        response.Headers.Remove("X-Ntech-Api-Error");
                    }

                    if (ContainsOneOfIgnoreCase(response.ContentType, "text/html", "application/json"))
                    {
                        var allowIFrame = HttpContext.Current != null && (HttpContext.Current.Items["NTechAllowIFrame"] as string) == "true";
                        if (!allowIFrame)
                        {
                            SetHeader("X-Frame-Options", "DENY");
                        }
                        response.Cache.SetCacheability(HttpCacheability.NoCache);
                        return;
                    }
                }
                catch
                {
                }
            };

            if(!isPublicModule)
                SetupCrossServiceCommunication(app);
        }

        private static void SetupCrossServiceCommunication(HttpApplication application)
        {
            var serviceRegistry =  NTechEnvironment.Instance.ServiceRegistry;
            var corsDomains = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            serviceRegistry.External.Values.ToList().ForEach(x =>
            {
                corsDomains.Add(new Uri(x).GetLeftPart(UriPartial.Authority));
            });

            application.BeginRequest += (sender, e) => 
            {
                //NOTE: This is not action attribute since chrome insists on sending a preflight (OPTIONS) request
                //      which never hits the filter since we have [HttpPost] decorations everywhere
                var context = HttpContext.Current;

                var incomingHost = context?.Request?.UrlReferrer?.GetLeftPart(UriPartial.Authority);
                if(incomingHost != null && corsDomains.Contains(incomingHost))
                {
                    context.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                    context.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization, X-NTech-Preserve-Case, X-NTech-Force-CamelCase");
                    context.Response.AddHeader("Access-Control-Allow-Origin", incomingHost);                    
                    if(context.Request.RequestType == "OPTIONS")
                        context.Response.End();
                }
            };
        }

        public static void HandleGlobal_Asax_ApplicationError(HttpServerUtility server)
        {
            var ex = server?.GetLastError();
            if (ex != null && !(ex is HttpException))
            {
                try { Serilog.NLog.Error(ex, "Unhandled exception"); } catch { /* Ignored */ }
            }
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

        /// <summary>
        /// Use by calling
        ///  AllowIFrameEmbeddingForThisContext(this.HttpContext)
        /// In any action method where the resulting view intends to embed iframes
        /// </summary>
        public static void AllowIFrameEmbeddingForThisContext(HttpContextBase context)
        {
            context.Items["NTechAllowIFrame"] = "true";
        }
    }

    
    //Marker attribute to allow forms binding when needed
    //Just decorate a controller or action with this to allow forms encoding in that context
    public class NTechHttpHardeningAllowFrameAttribute : Attribute
    {
        public static bool IsPresent(System.Web.Mvc.ControllerContext controllerContext)
        {
            if (controllerContext == null || controllerContext.Controller == null)
                return false;

            if (controllerContext.Controller.GetType().GetCustomAttributes(typeof(NTechHttpHardeningAllowFrameAttribute), true).Length > 0)
                return true;

            if (controllerContext.RouteData == null)
                return false;

            string action = (string)controllerContext.RouteData.Values["action"];
            if (!string.IsNullOrEmpty(action) && controllerContext.Controller.GetType().GetMethod(action).GetCustomAttributes(typeof(NTechHttpHardeningAllowFrameAttribute), true).Length > 0)
                return true;

            return false;
        }
    }
};