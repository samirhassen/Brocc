using nPreCredit.Code;
using NTech.Services.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api/CustomerInfo")]
    public class CustomerInfoController : NController
    {
        [HttpPost]
        [Route("FetchItems")]
        public ActionResult FetchItems(int customerId, IList<string> itemNames)
        {
            var cc = new PreCreditCustomerClient();
            var items = cc.GetCustomerCardItems(customerId, itemNames?.ToArray())?.Select(x => new
            {
                name = x.Key,
                value = x.Value
            })?.ToList();
            return Json2(items);
        }

        [HttpPost]
        [Route("FetchItemsBulk")]
        public ActionResult BulkFetchItems(List<int> customerIds, IList<string> itemNames)
        {
            var cc = new PreCreditCustomerClient();
            var items = cc.BulkFetchPropertiesByCustomerIdsD(customerIds?.ToHashSet(), itemNames?.ToArray());
            return Json2(items);
        }

        [HttpPost]
        [Route("FetchCustomerIdByCivicRegNr")]
        public ActionResult FetchCustomerIdByCivicRegNr(string civicRegNr)
        {
            var cc = new PreCreditCustomerClient();
            if (!NEnv.BaseCivicRegNumberParser.TryParse(civicRegNr, out var p))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid civicRegNr");
            return Json2(cc.GetCustomerId(p));
        }

        [HttpPost]
        [Route("FetchCustomerIdByOrgnr")]
        public ActionResult FetchCustomerIdByOrgnr(string orgnr)
        {
            var cc = new PreCreditCustomerClient();
            if (!NEnv.BaseOrganisationNumberParser.TryParse(orgnr, out var p))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid orgnr");
            return Json2(cc.GetCustomerId(p));
        }
    }
}