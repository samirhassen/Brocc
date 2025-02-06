using nCustomer.Code.Services.Kyc;
using nCustomer.DbModel;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace nCustomer.Controllers
{
    [NTechAuthorize]
    public class KycScreeningApiController : NController
    {
        /// <summary>
        /// Make sure all the customers have been screened that day
        /// </summary>
        [Route("Api/KycScreening/ListScreenBatch")]
        [HttpPost]
        public ActionResult ListScreenBatch(List<int> customerIds, DateTime screenDate, bool? isNonBatchScreen)
        {
            return Json2(this.Service.KycScreening.ListScreenBatchNew(customerIds, screenDate, isNonBatchScreen: isNonBatchScreen.GetValueOrDefault()));
        }

        /// <summary>
        /// Fetch customers with status changes that day
        /// </summary>
        [Route("Api/KycScreening/FetchCustomerStatusChanges")]
        [HttpPost]
        public ActionResult FetchCustomerStatusChanges(List<int> customerIds, DateTime screenDate)
        {
            if (customerIds == null)
                customerIds = new List<int>();

            var customerIdsWithChanges = new HashSet<int>();
            var totalScreenedCount = 0;
            foreach (var customerIdGroup in customerIds.Distinct().ToArray().SplitIntoGroupsOfN(500))
            {
                using (var context = new CustomersContext())
                {
                    var queryBase = context
                        .TrapetsQueryResults
                        .Where(x => customerIdGroup.Contains(x.CustomerId) && x.QueryDate == screenDate);
                    totalScreenedCount += queryBase.Count();
                    queryBase
                        .Select(x => new
                        {
                            CustomerId = x.CustomerId,
                            x.IsPepHit,
                            x.IsSanctionHit,
                            LastEarlierResult = context
                                .TrapetsQueryResults
                                .Where(y => y.CustomerId == x.CustomerId && y.QueryDate < x.QueryDate)
                                .OrderByDescending(y => y.Id)
                                .Select(y => new
                                {
                                    y.QueryDate,
                                    y.IsPepHit,
                                    y.IsSanctionHit
                                })
                                .FirstOrDefault()
                        })
                        .Where(x => (x.LastEarlierResult != null && (x.IsPepHit != x.LastEarlierResult.IsPepHit || x.IsSanctionHit != x.LastEarlierResult.IsSanctionHit)) || (x.LastEarlierResult == null && (x.IsPepHit || x.IsSanctionHit)))
                        .Select(x => x.CustomerId)
                        .ToList()
                        .ForEach(x => customerIdsWithChanges.Add(x));
                }
            }
            return Json(new
            {
                totalScreenedCount = totalScreenedCount,
                customerIdsWithChanges = customerIdsWithChanges
            });
        }

        /// <summary>
        /// Moved from the old KycController
        /// </summary>
        [Route("Kyc/FetchLatestKycScreenResult")]
        [HttpPost]
        public ActionResult FetchLatestKycScreenResult(int customerId)
        {
            if (NEnv.IsMortgageLoansEnabled)
                throw new Exception("Not enabled for mortage loans");
            using (var context = new CustomersContext())
            {
                var latestResult = context
                    .TrapetsQueryResults
                    .Where(x => x.CustomerId == customerId)
                    .OrderByDescending(x => x.Id)
                    .Select(x => new
                    {
                        x.QueryDate,
                        x.IsPepHit,
                        x.IsSanctionHit
                    })
                    .FirstOrDefault();

                return Json2(new
                {
                    latestResult = latestResult
                });
            }
        }

        /// <summary>
        /// Moved from the old KycController
        /// </summary>
        [Route("Kyc/IsCustomerScreened")]
        [HttpPost]
        public ActionResult IsCustomerScreened(int customerId)
        {
            if (NEnv.IsMortgageLoansEnabled)
                throw new Exception("Not enabled for mortage loans");
            var r = Service.KycScreening.IsCustomerScreened(customerId, null);
            return Json2(new { isScreened = r.Item1, latestScreenDate = r.Item2 });
        }

        [HttpPost]
        [Route("Api/KycScreening/QueryResultDetails")]
        public ActionResult GetKycQueryResultDetails(KycResultDetailsRequest request) =>
            Json2(Service.KycScreeningManagement.GetKycResultDetails(request));

        [HttpPost]
        [Route("Api/KycScreening/QueryResultHistoryDetails")]
        public ActionResult GetKycQueryResultDetailsHistory(GetKycResultDetailsHistoryRequest request) =>
            Json2(Service.KycScreeningManagement.GetKycResultDetailsHistory(request));
    }
}