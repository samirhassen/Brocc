using nCredit.DomainModel;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.BankAccounts.Se;
using NTech.Core.Credit.Shared.Services;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class ApiCreditNotificationDetailsController : NController
    {
        [HttpPost]
        [Route("Api/Credit/NotificationDetails")]
        public ActionResult Details(int notificationId)
        {
            if (notificationId <= 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing notificationId");

            using (var context = CreateCreditContext())
            {
                var date = Clock.Today;
                var h = context
                    .CreditNotificationHeaders
                    .Where(x => x.Id == notificationId).Select(x => new
                    {
                        x.Id,
                        x.CreditNr,
                        x.NotificationDate,
                        x.DueDate,
                        x.OcrPaymentReference,
                        x.PdfArchiveKey,
                        CoNotificationCreditNrs = context.CreditNotificationHeaders.Where(y => y.CoNotificationId != null && y.CoNotificationId == x.CoNotificationId).Select(y => y.CreditNr).AsEnumerable(),
                        CoNotificationMasterPdfArchiveKey =
                            x.IsCoNotificationMaster == false && x.CoNotificationId != null
                            ? context.CreditNotificationHeaders.Where(y => y.CoNotificationId == x.CoNotificationId && y.IsCoNotificationMaster == true).Select(y => y.PdfArchiveKey).FirstOrDefault()
                            : null,
                        Reminders = x.Reminders.OrderBy(y => y.ReminderNumber).Select(y => new
                        {
                            y.CreditNr,
                            y.ReminderDate,
                            y.ReminderNumber,
                            y.CoReminderId,
                            y.IsCoReminderMaster,
                            Documents = y.Documents.OrderBy(z => z.ApplicantNr).Select(z => new 
                            {
                                z.CustomerId,
                                z.ArchiveKey,
                            })
                        }),
                        SharedOcrPaymentReference = x.Credit.DatedCreditStrings
                            .Where(y => y.Name == DatedCreditStringCode.SharedOcrPaymentReference.ToString()).OrderByDescending(y => y.Id).Select(y => y.Value).FirstOrDefault()
                    })
                    .SingleOrDefault();
                
                if (h == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such notification");

                Dictionary<string, List<(int? CustomerId, string ArchiveKey)>> coReminderDocumentsByCoReminderId = null;
                Dictionary<string, List<string>> coReminderCreditsNrsByCoReminderId = null;
                if(h.Reminders.Any(x => x.CoReminderId != null))
                {
                    var coReminderIds = h.Reminders.Where(x => x.CoReminderId != null).Select(x => x.CoReminderId).ToHashSetShared();
                    var coReminderData = context
                        .CreditReminderHeaders.Where(x => coReminderIds.Contains(x.CoReminderId))
                        .Select(x => new
                        {
                            x.CoReminderId,
                            x.CreditNr,
                            x.IsCoReminderMaster,
                            Documents = x.Documents.Select(y => new
                            {
                                y.CustomerId,
                                y.ArchiveKey
                            }).ToList()
                        })
                        .ToList();
                    coReminderDocumentsByCoReminderId = coReminderData
                        .Where(x => x.IsCoReminderMaster == true)
                        .ToDictionary(x => x.CoReminderId, x => x.Documents.Select(y => (CustomerId: y.CustomerId, ArchiveKey: y.ArchiveKey)).ToList());
                    coReminderCreditsNrsByCoReminderId = coReminderData
                        .GroupBy(x => x.CoReminderId)
                        .ToDictionary(x => x.Key, x => x.Select(y => y.CreditNr).ToList());
                }

                var paymentOrderService = Service.PaymentOrder;
                var paymentOrder = paymentOrderService.GetPaymentOrderUiItems();

                var model = CreditNotificationDomainModel.CreateForSingleNotification(h.Id, context, paymentOrder.Select(x => x.OrderItem).ToList());

                var balance = new ExpandoObject() as IDictionary<string, object>;
                var totalInitialAmount = 0m;
                var totalWrittenOffAmount = 0m;
                var totalPaidAmount = 0m;
                var totalRemainingAmount = 0m;
                
                foreach (var amountTypeItem in paymentOrder.Select(x => x.OrderItem))
                {
                    var initialAmount = model.GetInitialAmount(date, amountTypeItem);
                    balance[amountTypeItem.GetUniqueId() + "InitialAmount"] = initialAmount;
                    totalInitialAmount += initialAmount;

                    var writtenOffAmount = model.GetWrittenOffAmount(date, amountTypeItem);
                    balance[amountTypeItem.GetUniqueId() + "WrittenOffAmount"] = writtenOffAmount;
                    totalWrittenOffAmount += writtenOffAmount;

                    var paidAmount = model.GetPaidAmount(date, amountTypeItem);
                    balance[amountTypeItem.GetUniqueId() + "PaidAmount"] = paidAmount;
                    totalPaidAmount += paidAmount;

                    var remainingAmount = initialAmount - writtenOffAmount - paidAmount;
                    balance[amountTypeItem.GetUniqueId() + "RemainingAmount"] = remainingAmount;
                    totalRemainingAmount += remainingAmount;
                }
                balance["TotalInitialAmount"] = totalInitialAmount;
                balance["TotalRemainingAmount"] = totalRemainingAmount;
                balance["TotalWrittenOffAmount"] = totalWrittenOffAmount;
                balance["TotalPaidAmount"] = totalPaidAmount;

                var incomingPaymentAccount = Service.PaymentAccount.GetIncomingPaymentBankAccountNr();
                var archiveKey = h.PdfArchiveKey ?? h.CoNotificationMasterPdfArchiveKey;
                var result = new
                {
                    NotifcationPdfLink = archiveKey == null
                                ? null
                                : Url.Action("ArchiveDocument", "ApiArchiveDocument", new { key = archiveKey }),
                    NotificationArchiveKey = archiveKey,
                    CoNotificationCreditNrs = h.CoNotificationCreditNrs.ToList(),
                    h.OcrPaymentReference,
                    h.SharedOcrPaymentReference,
                    h.NotificationDate,
                    h.DueDate,
                    PaymentIBAN = NEnv.ClientCfg.Country.BaseCountry == "FI" ? ((IBANFi)incomingPaymentAccount).NormalizedValue : null,
                    PaymentBankGiro = NEnv.ClientCfg.Country.BaseCountry == "SE" ? ((BankGiroNumberSe)incomingPaymentAccount).NormalizedValue : null,
                    Balance = balance,
                    Payments = model.GetPayments(date).OrderByDescending(x => x.TransactionDate).ToList(),
                    Reminders = h.Reminders.SelectMany(x =>
                    {
                        if (!x.IsCoReminderMaster.HasValue)
                        {
                            return x.Documents.Select(document => new LocalReminderDocument
                            {
                                ReminderDate = x.ReminderDate,
                                ReminderNumber = x.ReminderNumber,
                                CustomerId = document.CustomerId,
                                ReminderPdfLink = archiveKey == null
                                ? null
                                : Url.Action("ArchiveDocument", "ApiArchiveDocument", new { key = archiveKey }),
                                ArchiveKey = document.ArchiveKey,
                                CoReminderCreditNrs = (List<string>)null,
                            }).ToList();
                        }
                        else
                        {
                            var documents = coReminderDocumentsByCoReminderId[x.CoReminderId];
                            return documents.Select(document => new LocalReminderDocument
                            {
                                ReminderDate = x.ReminderDate,
                                ReminderNumber = x.ReminderNumber,
                                CustomerId = document.CustomerId,
                                ReminderPdfLink = archiveKey == null
                                ? null
                                : Url.Action("ArchiveDocument", "ApiArchiveDocument", new { key = archiveKey }),
                                ArchiveKey = document.ArchiveKey,
                                CoReminderCreditNrs = coReminderCreditsNrsByCoReminderId[x.CoReminderId],
                            }).ToList();
                        }
                    }).ToList(),
                    PaymentOrderItems = paymentOrder,
                    CreditNr = h.CreditNr
                };

                return Json2(result);
            }
        }

        private class LocalReminderDocument
        {
            public DateTime ReminderDate { get; internal set; }
            public int ReminderNumber { get; internal set; }
            public int? CustomerId { get; internal set; }
            public string ReminderPdfLink { get; internal set; }
            public string ArchiveKey { get; internal set; }
            public List<string> CoReminderCreditNrs { get; internal set; }
        }
    }
}