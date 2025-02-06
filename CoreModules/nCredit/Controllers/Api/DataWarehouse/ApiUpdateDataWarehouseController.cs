using NTech.Services.Infrastructure;
using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers.Api.DataWarehouse
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("Api")]
    public partial class ApiUpdateDataWarehouseController : NController
    {
        [Route("DataWarehouse/Update")]
        [HttpPost]
        public ActionResult UpdateDataWarehouse()
        {
            var currentUser = GetCurrentUserMetadata();
            var clock = Clock;
            var tasksLocal = tasks.Value;
            return CreditContext.RunWithExclusiveLock("ntech.ncredit.updatedatawarehouse", () =>
            {
                foreach (var t in tasksLocal.Where(x => x.IsEnabled))
                {
                    t.Merge(currentUser, clock);
                }

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }, () => { return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "The job was already running"); });
        }

        private readonly Lazy<DatawarehouseMergeTask[]> tasks = new Lazy<DatawarehouseMergeTask[]>(() => new DatawarehouseMergeTask[]
        {
                    //BEWARE: These are ordered since data can be interdependent. Dont reorder these without testing the effects.
                    new MergeCreditNotificationBalanceSnapshotTask(),
                    new MergeDimensionCreditTask(),
                    new MergeCreditNotificationStateTask(),
                    new MergeCreditCapitalBalanceEventTask(),
                    new MergeInitialEffectiveInterestRateTask(),
                    new MergeCreditOutgoingPaymentTask(),
                    new MergeCreditSnapshotTask(),
                    new MergeQuarterlyRatiBasisTask(),
                    new MergeQuarterlyRatiBusinessEventsTask(),
                    new MergeMonthlyLiquidityExposureTask()
        });
    }
}