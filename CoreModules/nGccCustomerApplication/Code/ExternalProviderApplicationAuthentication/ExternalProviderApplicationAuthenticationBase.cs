using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace nGccCustomerApplication.Code
{
    public abstract class ExternalProviderApplicationAuthenticationBase : ActionFilterAttribute
    {
        public ExternalProviderApplicationAuthenticationBase()
        {

        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                var callerIpAddress = filterContext.HttpContext?.GetOwinContext()?.Request?.RemoteIpAddress;
                if (NEnv.ProviderApplicationLogFolder != null)
                {
                    System.IO.Directory.CreateDirectory(NEnv.ProviderApplicationLogFolder);
                    var r = filterContext.RequestContext?.HttpContext?.Request;
                    if(r != null)
                    {
                        var key = Guid.NewGuid().ToString();
                        using (var ms = new MemoryStream())
                        {
                            var posBefore = r.InputStream.Position;
                            r.InputStream.Position = 0;
                            r.InputStream.CopyTo(ms);
                            r.InputStream.Position = posBefore;              
                            File.WriteAllBytes(System.IO.Path.Combine(NEnv.ProviderApplicationLogFolder, key + "-request-body.txt"), ms.ToArray());
                        }
                        var headersText = "";

                        foreach (var h in r.Headers.AllKeys)
                            headersText += h + "=" + r.Headers[h] + Environment.NewLine;
                        if(callerIpAddress != null)
                        {
                            headersText += $"<caller ip: {callerIpAddress}>" + Environment.NewLine;
                        }

                        System.IO.File.WriteAllText(System.IO.Path.Combine(NEnv.ProviderApplicationLogFolder, key + "-request-headers.txt"), headersText);
                    }
                }
                AuthResult authResult = null;

                var req = filterContext.HttpContext.Request;
                var authKey = req.Headers.AllKeys.Where(x => x.EqualsIgnoreCase("Authorization")).FirstOrDefault();
                if(authKey != null)
                {
                    var auth = req.Headers[authKey];
                    
                    if (!string.IsNullOrWhiteSpace(auth))
                    {
                        authResult = Authenticate(filterContext, auth.Trim(), callerIpAddress);
                    }
                }

                if (authResult == null)
                {                    
                    filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }
                else
                {
                    filterContext.HttpContext.Items["NTechAuthResult"] = authResult;
                }                    
            }
            catch (Exception ex)
            {
                NLog.Error(ex, $"Error in {ErrorName}");
                filterContext.HttpContext.Response.Clear();
                filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
                filterContext.HttpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                filterContext.Result = new HttpStatusCodeResult(System.Net.HttpStatusCode.InternalServerError, "Server error during login");
            }
        }
        protected abstract AuthResult Authenticate(ActionExecutingContext filterContext, string authHeaderValue, string callerIpAddress);

        protected abstract string ErrorName { get; }

        public class AuthResult
        {
            public string ProviderName { get; set; }
            public bool UsedApiKey { get; set; }
            public string UserName { get; set; }
            public string ApiKeyId { get; set; }
            public string CallerIpAddress { get; set; }
            public string PreCreditBearerToken { get; set; }
        }

        public static AuthResult RequireAuthResult(HttpContextBase httpContext)
        {
            var model = httpContext.Items["NTechAuthResult"] as AuthResult;
            if (model == null)
                throw new Exception("Missing auth result");
            return model;
        }
    }
}