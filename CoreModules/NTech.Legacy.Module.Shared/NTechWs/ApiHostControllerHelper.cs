using Newtonsoft.Json;
using NTech.Services.Infrastructure.NTechWsDoc;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace NTech.Services.Infrastructure.NTechWs
{
    public class ApiHostControllerHelper : INTechWsUrlService
    {
        private Lazy<RotatingLogFile> requestLog;
        private Lazy<RotatingLogFile> performanceLog;
        private Lazy<Dictionary<string, NTechWebserviceMethod>> allMethods;
        private readonly bool isProduction;
        private readonly string routePrefix;
        private readonly bool isVerboseLoggingEnabled;

        public static void RegisterRoutes(string routePrefix, System.Web.Routing.RouteCollection routes)
        {
            routes.MapRoute(
                name: "ApiHost",
                url: routePrefix + "/{*path}",
                defaults: new { controller = "ApiHost", action = "Handle" });
        }

        public ApiHostControllerHelper(string currentServiceName, string logFolder, Type anyClassFromServiceProject, string routePrefix, bool isProduction, bool isVerboseLoggingEnabled, List<System.Reflection.Assembly> additionalAssembliesToScan = null)
        {
            routePrefix = routePrefix?.TrimStart('/')?.TrimEnd('/');

            var assembliesToScan = new[] { anyClassFromServiceProject.Assembly }.Concat(additionalAssembliesToScan?.ToArray() ?? new System.Reflection.Assembly[] { });

            allMethods = new Lazy<Dictionary<string, NTechWebserviceMethod>>(() =>
            {
                var methodTypes = assembliesToScan.SelectMany(x => x.GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(NTechWebserviceMethod)))).ToList();

                return methodTypes.Select(x =>
                {
                    var m = Activator.CreateInstance(x) as NTechWebserviceMethod;
                    var fullPath = m.GetFullMethodPath(routePrefix);
                    return m.IsEnabled ? Tuple.Create(fullPath, m) : null;
                }).Where(x => x != null).ToDictionary(x => x.Item1, x => x.Item2, StringComparer.OrdinalIgnoreCase);
            });
            requestLog = new Lazy<RotatingLogFile>(() => new RotatingLogFile(Path.Combine(logFolder, "ws"), $"requests-{currentServiceName}"));
            performanceLog = new Lazy<RotatingLogFile>(() => new RotatingLogFile(Path.Combine(logFolder, "ws"), $"performance-{currentServiceName}", formatLogEntry: x => $"{x}{Environment.NewLine}"));
            this.isProduction = isProduction;
            this.routePrefix = routePrefix;
            this.isVerboseLoggingEnabled = isVerboseLoggingEnabled;
        }

        public System.Web.Mvc.ActionResult ServeDocs(System.Web.Mvc.Controller controller, Func<object> getTranslations, Func<string> getTestingToken, string viewName, string crossModuleNavigationTarget, NTechServiceRegistry serviceRegistry)
        {
            string whiteListedReturnUrl;
            if (!string.IsNullOrWhiteSpace(crossModuleNavigationTarget) && NTechNavigationTarget.IsValidCrossModuleNavigationTargetCode(crossModuleNavigationTarget))
                whiteListedReturnUrl = serviceRegistry.External.ServiceUrl("nBackOffice", "Ui/CrossModuleNavigate", Tuple.Create("targetCode", crossModuleNavigationTarget)).ToString();
            else
                whiteListedReturnUrl = serviceRegistry.External.ServiceRootUri("nBackOffice").ToString();

            var methods = new List<ServiceMethodDocumentation>();
            var g = new ServiceMethodDocumentationGenerator();
            foreach (var method in allMethods.Value)
            {
                var m = method.Value;
                try
                {
                    methods.Add(g.Generate(m.Path, m.HttpVerb, m.RequestType, m.ResponseType));
                }
                catch (Exception ex)
                {
                    NLog.Error(ex, $"Failed to genereate api documentation for {method.Value.GetType().FullName}");
                }
            }
            controller.ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.GetEncoding("iso-8859-1").GetBytes(JsonConvert.SerializeObject(new
            {
                isTest = !isProduction,
                methods = methods,
                apiRootPath = $"/{routePrefix}",
                translation = getTranslations?.Invoke(),
                testingToken = isProduction ? null : getTestingToken(),
                whiteListedReturnUrl = whiteListedReturnUrl
            })));
            return new System.Web.Mvc.ViewResult
            {
                ViewName = viewName,
                MasterName = null,
                ViewData = controller.ViewData,
                TempData = controller.TempData,
                ViewEngineCollection = controller.ViewEngineCollection
            };
        }

        private string NormalizePath(string path)
        {
            var p = (path ?? "").Trim().ToLowerInvariant();
            if (p.Length < 2)
                return null;
            else if (p.EndsWith("/"))
                return p.Substring(0, p.Length - 1);
            else
                return p;
        }

        private NTechWebserviceMethod RequireMethod(string absolutePath, string httpVerb)
        {
            return FindMethodOrNull(absolutePath, httpVerb, isInvalidHttpVerb => throw new NTechWebserviceMethodException(isInvalidHttpVerb ? $"Invalid http verb for '{absolutePath}'" : $"No such method exists: '{absolutePath}'")
            {
                ErrorCode = isInvalidHttpVerb ? "invalidHttpVerb" : "noSuchMethodExists",
                IsUserFacing = true
            });
        }

        private string ToAbsolutePath(string relativeOrAbsolutePath)
        {
            if (relativeOrAbsolutePath.StartsWith("/"))
                return relativeOrAbsolutePath;
            else
                return $"/{this.routePrefix}/{relativeOrAbsolutePath?.TrimStart('/')}";
        }

        public string CreateGetUrl(string path)
        {
            var m = RequireMethod(ToAbsolutePath(path), "GET");
            return m.GetFullMethodPath(this.routePrefix);
        }

        public string CreatePostUrl(string path)
        {
            var m = RequireMethod(ToAbsolutePath(path), "POST");
            return m.GetFullMethodPath(this.routePrefix);
        }

        private NTechWebserviceMethod.ActionResult ServeRequestInternal(string normalizedPath, string correlationId, System.Web.Mvc.Controller controller, Action<INTechWebserviceCustomData> setupCustomData)
        {
            bool isInvalidHttpVerb = false;
            var method = FindMethodOrNull(normalizedPath, controller.Request.HttpMethod, onMissing: x => { isInvalidHttpVerb = x; return null; });
            if (method == null)
            {
                if (isInvalidHttpVerb)
                    return NTechWebserviceMethod.CreateErrorResponse($"Invalid http verb. Expected '{method.HttpVerb}'", errorCode: "invalidHttpVerb");
                else
                    return NTechWebserviceMethod.CreateErrorResponse("No such method exists", errorCode: "noSuchMethodExists");
            }

            var requestContext = new NTechWebserviceMethodRequestContext
            {
                CurrentUserIdentity = controller?.User?.Identity as System.Security.Claims.ClaimsIdentity,
                HttpRequest = controller.Request,
                IsExceptionLoggingDisabled = false,
                IsRequestLoggingDisabled = !isVerboseLoggingEnabled
            };

            setupCustomData(requestContext);

            return method.Execute(requestContext,
                ex => NLog.Error(ex, "Error in webservice method. {Path}, {CorrelationId}", method.Path, correlationId),
                (requestJson, responseJson) =>
                {
                    if (!isVerboseLoggingEnabled)
                        return;

                    requestLog.Value.Log($"Path={normalizedPath}{Environment.NewLine}CorrelationId={correlationId}{Environment.NewLine}{requestJson}{Environment.NewLine}{responseJson}");
                });
        }

        private NTechWebserviceMethod FindMethodOrNull(string path, string httpVerb, Func<bool, NTechWebserviceMethod> onMissing = null)
        {
            var normalizedPath = NormalizePath(path);
            if (normalizedPath == null || !allMethods.Value.ContainsKey(normalizedPath))
                return onMissing?.Invoke(false);
            var method = allMethods.Value[normalizedPath];
            if (!method.HttpVerb.Equals(httpVerb, StringComparison.OrdinalIgnoreCase))
                return onMissing?.Invoke(true);
            return method;
        }

        public System.Web.Mvc.ActionResult ServeRequest(System.Web.Mvc.Controller controller, Action<INTechWebserviceCustomData> setupCustomData)
        {
            var t = Stopwatch.StartNew();
            try
            {
                var normalizedPath = NormalizePath(controller.Request.Path);
                var correlationId = Guid.NewGuid().ToString();
                var result = ServeRequestInternal(normalizedPath, correlationId, controller, setupCustomData);

                t.Stop();
                performanceLog.Value.Log($"{normalizedPath}\t{t.ElapsedMilliseconds}\t{correlationId}"); //TODO: Hook in our standard performance logger

                if (result.IsError)
                {
                    return NTechWebserviceMethod.ToFrameworkErrorActionResult(result);
                }
                else if (result.FileStreamResult != null)
                {
                    return new System.Web.Mvc.FileStreamResult(result.FileStreamResult.Stream, result.FileStreamResult.ContentType)
                    {
                        FileDownloadName = result.FileStreamResult.DownloadFileName
                    };
                }
                else
                {
                    return new RawJsonActionResult
                    {
                        JsonData = result.JsonResult
                    };
                }
            }
            finally
            {
                t.Stop();
            }
        }

        /// <summary>
        /// Put this after any other routing so legacy apis and single strange apis can sidestep this and be directly routed.
        ///
        /// In particular make sure this is after MapMvcAttributeRoutes
        /// </summary>
        public void SetupRouting(System.Web.Routing.RouteCollection routes)
        {
            routes.MapRoute(
                name: "ApiHost",
                url: routePrefix + "/{*path}",
                defaults: new { controller = "ApiHost", action = "Handle" });
        }
    }

    public interface INTechWsUrlService
    {
        string CreateGetUrl(string path);

        string CreatePostUrl(string path);
    }
}