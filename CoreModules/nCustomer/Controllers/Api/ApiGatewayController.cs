using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace nCustomer.Controllers
{
    [NTechApi]
    public class ApiGatewayController : NController
    {
        private ApiGatewayControllerHelper helper = new ApiGatewayControllerHelper(new Lazy<NTechServiceRegistry>(() => NEnv.ServiceRegistry));

        [Route("Api/GetUserModuleUrl")]
        [HttpPost]
        public ActionResult GetUserModuleUrl(string moduleName, string moduleLocalUrl, Dictionary<string, string> parameters = null)
        {
            return this.helper.HandleGetUserModuleUrl(moduleName, moduleLocalUrl, parameters: parameters);
        }

        [Route("Api/Gateway/{module}/{*path}")]
        [HttpPost]
        public ActionResult Post()
        {
            return this.helper.HandlePost(this, this.Request);
        }

        [Route("Ui/Gateway/{module}/{*path}")]
        [HttpGet]
        public ActionResult Get()
        {
            return this.helper.HandleGet(this, this.Request);
        }
    }
}