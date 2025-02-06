using nPreCredit.DbModel;
using nPreCredit.DbModel.Repository;
using NTech.Services.Infrastructure;
using System.Linq;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    [NTechAuthorizeCreditHigh]
    public class ScheduledTasksController : NController
    {
        [Route("ScheduledTasks/UpdateDatawarehouse")]
        public ActionResult UpdateDatawarehouse()
        {
            using (var context = new PreCreditContext())
            {
                var repo = new SystemItemRepository(this.CurrentUserId, this.InformationMetadata, this.Clock);

                var latestChangeDate = new[]
                        {
                            SystemItemCode.DwLatestMergedTimestamp_Dimension_CreditApplication,
                            SystemItemCode.DwLatestMergedTimestamp_Fact_CreditApplicationSnapshot
                        }
                    .Select(x => repo.GetLatestChangeDate(x, context))
                    .Where(x => x.HasValue)
                    .OrderByDescending(x => x.Value)
                    .FirstOrDefault();

                SetInitialData(new
                {
                    latestChangeDate = latestChangeDate,
                    updateUrl = Url.Action("UpdateDataWarehouse", "ApiUpdateDataWarehouse")
                });

                return View();
            }
        }
    }
}