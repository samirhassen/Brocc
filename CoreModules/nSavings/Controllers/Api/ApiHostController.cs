using nSavings.Code;
using nSavings.Code.Services;
using NTech;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Web.Mvc;

namespace nSavings.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
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
                () => this.Clock,
                () => this.CurrentUserId.ToString(),
                () => this.InformationMetadata));
        }
    }
}

namespace nSavings
{
    public static class NTechWebserviceMethodRequestExtensions
    {
        public static ControllerServiceFactory Service(this NTechWebserviceMethodRequestContext source)
        {
            return source.GetCustomDataValueOrNull<ControllerServiceFactory>("Service");
        }

        public static int CurrentUserId(this NTechWebserviceMethodRequestContext source)
        {
            return int.Parse(source.GetCustomDataValueOrNull<string>("CurrentUserId"));
        }

        public static string InformationMetadata(this NTechWebserviceMethodRequestContext source)
        {
            return source.GetCustomDataValueOrNull<string>("InformationMetadata");
        }

        public static IClock Clock(this NTechWebserviceMethodRequestContext source)
        {
            return source.GetCustomDataValueOrNull<IClock>("Clock");
        }

        public static INtechCurrentUserMetadata CurrentUserMetadata(this NTechWebserviceMethodRequestContext source)
        {
            return new NtechCurrentUserMetadata(source.CurrentUserIdentity);
        }

        public static INTechCurrentUserMetadata CurrentUserMetadataCore(this NTechWebserviceMethodRequestContext source)
        {
            return new NTechCurrentUserMetadataImpl(source.CurrentUserIdentity);
        }        

        public static void ExtendCustomData(
            INTechWebserviceCustomData d,
            Func<ControllerServiceFactory> controllerServiceFactory,
            Func<IClock> clock,
            Func<string> currentUserId, Func<string> informationMetadata)
        {
            d.SetCustomData("Service", controllerServiceFactory);
            d.SetCustomData("Clock", clock);
            d.SetCustomData("CurrentUserId", currentUserId);
            d.SetCustomData("InformationMetadata", informationMetadata);
        }
    }
}