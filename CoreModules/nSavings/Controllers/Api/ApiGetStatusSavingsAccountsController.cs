using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using nSavings.DbModel;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize(ValidateAccessToken = true)]
    [RoutePrefix("Api/SavingsAccount")]
    public class ApiGetStatusSavingsAccountsController : NController
    {
        public class SavingsAccountStatusItem
        {
            public int MainCustomerId { get; set; }
            public int CreatedByBusinessEventId { get; set; }
            public string SavingsAccountNr { get; set; }
            public string AccountStatus { get; set; }
        }

        [HttpPost]
        [Route("GetStatusByCustomerIds")]
        public ActionResult GetSavingsAccountStatus(IList<int> customerIds)
        {
            if (customerIds == null || customerIds.Count == 0)
            {
                return Json2(new Dictionary<int, IList<SavingsAccountStatusItem>>());
            }

            using (var context = new SavingsContext())
            {
                var result = context
                    .SavingsAccountHeaders
                    .Where(x => customerIds.Contains(x.MainCustomerId))
                    .Select(x => new
                    {
                        x.MainCustomerId,
                        x.SavingsAccountNr,
                        x.CreatedByBusinessEventId,
                        x.Status
                    })
                    .GroupBy(x => x.MainCustomerId)
                    .ToList()
                    .ToDictionary(x => x.Key, x => x.Select(y => new SavingsAccountStatusItem
                    {
                        AccountStatus = y.Status,
                        SavingsAccountNr = y.SavingsAccountNr,
                        CreatedByBusinessEventId = y.CreatedByBusinessEventId,
                        MainCustomerId = y.MainCustomerId
                    }));
                return Json2(result);
            }
        }
    }
}