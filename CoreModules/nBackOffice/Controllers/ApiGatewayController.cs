using System;
using System.Collections.Generic;
using System.Web.Mvc;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;

namespace nBackOffice.Controllers
{
    [NTechApi]
    public class ApiGatewayController : NController
    {
        private readonly ApiGatewayControllerHelper helper =
            new ApiGatewayControllerHelper(new Lazy<NTechServiceRegistry>(() => NEnv.ServiceRegistry));

        [Route("Api/GetUserModuleUrl")]
        [HttpPost]
        public ActionResult GetUserModuleUrl(string moduleName, string moduleLocalUrl,
            Dictionary<string, string> parameters = null)
        {
            return helper.HandleGetUserModuleUrl(moduleName, moduleLocalUrl, parameters: parameters);
        }

        [Route("Api/Gateway/{module}/{*path}")]
        [HttpPost]
        public ActionResult Post()
        {
            return helper.HandlePost(this, Request);
        }

        [Route("Ui/Gateway/{module}/{*path}")]
        [HttpGet]
        public ActionResult Get()
        {
            return helper.HandleGet(this, Request);
        }
    }
}