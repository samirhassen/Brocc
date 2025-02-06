using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PsdTwoPrototype.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PsdTwoPrototype.Controllers
{
    [Route("api/")]
    public class CalculationResultCallbackController : Controller
    {
        [HttpPost]
        [Route("CalculationResultCallback/{internalRequestId}")]
        public async Task<HttpResponseMessage> CalculationResultCallback([FromBody] Rootobject data)

        {
            string internalRequestId = (string)this.RouteData.Values["internalRequestId"];

            //TODO: error handling 
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
