using NTech.Core.Credit.Shared.Database;
using System;
using System.Data.Entity;
using System.Linq;

namespace nCredit.DbModel.DomainModel
{

    public class CurrentNotificationStateServiceLegacy : ICurrentNotificationStateService
    {
        public static IQueryable<CurrentNotificationState> GetCurrentOpenNotificationsStateQuery(CreditContext context, DateTime today)
        {
            return context
                .CreditNotificationHeaders
                .Where(x => !x.ClosedTransactionDate.HasValue)
                .Select(x => new
                {
                    NotificationId = x.Id,
                    x.CreditNr,
                    x.NotificationDate,
                    x.DueDate,
                    NrOfDaysOverdue = today < x.DueDate ? 0 : DbFunctions.DiffDays(x.DueDate, today) ?? 0,
                    NrOfPassedDueDatesWithoutFullPaymentSinceNotification =
                        today <= x.DueDate ? new int?(0) : DbFunctions.DiffMonths(x.DueDate, today) + (today.Day <= x.DueDate.Day ? 0 : 1),
                    InitialCapitalAmount = -x.Transactions.Where(y => y.BusinessEvent.EventType == BusinessEventType.NewNotification.ToString() && y.AccountCode == TransactionAccountType.NotNotifiedCapital.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                    PaidCapitalAmount = -x.Transactions.Where(y => y.IncomingPaymentId.HasValue && y.AccountCode == TransactionAccountType.CapitalDebt.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                    WrittenOffCapitalAmount = x.Transactions.Where(y => y.WriteoffId.HasValue && y.AccountCode == TransactionAccountType.NotNotifiedCapital.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                    InitialNonCapitalAmount = x.Transactions.Where(y => (y.BusinessEvent.EventType == BusinessEventType.NewNotification.ToString() || y.BusinessEvent.EventType == BusinessEventType.NewReminder.ToString()) && y.AccountCode != TransactionAccountType.NotNotifiedCapital.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                    PaidNonCapitalAmount = -x.Transactions.Where(y => y.IncomingPaymentId.HasValue && y.AccountCode != TransactionAccountType.CapitalDebt.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                    WrittenOffNonCapitalAmount = -x.Transactions.Where(y => y.WriteoffId.HasValue && y.AccountCode != TransactionAccountType.NotNotifiedCapital.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                    RemainingInterestAmount = x.Transactions.Where(y => y.AccountCode == TransactionAccountType.InterestDebt.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                    x.OcrPaymentReference
                })
                .Select(x => new
                {
                    NotificationId = x.NotificationId,
                    CreditNr = x.CreditNr,
                    NotificationDate = x.NotificationDate,
                    NrOfPassedDueDatesWithoutFullPaymentSinceNotification = x.NrOfPassedDueDatesWithoutFullPaymentSinceNotification.Value,
                    x.NrOfDaysOverdue,
                    DueDate = x.DueDate,
                    InitialAmount = x.InitialNonCapitalAmount + x.InitialCapitalAmount,
                    PaidAmount = x.PaidCapitalAmount + x.PaidNonCapitalAmount,
                    WrittenOffAmount = x.WrittenOffCapitalAmount + x.WrittenOffNonCapitalAmount,
                    x.RemainingInterestAmount,
                    x.OcrPaymentReference
                })
                .Select(x => new CurrentNotificationState
                {
                    NotificationId = x.NotificationId,
                    CreditNr = x.CreditNr,
                    InitialAmount = x.InitialAmount,
                    NotificationDate = x.NotificationDate,
                    DueDate = x.DueDate,
                    NrOfPassedDueDatesWithoutFullPaymentSinceNotification = x.NrOfPassedDueDatesWithoutFullPaymentSinceNotification,
                    NrOfDaysOverdue = x.NrOfDaysOverdue,
                    RemainingAmount = x.InitialAmount - x.PaidAmount - x.WrittenOffAmount,
                    RemainingInterestAmount = x.RemainingInterestAmount,
                    OcrPaymentReference = x.OcrPaymentReference
                });
        }

        public IQueryable<CurrentNotificationState> GetCurrentOpenNotificationsStateQuery(ICreditContextExtended context) =>
            GetCurrentOpenNotificationsStateQuery((CreditContext)context, context.CoreClock.Today);
    }
}