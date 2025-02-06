using nCredit.Code.Services;
using NTech;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Web.Mvc;

namespace nCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize()]
    [RoutePrefix(RoutePrefix)]
    public class ApiHostController : NController
    {
        public static Lazy<ApiHostControllerHelper> ApiHost { get; private set; } =
            new Lazy<ApiHostControllerHelper>(() => new ApiHostControllerHelper(
                NEnv.CurrentServiceName, NEnv.LogFolder?.FullName, typeof(ApiHostController), RoutePrefix, NEnv.IsProduction, NEnv.IsVerboseLoggingEnabled));

        private const string RoutePrefix = "api";

        [HttpGet]
        [Route("docs")]
        public ActionResult Docs(string backTarget)
        {
            return ApiHost.Value.ServeDocs(this, () => null, GetTestingToken, "Docs", backTarget, NEnv.ServiceRegistry);
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
                () => this.Service,
                () => this.GetCurrentUserMetadata(),
                () => this.Clock));
        }
    }
}

namespace nCredit
{
    public static class NTechWebserviceMethodRequestExtensions
    {
        public static ControllerServiceFactory Service(this NTechWebserviceMethodRequestContext source)
        {
            return source.GetCustomDataValueOrNull<ControllerServiceFactory>("Service");
        }

        public static INTechCurrentUserMetadata CurrentUserMetadata(this NTechWebserviceMethodRequestContext source)
        {
            return source.GetCustomDataValueOrNull<INTechCurrentUserMetadata>("CurrentUserMetadata");
        }

        public static IClock Clock(this NTechWebserviceMethodRequestContext source)
        {
            return source.GetCustomDataValueOrNull<IClock>("Clock");
        }

        public static void ExtendCustomData(INTechWebserviceCustomData d, Func<ControllerServiceFactory> controllerServiceFactory, Func<INTechCurrentUserMetadata> currentUserMetadata, Func<IClock> clock)
        {
            d.SetCustomData("Service", controllerServiceFactory);
            d.SetCustomData("CurrentUserMetadata", currentUserMetadata);
            d.SetCustomData("Clock", clock);
        }
    }

    public class CreditWebserviceRequestContext : NTechWebserviceMethodRequestContext
    {
        public ControllerServiceFactory Service { get; set; }
        public INTechCurrentUserMetadata CurrentUserMetadata { get; set; }
        public IClock Clock { get; set; }
    }
}