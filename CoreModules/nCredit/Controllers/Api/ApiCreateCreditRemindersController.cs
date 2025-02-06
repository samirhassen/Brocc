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
    public class ApiCreateCreditRemindersController : NController
    {
        private ActionResult CreateRemindersI(bool createReminders, bool createDeliverFile, bool useDelayedDocuments, CreditType creditType)
        {
            try
            {
                var r = Service.CreateReminderService(useDelayedDocuments).CreateReminders(createReminders, createDeliverFile, creditType);

                return Json2(new { errors = r.Errors, totalMilliseconds = r.TotalMilliseconds, warnings = r.Warnings });
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "CreditReminders crashed");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [HttpPost]
        [Route("Api/Credit/CreditRemindersStatus")]
        public ActionResult GetReminderJobsStatus()
        {
            using (var context = Service.ContextFactory.CreateContext())
            {
                var m = Service.CreateNewCreditRemindersBusinessEventManager(false);
                return Json2(m.GetStatus(context, NEnv.IsMortgageLoansEnabled ? CreditType.MortgageLoan : CreditType.UnsecuredLoan));
            }
        }

        [HttpPost]
        [Route("Api/Credit/CreateReminders")]
        public ActionResult CreateReminders(bool? onlyCreateDeliveryFile, bool? onlyCreateReminders, bool? skipRunOrderCheck, IDictionary<string, string> schedulerData, bool? skipRecentRemindersCheck)
        {
            if (NEnv.IsMortgageLoansEnabled)
                throw new Exception("Not allowed for mortgage loans. Use MortgageLoan/Remind instead.");

            Func<string, string> getSchedulerData = s => (schedulerData != null && schedulerData.ContainsKey(s)) ? schedulerData[s] : null;
            bool createReminders;
            bool createDeliveryFile;

            if (onlyCreateReminders.HasValue && onlyCreateDeliveryFile.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "onlyCreateReminders and onlyCreateDeliveryFile cannot be combined");
            }
            else if (onlyCreateDeliveryFile.HasValue && onlyCreateDeliveryFile.Value == true)
            {
                createReminders = false;
                createDeliveryFile = true;
            }
            else if (onlyCreateReminders.HasValue && onlyCreateReminders.Value == true)
            {
                createReminders = true;
                createDeliveryFile = false;
            }
            else
            {
                var skipDeliveryExport = getSchedulerData("skipDeliveryExport") == "true";
                createReminders = true;
                createDeliveryFile = !skipDeliveryExport;
            }

            var creditType = NEnv.IsMortgageLoansEnabled ? CreditType.MortgageLoan : CreditType.UnsecuredLoan;
            var useDelayedDocuments = getSchedulerData("useDelayedDocuments") == "true";

            var c = Service.DocumentClientHttpContext;
            return CreditContext.RunWithExclusiveLock("ntech.scheduledjobs.createreminders",
                    () => CreateRemindersI(createReminders, createDeliveryFile, useDelayedDocuments, creditType),
                    () => Json2(new { errors = new[] { "Job is already running" } })
            );
        }

        private object GetRemindersPageInitialData()
        {
            CreditType creditType;
            string createFileUrl;

            if (NEnv.IsMortgageLoansEnabled)
            {
                creditType = CreditType.MortgageLoan;
                createFileUrl = Service.WsUrl.CreatePostUrl("MortgageLoans/Remind");
            }
            else
            {
                if (NEnv.IsCompanyLoansEnabled && NEnv.IsUnsecuredLoansEnabled)
                    throw new Exception("Reminders dont support systems with both company and unsecured loans");

                if (NEnv.IsCompanyLoansEnabled)
                {
                    creditType = CreditType.CompanyLoan;
                    createFileUrl = Service.WsUrl.CreatePostUrl("CompanyCredit/Remind");
                }
                else
                {
                    creditType = CreditType.UnsecuredLoan;
                    createFileUrl = Url.Action("CreateReminders", "ApiCreateCreditReminders");
                }
            }

            var notificationProcessSettings = NEnv.NotificationProcessSettings.GetByCreditType(creditType);

            using (var context = CreateCreditContext())
            {
                var m = Service.CreateNewCreditRemindersBusinessEventManager(false);
                return new
                {
                    status = m.GetStatus(context, creditType),
                    createFileUrl = createFileUrl,
                    getFilesPageUrl = Url.Action("GetFilesPage", "ApiCreateCreditReminders"),
                    hasPerLoanDueDay = NEnv.HasPerLoanDueDay,
                    notificationProcessSettings
                };
            }
        }

        [HttpPost]
        [Route("Api/Credit/GetReminderFilesPage")]
        public ActionResult GetFilesPage(int pageSize, int pageNr = 0, bool includeInitialData = false)
        {
            var initialData = includeInitialData ? GetRemindersPageInitialData() : null;
            using (var context = new CreditContext())
            {
                var baseResult = context
                    .OutgoingCreditReminderDeliveryFileHeaders;

                var totalCount = baseResult.Count();
                var currentPage = baseResult
                    .OrderByDescending(x => x.Timestamp)
                    .Skip(pageSize * pageNr)
                    .Take(pageSize)
                    .ToList()
                    .Select(x => new
                    {
                        x.TransactionDate,
                        x.FileArchiveKey,
                        ReminderCount = x.Reminders.Count,
                        UserId = x.ChangedById,
                    })
                    .ToList()
                    .Select(x => new
                    {
                        x.TransactionDate,
                        x.ReminderCount,
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
                    Page = currentPage.ToList(),
                    InitialData = initialData
                });
            }
        }
    }
}