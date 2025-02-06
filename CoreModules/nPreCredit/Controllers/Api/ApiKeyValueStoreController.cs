using nPreCredit.Code.Services;
using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api/KeyValueStore")]
    public class ApiKeyValueStoreController : NController
    {
        [Route("Set")]
        [HttpPost]
        public ActionResult SetValue(string key, string keySpace, string value)
        {
            bool wasUpdated = false;
            this.Service.Resolve<IKeyValueStoreService>().SetValue(key, keySpace, value, observeWasUpdated: x => wasUpdated = x);
            return Json2(new { wasUpdated });
        }

        [Route("Get")]
        [HttpPost]
        public ActionResult GetValue(string key, string keySpace)
        {
            var value = this.Service.Resolve<IKeyValueStoreService>().GetValue(key, keySpace);
            return Json2(new { value });
        }

        [Route("Remove")]
        [HttpPost]
        public ActionResult RemoveValue(string key, string keySpace)
        {
            bool wasRemoved = false;
            this.Service.Resolve<IKeyValueStoreService>().RemoveValue(key, keySpace, observeWasRemoved: x => wasRemoved = x);
            return Json2(new { wasRemoved });
        }
    }
}