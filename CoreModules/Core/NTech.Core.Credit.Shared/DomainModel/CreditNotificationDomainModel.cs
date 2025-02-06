using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Credit.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.DomainModel
{
    public class CreditNotificationDomainModel : CreditNotificationTransactionsModel
    {
        private DateTime notificationDate;
        private DateTime dueDate;
        private int notificationId;
        private string creditNr;
        private string ocrPaymentReference;
        private readonly DateTime? closedTransactionDate;

        private CreditNotificationDomainModel(int id, List<AccountTransaction> transactions, DateTime dueDate, DateTime notificationDate, string creditNr, string ocrPaymentReference, DateTime? closedTransactionDate,
            List<PaymentOrderItem> paymentOrder) : base(transactions, paymentOrder)
        {
            this.dueDate = dueDate;
            this.notificationDate = notificationDate;
            this.creditNr = creditNr;
            this.notificationId = id;
            this.ocrPaymentReference = ocrPaymentReference;
            this.closedTransactionDate = closedTransactionDate;
        }

        private static IEnumerable<IEnumerable<T>> SplitIntoGroupsOfN<T>(T[] array, int n)
        {
            for (var i = 0; i < (float)array.Length / n; i++)
            {
                yield return array.Skip(i * n).Take(n);
            }
        }

        public static Dictionary<string, Dictionary<int, CreditNotificationDomainModel>> CreateForSeveralCredits(ISet<string> creditNrs, ICreditContextExtended context,
            List<PaymentOrderItem> paymentOrder, bool onlyFetchOpen)
        {
            var dd = new Dictionary<string, Dictionary<int, CreditNotificationDomainModel>>();

            foreach (var g in SplitIntoGroupsOfN(creditNrs.ToArray(), 300))
            {
                var nBase = context.CreditNotificationHeadersQueryable;
                if (onlyFetchOpen)
                    nBase = nBase.Where(x => !x.ClosedTransactionDate.HasValue);

                var h = nBase
                    .Where(x => g.Contains(x.CreditNr))
                    .Select(x => new { x.DueDate, x.NotificationDate, x.Id, x.CreditNr, x.OcrPaymentReference, x.ClosedTransactionDate })
                    .ToDictionary(x => x.Id, x => x);

                var notificationIds = h.Keys.ToList();
                var trs = context
                    .TransactionsIncludingBusinessEventQueryable
                    .Where(x => x.CreditNotificationId.HasValue && notificationIds.Contains(x.CreditNotificationId.Value))
                    .ToList();

                foreach (var creditTransactions in trs.GroupBy(x => x.CreditNr))
                {
                    var d = new Dictionary<int, CreditNotificationDomainModel>();
                    foreach (var id in creditTransactions.Where(x => x.CreditNotificationId.HasValue).Select(x => x.CreditNotificationId.Value).Distinct())
                    {
                        d[id] = new CreditNotificationDomainModel(id, creditTransactions.Where(x => x.CreditNotificationId == id).ToList(), h[id].DueDate, h[id].NotificationDate, h[id].CreditNr, h[id].OcrPaymentReference,
                            h[id].ClosedTransactionDate, paymentOrder);
                    }
                    dd[creditTransactions.Key] = d;
                }
            }

            foreach (var notNotifiedCreditNr in creditNrs.Where(x => !dd.ContainsKey(x)).ToList())
            {
                dd[notNotifiedCreditNr] = new Dictionary<int, CreditNotificationDomainModel>();
            }

            return dd;
        }

        public static Dictionary<int, CreditNotificationDomainModel> CreateForCredit(string creditNr, ICreditContextExtended context, List<PaymentOrderItem> paymentOrder, bool onlyFetchOpen)
        {
            var trs = context
                .TransactionsIncludingBusinessEventQueryable
                .Where(x => x.CreditNotification.CreditNr == creditNr);

            var nBase = context.CreditNotificationHeadersQueryable;
            if (onlyFetchOpen)
                nBase = nBase.Where(x => !x.ClosedTransactionDate.HasValue);

            var h = nBase
                .Where(x => x.CreditNr == creditNr)
                .Select(x => new { x.DueDate, x.NotificationDate, x.Id, x.CreditNr, x.OcrPaymentReference, x.ClosedTransactionDate })
                .ToDictionary(x => x.Id, x => x);

            var d = new Dictionary<int, CreditNotificationDomainModel>();
            foreach (var id in h.Keys)
            {
                d[id] = new CreditNotificationDomainModel(id, trs.Where(x => x.CreditNotificationId == id).ToList(), h[id].DueDate, h[id].NotificationDate, h[id].CreditNr, h[id].OcrPaymentReference, h[id].ClosedTransactionDate, paymentOrder);
            }
            return d;
        }

        public static Dictionary<int, CreditNotificationDomainModel> CreateForNotifications(List<int> notificationIds, ICreditContextExtended context, List<PaymentOrderItem> paymentOrder)
        {
            var trs = context
                .TransactionsIncludingBusinessEventQueryable
                .Where(x => notificationIds.Contains(x.CreditNotification.Id));

            var h = context
                .CreditNotificationHeadersQueryable
                .Where(x => notificationIds.Contains(x.Id))
                .Select(x => new { x.DueDate, x.NotificationDate, x.Id, x.CreditNr, x.OcrPaymentReference, x.ClosedTransactionDate })
                .ToDictionary(x => x.Id, x => x);

            var d = new Dictionary<int, CreditNotificationDomainModel>();
            foreach (var id in h.Keys)
            {
                d[id] = new CreditNotificationDomainModel(id, trs.Where(x => x.CreditNotificationId == id).ToList(), h[id].DueDate, h[id].NotificationDate, h[id].CreditNr, h[id].OcrPaymentReference, h[id].ClosedTransactionDate, paymentOrder);
            }
            return d;
        }

        public static CreditNotificationDomainModel CreateForSingleNotification(int notificationId, ICreditContextExtended context, List<PaymentOrderItem> paymentOrder)
        {
            var trs = context
                .TransactionsIncludingBusinessEventQueryable
                .Where(x => x.CreditNotificationId == notificationId);

            var h = context.CreditNotificationHeadersQueryable.Where(x => x.Id == notificationId).Select(x => new { x.DueDate, x.NotificationDate, x.CreditNr, x.OcrPaymentReference, x.ClosedTransactionDate }).Single();

            return new CreditNotificationDomainModel(notificationId, trs.ToList(), h.DueDate, h.NotificationDate, h.CreditNr, h.OcrPaymentReference, h.ClosedTransactionDate, paymentOrder);
        }

        public DateTime DueDate
        {
            get
            {
                return this.dueDate;
            }
        }

        public DateTime NotificationDate
        {
            get
            {
                return this.notificationDate;
            }
        }

        public string CreditNr
        {
            get
            {
                return this.creditNr;
            }
        }

        public int NotificationId
        {
            get
            {
                return this.notificationId;
            }
        }

        public string OcrPaymentReference
        {
            get
            {
                return this.ocrPaymentReference;
            }
        }

        public class PaymentSummary
        {
            public int PaymentId { get; set; }
            public DateTime TransactionDate { get; set; }

            public decimal Amount { get; set; }
        }

        public IList<PaymentSummary> GetPayments(DateTime date)
        {
            return GetTransactions(date)
                .Where(x => x.IncomingPaymentId.HasValue)
                .GroupBy(x => x.IncomingPaymentId.Value)
                .Select(x => new PaymentSummary
                {
                    PaymentId = x.Key,
                    TransactionDate = x.Select(y => y.TransactionDate).First(),
                    Amount = -x.Sum(y => y.Amount)
                }).ToList();
        }

        private static int NrOfTimesDayOfMonthPassedBetween(DateTime from, DateTime to, int dayInMonth)
        {
            if (to < from)
                throw new Exception("to < from makes no sense");

            var d = from;
            var count = 0;
            while (d < to)
            {
                if (d.Day == dayInMonth)
                    count++;
                d = d.AddDays(1);
            }

            return count;
        }

        public int GetNrOfPassedDueDatesWithoutFullPaymentSinceNotification(DateTime transactionDate)
        {
            var minTrDate = transactions.Where(x => x.TransactionDate <= transactionDate).Select(x => x.TransactionDate).OrderBy(x => x).Select(x => (DateTime?)x).FirstOrDefault();
            if (!minTrDate.HasValue)
                return 0; //Paid on or (which should not be possible) before the notification date

            var isPaid = GetRemainingBalance(transactionDate) <= 0m;

            if (isPaid)
                return 0;
            else
                return NrOfTimesDayOfMonthPassedBetween(minTrDate.Value, transactionDate, dueDate.Day);
        }

        public int GetNrOfPassedDaysWithoutFullPaymentSinceNotification(DateTime transactionDate)
        {
            var minTrDate = transactions.Where(x => x.TransactionDate <= transactionDate).Select(x => x.TransactionDate).OrderBy(x => x).FirstOrDefault();

            if (minTrDate == default)
            {
                return 0; // No transactions on or before the notification date
            }

            if (GetRemainingBalance(transactionDate) <= 0m)
            {
                return 0; // Fully paid since the notification date
            }

            TimeSpan timeSpan = transactionDate.Date - minTrDate.Date;
            return timeSpan.Days;
        }

        /// <summary>
        /// If transactionDate exists meanst that notfication is payed. isopen=true when not payed. 
        /// </summary>
        /// <param name="transactionDate"></param>
        /// <returns></returns>
        public bool IsOpen(DateTime transactionDate)
        {
            return !GetClosedDate(transactionDate).HasValue;

        }

        public DateTime? GetClosedDate(DateTime transactionDate, bool onlyUseClosedDate = false)
        {
            if (closedTransactionDate.HasValue)
                return closedTransactionDate;

            if (onlyUseClosedDate) //Used to make tests more strict than reality. We should really remove the transaction fallback here and only use closed date everywhere.
                return null;

            /* The wierd logic here is the way we did it before we had an explicit stored ClosedDate in CreditNotificationHeader.
             * It's likely the code below could just be replaced with return null.
             */
            var maxDate = GetTransactions(transactionDate).Where(x => x.IncomingPaymentId.HasValue).Select(x => (DateTime?)x.TransactionDate).Max();
            if (!maxDate.HasValue)
                return null;

            var b = GetRemainingBalance(maxDate.Value);
            if (b <= 0m)
            {
                return maxDate.Value;
            }
            else
            {
                return null;
            }
        }

        public class NotificationListModel
        {
            public int Id { get; set; }
            public DateTime DueDate { get; set; }
            public string CreditNr { get; set; }
            public decimal InitialAmount { get; set; }
            public decimal WrittenOffAmount { get; set; }
            public decimal PaidAmount { get; set; }
            public DateTime? LastPaidDate { get; set; }
            public bool IsPaid { get; set; }
            public bool IsOverDue { get; set; }
            public int CurrentNrOfPassedDueDatesWithoutFullPayment { get; set; }
            public int? AtPaymentNrOfPassedDueDatesWithoutFullPayment { get; set; }
        }

        public static IList<NotificationListModel> GetNotificationListModel(ICreditContextExtended context, DateTime date, string creditNr, IDictionary<int, CreditNotificationDomainModel> modelsbyNotificationId)
        {
            Func<CreditNotificationDomainModel, int?> atPaymentNrOfPassedDueDatesWithoutFullPayment = m =>
            {
                if (m.GetRemainingBalance(date) > 0m)
                {
                    return null;
                }
                else
                {
                    var closedDate = m.GetClosedDate(date);
                    if (!closedDate.HasValue)
                        return null;
                    else
                        return m.GetNrOfPassedDueDatesWithoutFullPaymentSinceNotification(closedDate.Value.AddDays(-1));
                }
            };

            return context
                .CreditNotificationHeadersQueryable
                .Where(x => x.CreditNr == creditNr)
                .Select(x => new
                {
                    Id = x.Id,
                    x.CreditNr,
                    x.DueDate
                })
                .ToList()
                .OrderByDescending(x => x.DueDate)
                .Select(x => new
                {
                    x.Id,
                    x.DueDate,
                    x.CreditNr,
                    InitialAmount = modelsbyNotificationId[x.Id].GetInitialAmount(date),
                    WrittenOffAmount = modelsbyNotificationId[x.Id].GetWrittenOffAmount(date),
                    PaidAmount = modelsbyNotificationId[x.Id].GetPaidAmount(date),
                    LastPaidDate = modelsbyNotificationId[x.Id].GetLastPaymentTransactionDate(date)
                })
                .ToList()
                .Select(x => new
                {
                    x.Id,
                    x.DueDate,
                    x.CreditNr,
                    x.InitialAmount,
                    x.WrittenOffAmount,
                    x.PaidAmount,
                    x.LastPaidDate,
                    IsPaid = modelsbyNotificationId[x.Id].GetRemainingBalance(date) <= 0m
                })
                .ToList()
                .Select(x => new NotificationListModel
                {
                    Id = x.Id,
                    DueDate = x.DueDate,
                    CreditNr = x.CreditNr,
                    InitialAmount = x.InitialAmount,
                    WrittenOffAmount = x.WrittenOffAmount,
                    PaidAmount = x.PaidAmount,
                    LastPaidDate = x.LastPaidDate,
                    IsPaid = x.IsPaid,
                    IsOverDue = (date > x.DueDate && !x.IsPaid) || (x.IsPaid && x.LastPaidDate.HasValue && x.LastPaidDate.Value > x.DueDate),
                    CurrentNrOfPassedDueDatesWithoutFullPayment = modelsbyNotificationId[x.Id].GetNrOfPassedDueDatesWithoutFullPaymentSinceNotification(date),
                    AtPaymentNrOfPassedDueDatesWithoutFullPayment = atPaymentNrOfPassedDueDatesWithoutFullPayment(modelsbyNotificationId[x.Id])
                })
                .ToList();
        }

        private IEnumerable<AccountTransaction> GetTransactions(DateTime transactionDate)
        {
            return transactions.Where(x => x.TransactionDate <= transactionDate);
        }
    }
}