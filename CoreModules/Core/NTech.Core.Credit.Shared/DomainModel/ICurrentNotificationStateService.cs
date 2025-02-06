using NTech.Core.Credit.Shared.Database;
using System;
using System.Linq;

namespace nCredit.DbModel.DomainModel
{
    public interface ICurrentNotificationStateService
    {
        IQueryable<CurrentNotificationState> GetCurrentOpenNotificationsStateQuery(ICreditContextExtended context);
    }

    public class CurrentNotificationState
    {
        public decimal InitialAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal RemainingInterestAmount { get; set; }
        public string CreditNr { get; set; }
        public DateTime NotificationDate { get; set; }
        public DateTime DueDate { get; set; }
        public int NrOfPassedDueDatesWithoutFullPaymentSinceNotification { get; set; }
        public int NrOfDaysOverdue { get; set; }
        public string OcrPaymentReference { get; set; }
        public int NotificationId { get; set; }
    }
}