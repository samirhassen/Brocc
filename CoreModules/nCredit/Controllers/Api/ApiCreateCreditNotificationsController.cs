using nCredit.DomainModel;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    [NTechAuthorizeCreditHigh(ValidateAccessToken = true)]
    public class ApiCreateCreditNotificationsController : NController
    {
        private ActionResult CreateNotificationsI(bool skipDeliveryExport, bool skipNotify, bool useDelayedDocuments)
        {
            try
            {
                var notificationService = Service.GetNotificationService(useDelayedDocuments);
                var result = notificationService.CreateNotifications(skipDeliveryExport, skipNotify);
                return Json2(new { successCount = result.SuccessCount, failCount = result.FailCount, errors = result.Errors, totalMilliseconds = result.TotalMilliseconds, warnings = result.Warnings });
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "CreditNotification crashed");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [HttpPost]
        [Route("Api/Credit/CreateNotifications")]
        public ActionResult CreateNotifications(IDictionary<string, string> schedulerData = null)
        {
            Func<string, string> getSchedulerData = s => (schedulerData != null && schedulerData.ContainsKey(s)) ? schedulerData[s] : null;
            var skipDeliveryExport = getSchedulerData("skipDeliveryExport") == "true";
            var useDelayedDocuments = getSchedulerData("useDelayedDocuments") == "true";

            var c = Service.DocumentClientHttpContext;
            return CreditContext.RunWithExclusiveLock("ntech.scheduledjobs.createnotifications",
                    () => CreateNotificationsI(skipDeliveryExport, false, useDelayedDocuments),
                    () => new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Job is already running")
            );
        }

        [HttpPost]
        [Route("Api/Credit/GetNotificationFilesPage")]
        public ActionResult GetNotificationFilesPage(int pageSize, int pageNr = 0)
        {
            using (var context = new CreditContext())
            {
                var baseResult = context
                    .OutgoingCreditNotificationDeliveryFileHeaders;

                var totalCount = baseResult.Count();
                var currentPage = baseResult
                    .OrderByDescending(x => x.Timestamp)
                    .Skip(pageSize * pageNr)
                    .Take(pageSize)
                    .Select(x => new
                    {
                        x.TransactionDate,
                        x.FileArchiveKey,
                        NotificationCount = x.Notifications.Count
                            //Co-notification slaves
                            + context.CreditNotificationHeaders.Count(y =>
                                !y.OutgoingCreditNotificationDeliveryFileHeaderId.HasValue
                                && y.IsCoNotificationMaster == false
                                && x.Notifications.Any(z => z.CoNotificationId == y.CoNotificationId)),
                        UserId = x.ChangedById,
                    })
                    .ToList()
                    .Select(x => new
                    {
                        x.TransactionDate,
                        x.NotificationCount,
                        x.UserId,
                        UserDisplayName = GetUserDisplayNameByUserId(x.UserId.ToString()),
                        x.FileArchiveKey,
                        ArchiveDocumentUrl = Url.Action("ArchiveDocument", "ApiArchiveDocument", new { key = x.FileArchiveKey, setFileDownloadName = true }),
                    })
                    .ToList();

                var nrOfPages = (totalCount / pageSize) + (totalCount % pageSize == 0 ? 0 : 1);

                return Json2(new
                {
                    CurrentPageNr = pageNr,
                    TotalNrOfPages = nrOfPages,
                    Page = currentPage.ToList()
                });
            }
        }
    }
}