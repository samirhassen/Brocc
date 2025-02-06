using nCredit.DbModel.BusinessEvents;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class ApiCreditNotificationsController : NController
    {
        public class SearchCreditRequest
        {
            public string CivicRegNr { get; set; }
            public string CreditNr { get; set; }
        }


        [HttpPost]
        [Route("Api/Credit/Notifications")]
        public ActionResult CreditNotifications(string creditNr)
        {
            if (string.IsNullOrWhiteSpace(creditNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing creditNr");
            using (var context = CreateCreditContext())
            {
                var date = Clock.Today;

                var credit = context
                    .CreditHeaders
                    .Where(x => x.CreditNr == creditNr)
                    .Select(x => new
                    {
                        LatestActiveProcessSuspendingTerminationLetter = x
                            .TerminationLetters
                            .Where(y => y.SuspendsCreditProcess == true && y.InactivatedByBusinessEventId == null)
                            .OrderByDescending(y => y.Id)
                            .Select(y => new
                            {
                                DueDate = (DateTime?)y.DueDate,
                                y.CoTerminationId,
                                y.IsCoTerminationMaster,
                                y.Id
                            })
                            .FirstOrDefault()

                    }).SingleOrDefault();

                if (credit == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such credit");

                var modelsbyNotificationId = CreditNotificationDomainModel.CreateForCredit(creditNr, context, Service.PaymentOrder.GetPaymentOrderItems(), onlyFetchOpen: false);
                var notifications = CreditNotificationDomainModel.GetNotificationListModel(context, date, creditNr, modelsbyNotificationId);
                var totalUnpaidAmount = notifications.Where(x => !x.IsPaid).Aggregate(0m, (acc, x) => acc + x.InitialAmount - x.PaidAmount - x.WrittenOffAmount);
                var totalOverDueUnpaidAmount = notifications.Where(x => !x.IsPaid && x.IsOverDue).Aggregate(0m, (acc, x) => acc + x.InitialAmount - x.PaidAmount - x.WrittenOffAmount);

                var model = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, NEnv.EnvSettings);
                var promisedToPayDate = model.GetPromisedToPayDate(this.Clock.Today);
                var hasTerminationLettersThatSuspendTheCreditProcess = NewCreditTerminationLettersBusinessEventManager.HasTerminationLettersThatSuspendTheCreditProcess(NEnv.ClientCfgCore);

                object latestActiveCreditProcessSuspendingTerminationLetters = null;
                if (hasTerminationLettersThatSuspendTheCreditProcess && credit.LatestActiveProcessSuspendingTerminationLetter?.DueDate != null)
                {
                    var c = credit.LatestActiveProcessSuspendingTerminationLetter;
                    latestActiveCreditProcessSuspendingTerminationLetters = GetLatestActiveCreditProcessSuspendingTerminationLetters(c.Id, c.CoTerminationId, c.IsCoTerminationMaster, context);
                }

                return Json2(new
                {
                    today = date,
                    ocrPaymentReference = model.GetOcrPaymentReference(this.Clock.Today),
                    creditNr = creditNr,
                    creditStatus = model.GetStatus().ToString(),
                    promisedToPayDate = promisedToPayDate,
                    notifications = notifications,
                    totalUnpaidAmount = totalUnpaidAmount,
                    totalOverDueUnpaidAmount = totalOverDueUnpaidAmount,
                    hasTerminationLettersThatSuspendTheCreditProcess,
                    latestActiveCreditProcessSuspendingTerminationLetterDuedate = credit.LatestActiveProcessSuspendingTerminationLetter?.DueDate,
                    latestActiveCreditProcessSuspendingTerminationLetters
                });
            }
        }

        private object GetLatestActiveCreditProcessSuspendingTerminationLetters(int terminationLetterId, string coTerminationId, bool? isCoTerminationMaster, CreditContextExtended context)
        {
            IQueryable<CreditTerminationLetterHeader> documentHost;
            if (coTerminationId == null || isCoTerminationMaster == true)
                documentHost = context.CreditTerminationLetterHeaders.Where(x => x.Id == terminationLetterId);
            else
                documentHost = context.CreditTerminationLetterHeaders.Where(x => x.CoTerminationId == coTerminationId && x.IsCoTerminationMaster == true);

            List<string> coTerminationCreditNrs = null;
            if (coTerminationId != null)
                coTerminationCreditNrs = context.CreditTerminationLetterHeaders.Where(x => x.CoTerminationId == coTerminationId).Select(x => x.CreditNr).ToList();

            return documentHost.SelectMany(x => x.Documents.Select(y => new
            {
                y.CustomerId,
                y.ArchiveKey
            })).ToList().Select(x => new
            {
                customerId = x.CustomerId,
                archiveKey = x.ArchiveKey,
                coTerminationCreditNrs
            });
        }

        [HttpPost]
        [Route("Api/Credit/AllUnpaidNotifications")]
        public ActionResult AllUnpaidNotifications(DateTime? notificationDate)
        {
            using (var context = new CreditContext())
            {
                var queryBase = CurrentNotificationStateServiceLegacy
                    .GetCurrentOpenNotificationsStateQuery(context, Clock.Today);
                if (notificationDate.HasValue)
                    queryBase = queryBase.Where(x => x.NotificationDate == notificationDate.Value);

                var result = queryBase
                    .Select(x => new
                    {
                        x.NotificationId,
                        x.CreditNr,
                        x.DueDate,
                        x.NotificationDate,
                        UnpaidAmount = x.RemainingAmount,
                        OcrPaymentReference = x.OcrPaymentReference,
                    })
                    .ToList();

                var notificationIds = result.Select(x => x.NotificationId).ToList();
                var sharedOcrByNotificationId = context
                    .CreditNotificationHeaders
                    .Where(x => notificationIds.Contains(x.Id))
                    .Select(x => new
                    {
                        NotificationId = x.Id,
                        SharedOcrPaymentReference = x
                            .Credit
                            .DatedCreditStrings
                            .Where(y => y.Name == DatedCreditStringCode.SharedOcrPaymentReference.ToString())
                            .OrderByDescending(y => y.Id)
                            .Select(y => y.Value)
                            .FirstOrDefault(),
                        x.CoNotificationId
                    })
                    .ToDictionary(x => x.NotificationId);

                return Json2(new { notifications = result.Select(x => new
                {
                    x.CreditNr,
                    x.NotificationDate,
                    x.DueDate,
                    x.UnpaidAmount,
                    x.OcrPaymentReference,
                    sharedOcrByNotificationId.Opt(x.NotificationId)?.SharedOcrPaymentReference,
                    sharedOcrByNotificationId.Opt(x.NotificationId)?.CoNotificationId,
                    ExpectedPaymentOcrPaymentReference = sharedOcrByNotificationId.Opt(x.NotificationId)?.SharedOcrPaymentReference ?? x.OcrPaymentReference
                })});
            }
        }

        [HttpPost]
        [Route("Api/Credit/MaxTransactionDate")]
        public ActionResult MaxTransactionDate()
        {
            using (var context = new CreditContext())
            {
                var d = context.Transactions.OrderByDescending(x => x.TransactionDate).Select(x => (DateTime?)x.TransactionDate).FirstOrDefault();
                return Json2(new { maxTransactionDate = d });
            }
        }
    }
}