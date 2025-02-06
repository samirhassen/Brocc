using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Web.Mvc;

namespace nDataWarehouse.Controllers
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
            return ApiHost.Value.ServeDocs(this, GetTranslations, GetTestingToken, "Docs", backTarget, NEnv.ServiceRegistry);
        }

        private string GetTestingToken()
        {
            if (NEnv.IsProduction)
                return null;

            var unp = NEnv.AutomationUsernameAndPassword;
            if (unp == null)
                return null;

            return NHttp.AquireSystemUserAccessTokenWithUsernamePassword(unp.Item1, unp.Item2, NEnv.ServiceRegistry.Internal.ServiceRootUri("nUser"));
        }

        //Routed after any attribute routes in routeconfig
        public ActionResult Handle()
        {
            var h = ApiHost.Value;
            return h.ServeRequest(this, x => NTechWebserviceMethodRequestExtensions.ExtendCustomData(x));
        }
    }
}

namespace nDataWarehouse
{
    public static class NTechWebserviceMethodRequestExtensions
    {
        public static void ExtendCustomData(INTechWebserviceCustomData d)
        {

        }
    }

    public class DataWarehouseWebserviceRequestContext : NTechWebserviceMethodRequestContext
    {

    }
}