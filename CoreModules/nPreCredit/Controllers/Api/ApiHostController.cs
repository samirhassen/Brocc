using NTech;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix(RoutePrefix)]
    public class ApiHostController : NController
    {
        private static Lazy<ApiHostControllerHelper> ApiHost { get; set; } =
            new Lazy<ApiHostControllerHelper>(() =>
            {
                List<System.Reflection.Assembly> additionalAssembliesToScan = null;

                if (NEnv.EnabledPluginNames.Any())
                {
                    var plugins = DependancyInjection.Services.Resolve<NTechExternalAssemblyLoader>().LoadPlugins(NEnv.PluginSourceFolders, NEnv.EnabledPluginNames);
                    additionalAssembliesToScan = plugins.Select(x => x.Item2).ToList();
                }

                return new ApiHostControllerHelper(
                    NEnv.CurrentServiceName, NEnv.LogFolder?.FullName, typeof(ApiHostController),
                    RoutePrefix, NEnv.IsProduction, NEnv.IsVerboseLoggingEnabled,
                    additionalAssembliesToScan: additionalAssembliesToScan);
            });

        public const string RoutePrefix = "api";

        [HttpGet]
        [Route("docs")]
        public ActionResult Docs(string backTarget)
        {
            return ApiHost.Value.ServeDocs(this, GetTranslations, GetTestingToken, "Docs", backTarget, NEnv.ServiceRegistry);
        }

        private string GetTestingToken()
        {
            if (NEnv.IsProduction)
                return null;
            var unp = NEnv.ApplicationAutomationUsernameAndPassword;
            return NHttp.AquireSystemUserAccessTokenWithUsernamePassword(unp.Item1, unp.Item2, NEnv.ServiceRegistry.Internal.ServiceRootUri("nUser"));
        }

        //Routed after any attribute routes in routeconfig
        public ActionResult Handle()
        {
            var h = ApiHost.Value;
            return h.ServeRequest(this, x => NTechWebserviceMethodRequestExtensions.ExtendCustomData(x,
                DependancyInjection.Services));
        }
    }
}

namespace nPreCredit
{
    public static class NTechWebserviceMethodRequestExtensions
    {
        public static INTechCurrentUserMetadata CurrentUserMetadata(this NTechWebserviceMethodRequestContext source)
        {
            return source.GetCustomDataValueOrNull<INTechCurrentUserMetadata>("CurrentUserMetadata");
        }

        public static IClock Clock(this NTechWebserviceMethodRequestContext source)
        {
            return source.GetCustomDataValueOrNull<IClock>("Clock");
        }

        public static IDependencyResolver Resolver(this NTechWebserviceMethodRequestContext source)
        {
            return source.GetCustomDataValueOrNull<IDependencyResolver>("Resolver");
        }

        public static void ExtendCustomData(INTechWebserviceCustomData d, IDependencyResolver resolver)
        {
            d.SetCustomData("CurrentUserMetadata", () => resolver.Resolve<INTechCurrentUserMetadata>());
            d.SetCustomData("Clock", () => resolver.Resolve<IClock>());
            d.SetCustomData("Resolver", () => resolver);
        }
    }
}