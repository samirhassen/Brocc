using nCredit.Code;
using nCredit.Excel;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    [RoutePrefix("Api/AccountTransaction")]
    public class ApiAccountTransactionDetailsController : NController
    {
        private class CapitalDebtTransactionDetailsModel
        {
            public string AccountCode { get; set; }
            public decimal Amount { get; set; }
            public long TransactionId { get; set; }
            public DateTime TransactionDate { get; set; }
            public int BusinessEventId { get; set; }
            public string CreditNr { get; set; }
            public DateTime? BookkeepingExportDate { get; set; }

            public bool HasConnectedOutgoingPayment { get; set; }
            public DateTime? OutgoingPaymentFileDate { get; set; }

            public bool HasConnectedIncomingPayment { get; set; }
            public int? IncomingPaymentId { get; set; }
            public DateTime? IncomingPaymentFileDate { get; set; }
            public string IncomingPaymentExternalId { get; set; }
            public string IncomingPaymentOcrReference { get; set; }
            public string IncomingPaymentClientAccountIban { get; set; }
            public string IncomingPaymentCustomerName { get; set; }
            public string IncomingPaymentAutogiroPayerNumber { get; set; }
            public string BusinessEventRoleCode { get; set; }
            public string SubAccountCode { get; set; }
        }

        [HttpPost]
        [Route("CapitalDebtTransactionDetails")]
        public ActionResult GetCreditCapitalDebtTransactionDetails(int transactionId)
        {
            using (var context = new CreditContext())
            {
                var tr = context
                    .Transactions
                    .Where(x => x.Id == transactionId)
                    .Select(x => new
                    {
                        x.AccountCode,
                        x.Amount,
                        TransactionId = x.Id,
                        TransactionDate = x.TransactionDate,
                        x.BusinessEventId,
                        BusinessEventType = x.BusinessEvent.EventType,
                        x.CreditNr,
                        x.BusinessEventRoleCode,
                        x.SubAccountCode,
                        CreditTransactions = x
                            .BusinessEvent
                            .Transactions
                            .Where(y => x.CreditNr == y.CreditNr)
                    })
                    .Select(x => new
                    {
                        AccountCode = x.AccountCode,
                        Amount = x.Amount,
                        TransactionId = x.TransactionId,
                        TransactionDate = x.TransactionDate,
                        BusinessEventId = x.BusinessEventId,
                        CreditNr = x.CreditNr,
                        BookkeepingExportDate = x
                            .CreditTransactions
                            .Where(y => y.OutgoingBookkeepingFileHeaderId.HasValue)
                            .Min(y => (DateTime?)y.OutgoingBookkeepingFile.TransactionDate),
                        HasConnectedOutgoingPayment = x.CreditTransactions.Any(y => y.OutgoingPaymentId.HasValue),
                        HasConnectedIncomingPayment = x.CreditTransactions.Any(y => y.IncomingPaymentId.HasValue),
                        ConnectedIncomingPaymentIds = x.CreditTransactions.Where(y => y.IncomingPaymentId.HasValue).Select(y => y.IncomingPaymentId),
                        OutgoingPaymentFileDate = x.CreditTransactions.Min(y => (DateTime?)y.OutgoingBookkeepingFile.TransactionDate),
                        BusinessEventRoleCode = x.BusinessEventRoleCode,
                        x.SubAccountCode
                    })
                    .SingleOrDefault();
                if (tr == null || tr.CreditNr == null || tr.AccountCode != TransactionAccountType.CapitalDebt.ToString())
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Not a credit capital transaction");
                var result = new CapitalDebtTransactionDetailsModel
                {
                    AccountCode = tr.AccountCode,
                    Amount = tr.Amount,
                    HasConnectedOutgoingPayment = tr.HasConnectedOutgoingPayment,
                    BusinessEventId = tr.BusinessEventId,
                    CreditNr = tr.CreditNr,
                    BookkeepingExportDate = tr.BookkeepingExportDate,
                    OutgoingPaymentFileDate = tr.OutgoingPaymentFileDate,
                    TransactionDate = tr.TransactionDate,
                    TransactionId = tr.TransactionId,
                    HasConnectedIncomingPayment = false,
                    BusinessEventRoleCode = tr.BusinessEventRoleCode,
                    SubAccountCode = tr.SubAccountCode
                };
                if (tr.HasConnectedIncomingPayment)
                {
                    var connectedIncomingPaymentIds = tr.ConnectedIncomingPaymentIds.Select(y => y.Value).Distinct().ToList();
                    if (connectedIncomingPaymentIds.Count > 1)
                        throw new Exception($"Transaction {transactionId} has a business event with multiple connected incoming payments which is not supported here");
                    var connectedIncomingPaymentId = connectedIncomingPaymentIds.Single();

                    var namesToInclude = new[]
                    {
                        IncomingPaymentHeaderItemCode.ExternalId.ToString(),
                        IncomingPaymentHeaderItemCode.OcrReference.ToString(),
                        IncomingPaymentHeaderItemCode.ClientAccountIban.ToString(),
                        IncomingPaymentHeaderItemCode.CustomerName.ToString(),
                        IncomingPaymentHeaderItemCode.AutogiroPayerNumber.ToString()
                    };
                    var p = context
                        .IncomingPaymentHeaders
                        .Where(x => x.Id == connectedIncomingPaymentId)
                        .Select(x => new
                        {
                            x.Id,
                            Items = x.Items.Where(y => namesToInclude.Contains(y.Name)).Select(y => new { y.Name, y.Value, y.IsEncrypted }),
                            IncomingPaymentFileDate = (DateTime?)x.IncomingPaymentFile.TransactionDate
                        })
                        .Single();

                    var items = p.Items.ToDictionary(x => x.Name, x => new { x.Value, x.IsEncrypted });
                    var encryptedItemIds = p.Items.Where(x => x.IsEncrypted).Select(y => long.Parse(y.Value)).ToArray();
                    var decryptedItems = encryptedItemIds.Any()
                        ? EncryptionContext.Load(context, encryptedItemIds, NEnv.EncryptionKeys.AsDictionary())
                        : null;

                    Func<IncomingPaymentHeaderItemCode, string> getValue = n =>
                    {
                        var i = items.Opt(n.ToString());
                        if (i == null) return null;
                        return i.IsEncrypted ? decryptedItems[long.Parse(i.Value)] : i.Value;
                    };

                    result.HasConnectedIncomingPayment = true;
                    result.IncomingPaymentId = p.Id;
                    result.IncomingPaymentFileDate = p.IncomingPaymentFileDate;
                    result.IncomingPaymentExternalId = getValue(IncomingPaymentHeaderItemCode.ExternalId);
                    result.IncomingPaymentClientAccountIban = getValue(IncomingPaymentHeaderItemCode.ClientAccountIban);
                    result.IncomingPaymentOcrReference = getValue(IncomingPaymentHeaderItemCode.OcrReference);
                    result.IncomingPaymentCustomerName = getValue(IncomingPaymentHeaderItemCode.CustomerName);
                    result.IncomingPaymentAutogiroPayerNumber = getValue(IncomingPaymentHeaderItemCode.AutogiroPayerNumber);
                }

                return Json2(new
                {
                    Details = result
                });
            }
        }

        [HttpGet]
        [Route("BusinessEventTransactionDetailsAsXls")]
        public ActionResult GetBusinessEventTransactionDetailsAsXls(int businessEventId)
        {
            using (var context = new CreditContext())
            {
                var trs = GetAccountTransactionUiDetailsModel(context)
                    .Where(x => x.TransactionBusinessEventId == businessEventId)
                    .ToList();

                var request = new DocumentClientExcelRequest
                {
                    Sheets = new DocumentClientExcelRequest.Sheet[]
                    {
                            new DocumentClientExcelRequest.Sheet
                            {
                                AutoSizeColumns = true,
                                Title = "Transactions"
                            }
                    }
                };

                var s = request.Sheets[0];
                s.SetColumnsAndData(trs,
                    trs.Col(x => x.TransactionTransactionDate, ExcelType.Date, "Date"),
                    trs.Col(x => x.TransactionBusinessEventType, ExcelType.Text, "Event"),
                    trs.Col(x => x.TransactionAccountCode, ExcelType.Text, "Account"),
                    trs.Col(x => x.TransactionAmount, ExcelType.Number, "Amount"),
                    trs.Col(x => x.CreditNr, ExcelType.Text, "Connected credit"),
                    trs.Col(x => x.NotificationDueDate, ExcelType.Date, "Connected notification"),
                    trs.Col(x => x.IncomingPaymentFileTransactionDate ?? x.IncomingPaymentTransactionDate, ExcelType.Date, "Connected incoming payment"),
                    trs.Col(x => x.OutgoingPaymentFileTransactionDate ?? x.OutgoingPaymentTransactionDate, ExcelType.Date, "Connected outgoing payment"),
                    trs.Col(x => x.ReminderDate, ExcelType.Date, "Connected reminder"),
                    trs.Col(x => x.WriteoffTransactionDate, ExcelType.Date, "Connected writeoff"),
                    trs.Col(x => x.PaymentFreeMonthForDueDate, ExcelType.Date, "Connected paymentfree month"),
                    trs.Col(x => x.BookKeepingFileTransactionDate, ExcelType.Date, "Included in bookkeeping"),
                    trs.Col(x => x.TransactionId.ToString(), ExcelType.Text, "Transaction id")
                );

                var client = Service.DocumentClientHttpContext;
                var report = client.CreateXlsx(request);

                return new FileStreamResult(report, XlsxContentType) { FileDownloadName = $"BusinessEventTransactions-{businessEventId}.xlsx" };
            }
        }

        [HttpGet()]
        [Route("CreditFilteredPaymentDetailsAsXls")]
        public ActionResult GetCreditFilteredPaymentDetailsAsXls(int paymentId, string creditNr)
        {
            //A payment can in theory be spread across multiple credits (placed in parts from unplaced for instance) hence the creditNr filter
            const string Sql = @"with Trs
as
(
	select	t.*
	from	AccountTransaction t
	where	t.BusinessEventId in(select t.BusinessEventId from AccountTransaction t where t.CreditNr = @creditNr and t.IncomingPaymentId = @incomingPaymentId)
	and		t.CreditNr = @creditNr
	union all
	select	t.*
	from	AccountTransaction t
	where	t.IncomingPaymentId = @incomingPaymentId
	and		t.CreditNr is null
),
Tmp
as
(
	select	case
				when t.CreditNr is not null and t.AccountCode = 'CapitalDebt' and t.CreditNotificationId is not null and t.IncomingPaymentId is not null and t.WriteoffId is null and t.Amount < 0 then 'PaymentNotifiedCapital'
				when t.CreditNr is not null and t.AccountCode <> 'CapitalDebt' and t.CreditNotificationId is not null and t.IncomingPaymentId is not null and t.WriteoffId is null and t.Amount < 0 then 'PaymentNotifiedX'
				when t.CreditNr is not null and t.AccountCode = 'CapitalDebt' and t.CreditNotificationId is null and t.IncomingPaymentId is not null and t.WriteoffId is null  and t.Amount < 0 then 'PaymentNotNotifiedCapital'
				when t.CreditNr is not null and t.AccountCode = 'InterestDebt' and t.CreditNotificationId is null and t.IncomingPaymentId is not null and t.WriteoffId is null and t.Amount < 0 then 'PaymentNotNotifiedInterest'
                when t.CreditNr is not null and t.AccountCode = 'SwedishRseDebt' and t.CreditNotificationId is null and t.IncomingPaymentId is not null and t.WriteoffId is null and t.Amount < 0 then 'PaymentNotNotifiedSwedishRse'
				when t.CreditNr is not null and t.AccountCode = 'InterestDebt' and t.CreditNotificationId is null and t.IncomingPaymentId is null and t.WriteoffId is null and t.Amount > 0 then 'Ignored' --Created not notified interest
                when t.CreditNr is not null and t.AccountCode = 'SwedishRseDebt' and t.CreditNotificationId is null and t.IncomingPaymentId is null and t.WriteoffId is null and t.Amount > 0 then 'Ignored' --Created RSE
				when t.CreditNr is not null and t.AccountCode = 'NotNotifiedCapital' and t.CreditNotificationId is null and t.IncomingPaymentId is null and t.WriteoffId is null then 'Ignored'
				when t.CreditNr is not null and t.AccountCode = 'CapitalDebt' and t.CreditNotificationId is null and t.IncomingPaymentId is null and t.WriteoffId is not null then 'WriteoffNotNotifiedCapital'
				when t.CreditNr is not null and t.AccountCode <> 'CapitalDebt' and t.CreditNotificationId is not null and t.IncomingPaymentId is null and t.WriteoffId is not null then 'WriteoffNotifiedX'
				when t.CreditNr is not null and t.AccountCode = 'NotNotifiedCapital' and t.CreditNotificationId is null and t.IncomingPaymentId is null and t.WriteoffId is not null then 'Ignored'
				when t.CreditNr is null and t.AccountCode = 'UnplacedPayment' and t.IncomingPaymentId is not null and t.Amount > 0 then 'AddedToUnplacedPayment'
				when t.CreditNr is not null and t.AccountCode = 'UnplacedPayment' and t.IncomingPaymentId is not null and t.Amount < 0 then 'Ignored'
                when t.CreditNr is null and b.EventType = 'Repayment' and t.IncomingPaymentId is not null then 'Repayment'
				else 'Unknown'
			end as TransactionCategory,
			t.TransactionDate,
			b.EventType,
			t.AccountCode,
			-t.Amount as Amount,
			t.CreditNotificationId,
			t.CreditNr,
			t.BusinessEventId,
			t.Id as TransactionId,
            h.DueDate as NotificationDueDate,
            cast(case when t.WriteoffId is null then 0 else 1 end as bit) as IsWriteoff
	from	Trs t
	join	BusinessEvent b on b.Id = t.BusinessEventId
    left outer join CreditNotificationHeader h on h.Id = t.CreditNotificationId
)
select	t.*,
        cast(case when exists(select 1 from DatedCreditString d where d.CreditNr = @creditNr and d.BusinessEventId = t.BusinessEventId and d.Name = 'CreditStatus' and d.Value = 'Settled') then 1 else 0 end as bit) as IsCreditSettledByEvent
from	Tmp t
where	t.TransactionCategory <> 'Ignored'
order by t.BusinessEventId, t.TransactionId";
            using (var context = new CreditContext())
            {
                var result = context
                    .Database
                    .SqlQuery<IncomingPaymentTransactionModel>(
                        Sql,
                        new SqlParameter("@creditNr", creditNr),
                        new SqlParameter("@incomingPaymentId", paymentId))
                    .ToList();

                var rows = new List<IncomingPaymentExcelRow>();
                var allAccountCodes = result.Select(x => x.AccountCode).Distinct().ToList(); //TODO: Custom order?
                foreach (var evt in result.GroupBy(x => x.BusinessEventId).OrderBy(x => x.Key))
                {
                    //AddedToUnplacedPayment
                    foreach (var n in evt.Where(x => x.TransactionCategory == "AddedToUnplacedPayment"))
                    {
                        var er = new IncomingPaymentExcelRow
                        {
                            EventType = n.EventType,
                            EventDate = n.TransactionDate,
                            Target = $"Added to unplaced",
                            UnplacedAmount = n.Amount,
                            PaidAmount = new Dictionary<string, decimal>(),
                            WrittenOffAmount = new Dictionary<string, decimal>()
                        };
                        rows.Add(er);
                        n.IsProcessed = true;
                    }

                    Action<IDictionary<string, decimal>, string, decimal> addTo = (a, b, c) =>
                    {
                        if (!a.ContainsKey(b))
                            a[b] = 0m;
                        a[b] += c;
                    };

                    //Notifications
                    foreach (var n in evt
                        .Where(x => x.TransactionCategory == "PaymentNotifiedCapital" || x.TransactionCategory == "PaymentNotifiedX" || x.TransactionCategory == "WriteoffNotifiedX")
                        .GroupBy(x => x.NotificationDueDate.Value))
                    {
                        var er = new IncomingPaymentExcelRow
                        {
                            EventType = n.First().EventType,
                            EventDate = n.First().TransactionDate,
                            Target = $"Notification due {n.Key.ToString("yyyy-MM-dd")}",
                            PaidAmount = new Dictionary<string, decimal>(),
                            WrittenOffAmount = new Dictionary<string, decimal>()
                        };
                        foreach (var ntr in n)
                        {
                            ntr.IsProcessed = true;
                            if (ntr.IsWriteoff)
                                addTo(er.WrittenOffAmount, ntr.AccountCode, ntr.Amount);
                            else
                                addTo(er.PaidAmount, ntr.AccountCode, ntr.Amount);
                        }
                        er.TotalPaidAmount = er.PaidAmount.Sum(x => x.Value);
                        rows.Add(er);
                    }

                    //Not notified capital or interest
                    var notNotified = evt.Where(x => x.TransactionCategory.IsOneOf("PaymentNotNotifiedCapital", "WriteoffNotNotifiedCapital", "PaymentNotNotifiedInterest", "PaymentNotNotifiedSwedishRse")).ToList();
                    if (notNotified.Any())
                    {
                        var er = new IncomingPaymentExcelRow
                        {
                            EventType = evt.First().EventType,
                            EventDate = evt.First().TransactionDate,
                            Target = $"Not notified",
                            PaidAmount = new Dictionary<string, decimal>(),
                            WrittenOffAmount = new Dictionary<string, decimal>()
                        };
                        foreach (var ntr in notNotified)
                        {
                            ntr.IsProcessed = true;
                            if (ntr.IsWriteoff)
                                addTo(er.WrittenOffAmount, ntr.AccountCode, ntr.Amount);
                            else
                                addTo(er.PaidAmount, ntr.AccountCode, ntr.Amount);
                        }
                        er.TotalPaidAmount = er.PaidAmount.Sum(x => x.Value);
                        rows.Add(er);
                    }

                    //Repayments
                    var repayments = evt.Where(x => x.TransactionCategory == "Repayment").ToList();
                    if (repayments.Any())
                    {
                        var er = new IncomingPaymentExcelRow
                        {
                            EventType = evt.First().EventType,
                            EventDate = evt.First().TransactionDate,
                            Target = $"Repayment",
                            UnplacedAmount = repayments.Sum(x => x.Amount),
                            PaidAmount = new Dictionary<string, decimal>(),
                            WrittenOffAmount = new Dictionary<string, decimal>()
                        };
                        foreach (var ntr in repayments)
                        {
                            ntr.IsProcessed = true;
                        }
                        rows.Add(er);
                    }
                    var settlementProxy = evt.FirstOrDefault(x => x.IsCreditSettledByEvent);//Any will do
                    if (settlementProxy != null)
                        rows.Add(new IncomingPaymentExcelRow
                        {
                            EventDate = settlementProxy.TransactionDate,
                            EventType = settlementProxy.EventType,
                            Target = "Credit settled",
                            PaidAmount = new Dictionary<string, decimal>(),
                            WrittenOffAmount = new Dictionary<string, decimal>()
                        });
                }

                if (result.Any(x => !x.IsProcessed))
                {
                    //Guard against forgetting to update this in case of future changes. Better than showing incorrect values.
                    throw new Exception($"GetCreditFilteredPaymentDetailsAsXls failed on payment with id {paymentId} and credit {creditNr} since some there are transactions not being counted");
                }

                var request = new DocumentClientExcelRequest
                {
                    Sheets = new DocumentClientExcelRequest.Sheet[]
                    {
                            new DocumentClientExcelRequest.Sheet
                            {
                                AutoSizeColumns = true,
                                Title = "Payment details"
                            }
                    }
                };

                var s = request.Sheets[0];
                var cols = DocumentClientExcelRequest.CreateDynamicColumnList(rows);

                cols.Add(rows.Col(x => x.EventType, ExcelType.Text, "Event type"));
                cols.Add(rows.Col(x => x.EventDate, ExcelType.Date, "Event date"));
                cols.Add(rows.Col(x => x.Target, ExcelType.Text, "Target"));
                cols.Add(rows.Col(x => x.UnplacedAmount, ExcelType.Number, "Unplaced amount"));
                cols.Add(rows.Col(x => x.TotalPaidAmount, ExcelType.Number, $"Total Paid", includeSum: true));
                foreach (var accountCode in allAccountCodes.Except(new[] { "UnplacedPayment" }))
                {
                    if (rows.Any(x => x.PaidAmount.ContainsKey(accountCode)))
                        cols.Add(rows.Col(x => x.PaidAmount.OptS(accountCode), ExcelType.Number, $"Paid {accountCode}", includeSum: true));
                }
                foreach (var accountCode in allAccountCodes.Except(new[] { "UnplacedPayment" }))
                {
                    if (rows.Any(x => x.WrittenOffAmount.ContainsKey(accountCode)))
                        cols.Add(rows.Col(x => x.WrittenOffAmount.OptS(accountCode), ExcelType.Number, $"Written off {accountCode}", includeSum: true));
                }

                s.SetColumnsAndData(rows, cols.ToArray());

                var client = Service.DocumentClientHttpContext;
                var report = client.CreateXlsx(request);

                return new FileStreamResult(report, XlsxContentType) { FileDownloadName = $"PaymentDetails-{creditNr}-{paymentId}.xlsx" };
            }
        }

        private class IncomingPaymentExcelRow
        {
            public string EventType { get; set; }
            public DateTime EventDate { get; set; }
            public string Target { get; set; }
            public decimal? UnplacedAmount { get; set; }
            public decimal? TotalPaidAmount { get; set; }
            public IDictionary<string, decimal> PaidAmount { get; set; }
            public IDictionary<string, decimal> WrittenOffAmount { get; set; }
        }

        private class IncomingPaymentTransactionModel
        {
            public string TransactionCategory { get; set; }
            public DateTime TransactionDate { get; set; }
            public string EventType { get; set; }
            public string AccountCode { get; set; }
            public decimal Amount { get; set; }
            public DateTime? NotificationDueDate { get; set; }
            public string CreditNr { get; set; }
            public int BusinessEventId { get; set; }
            public long TransactionId { get; set; }
            public bool IsWriteoff { get; set; }
            public bool IsProcessed { get; set; }
            public bool IsCreditSettledByEvent { get; set; }
        }

        private IQueryable<AccountTransactionExcelModel> GetAccountTransactionUiDetailsModel(CreditContext context)
        {
            return context.Transactions.Select(x => new AccountTransactionExcelModel
            {
                //Transaction
                TransactionId = x.Id,
                TransactionAccountCode = x.AccountCode,
                TransactionTransactionDate = x.TransactionDate,
                TransactionAmount = x.Amount,
                TransactionBusinessEventId = x.BusinessEventId,
                TransactionBusinessEventType = x.BusinessEvent.EventType,

                //Credit
                CreditNr = x.CreditNr,

                //Notification
                NotificationId = x.CreditNotificationId,
                NotificationDueDate = x.CreditNotification.DueDate,

                //Incoming payment
                IncomingPaymentId = x.IncomingPaymentId,
                IncomingPaymentTransactionDate = x.IncomingPayment.TransactionDate,
                IncomingPaymentIsFullyPlaced = x.IncomingPayment.IsFullyPlaced,
                IncomingPaymentFileId = x.IncomingPayment.IncomingPaymentFileId,
                IncomingPaymentFileTransactionDate = x.IncomingPayment.IncomingPaymentFile.TransactionDate,
                IncomingPaymentFilePdfArchiveKey = x.IncomingPayment.IncomingPaymentFile.FileArchiveKey,

                //Outgoing payment
                OutgoingPaymentId = x.OutgoingPaymentId,
                OutgoingPaymentTransactionDate = x.OutgoingPayment.TransactionDate,
                OutgoingPaymentFileId = x.OutgoingPayment.OutgoingPaymentFileHeaderId,
                OutgoingPaymentFilePdfArchiveKey = x.OutgoingPayment.OutgoingPaymentFile.FileArchiveKey,
                OutgoingPaymentFileTransactionDate = x.OutgoingPayment.OutgoingPaymentFile.TransactionDate,

                //Reminder
                ReminderId = x.ReminderId,
                ReminderDate = x.Reminder.ReminderDate,
                ReminderNumber = x.Reminder.ReminderNumber,
                ReminderNotificationDueDate = x.Reminder.Notification.DueDate,

                //Writeoff
                WriteoffId = x.WriteoffId,
                WriteoffTransactionDate = x.Writeoff.TransactionDate,

                //BookKeeping
                BookKeepingFileId = x.OutgoingBookkeepingFileHeaderId,
                BookKeepingFileXlsArchiveKey = x.OutgoingBookkeepingFile.XlsFileArchiveKey,
                BookKeepingFileTransactionDate = x.OutgoingBookkeepingFile.TransactionDate,

                //Payment free month
                PaymentFreeMonthId = x.CreditPaymentFreeMonthId,
                PaymentFreeMonthForDueDate = x.PaymentFreeMonth.DueDate
            });
        }

        private class AccountTransactionExcelModel
        {
            public long TransactionId { get; set; }
            public string TransactionAccountCode { get; set; }
            public DateTime TransactionTransactionDate { get; set; }
            public decimal TransactionAmount { get; set; }
            public int TransactionBusinessEventId { get; set; }
            public string TransactionBusinessEventType { get; set; }

            public string CreditNr { get; set; }

            public int? NotificationId { get; set; }
            public DateTime? NotificationDueDate { get; set; }

            public int? IncomingPaymentId { get; set; }
            public DateTime? IncomingPaymentTransactionDate { get; set; }
            public bool? IncomingPaymentIsFullyPlaced { get; set; }
            public int? IncomingPaymentFileId { get; set; }
            public DateTime? IncomingPaymentFileTransactionDate { get; set; }
            public string IncomingPaymentFilePdfArchiveKey { get; set; }

            public int? OutgoingPaymentId { get; set; }
            public DateTime? OutgoingPaymentTransactionDate { get; set; }
            public int? OutgoingPaymentFileId { get; set; }
            public string OutgoingPaymentFilePdfArchiveKey { get; set; }
            public DateTime? OutgoingPaymentFileTransactionDate { get; set; }

            public int? ReminderId { get; set; }
            public DateTime? ReminderDate { get; set; }
            public int? ReminderNumber { get; set; }
            public DateTime? ReminderNotificationDueDate { get; set; }

            public int? WriteoffId { get; set; }
            public DateTime? WriteoffTransactionDate { get; set; }

            public int? BookKeepingFileId { get; set; }
            public string BookKeepingFileXlsArchiveKey { get; set; }
            public DateTime? BookKeepingFileTransactionDate { get; set; }

            public int? PaymentFreeMonthId { get; set; }
            public DateTime? PaymentFreeMonthForDueDate { get; set; }
        }
    }
}