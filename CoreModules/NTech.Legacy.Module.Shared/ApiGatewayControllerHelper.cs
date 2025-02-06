using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace NTech.Services.Infrastructure.NTechWs
{
    public class ApiGatewayControllerHelper
    {
        private readonly Lazy<NTechServiceRegistry> serviceRegistry;

        public ApiGatewayControllerHelper(Lazy<NTechServiceRegistry> serviceRegistry)
        {
            this.serviceRegistry = serviceRegistry;
        }

        public ActionResult HandleGetUserModuleUrl(string moduleName, string moduleLocalUrl, Dictionary<string, string> parameters = null)
        {
            if (parameters != null)
            {
                parameters.Remove("action");
                parameters.Remove("controller");
            }
            var querystringParameters = parameters?.Select(x => Tuple.Create(x.Key, x.Value))?.ToArray() ?? new Tuple<string, string>[] { };

            Func<NTechServiceRegistry.AccessPath, string> create = p =>
                p.ServiceUrl(moduleName, moduleLocalUrl, querystringParameters).ToString();

            return new JsonNetActionResult
            {
                Data = new
                {
                    Url = create(serviceRegistry.Value.Internal),
                    UrlInternal = create(serviceRegistry.Value.Internal),
                    UrlExternal = create(serviceRegistry.Value.External)
                }
            };
        }

        public ActionResult HandleGet(Controller controller, HttpRequestBase request)
        {
            var accessToken = NHttp.GetCurrentAccessToken();

            if (string.IsNullOrWhiteSpace(accessToken))
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Access token missing");

            var moduleName = controller.RouteData.Values["module"] as string;
            var path = controller.RouteData.Values["path"] as string;

            var s = serviceRegistry.Value;
            if (!s.ContainsService(moduleName))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"No such module: '{moduleName}");

            var ps = new List<Tuple<string, string>>(request.QueryString.AllKeys.Length + 1);
            ps.AddRange(request.QueryString.AllKeys.Select(x => Tuple.Create(x, request.QueryString[x])));
            if(!request.QueryString.AllKeys.Contains("apiureq")) 
                ps.Add(Tuple.Create("apiureq", "1")); //Se NTech.Services.Infrastructure.LoginSetupSupport for why this exists

            var url = s.External.ServiceUrl(moduleName, path, ps.ToArray()).ToString();
            return (new RedirectResult(url));
        }

        public ActionResult HandlePost(Controller controller, HttpRequestBase request)
        {
            var accessToken = NHttp.GetCurrentAccessToken();

            if (string.IsNullOrWhiteSpace(accessToken))
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Access token missing");

            var moduleName = controller.RouteData.Values["module"] as string;
            var path = controller.RouteData.Values["path"] as string;

            var s = serviceRegistry.Value;
            if (!s.ContainsService(moduleName))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"No such module: '{moduleName}");

            if (!request.ContentType.Contains("application/json"))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid content type. Must be application/json");

            request.InputStream.Position = 0;
            using (var r = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                var requestString = r.ReadToEnd();
                var p = NHttp
                    .Begin(s.Internal.ServiceRootUri(moduleName), accessToken, TimeSpan.FromMinutes(5))
                    .PostJsonRaw(path, requestString);

                if (p.IsSuccessStatusCode)
                {
                    if(p.ContentType == "application/xml")
                    {
                        var ms = new MemoryStream();
                        p.CopyToStream(ms);
                        ms.Position = 0;
                        return new FileStreamResult(ms, "application/xml");
                    }
                    else
                        return new RawJsonActionResult
                        {
                            JsonData = p.ParseAsRawJson()
                        };
                }
                else if(p.IsApiError)
                {
                    var apiError = p.ParseApiError();
                    return new RawJsonActionResult
                    {
                        CustomHttpStatusCode = p.StatusCode,
                        CustomStatusDescription = apiError.ErrorCode,
                        JsonData = JsonConvert.SerializeObject(new
                        {
                            errorCode = apiError.ErrorCode,
                            errorMessage = apiError.ErrorMessage
                        })
                    };
                }
                else
                    return new HttpStatusCodeResult(p.StatusCode, p.ReasonPhrase);
            }
        }
    }
}