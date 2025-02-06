using nCredit.DomainModel;
using NTech.Core.Credit.Shared.Services;
using System;
using System.Collections.Generic;

namespace nCredit.DbModel.BusinessEvents
{
    public class NotificationPaymentPlacementModel : INotificationPaymentPlacementModel
    {
        private Dictionary<int, CreditNotificationDomainModel> notifications;
        private int notificationId;
        private readonly DateTime transactionDate;
        private readonly Action<PaymentOrderItem, decimal, int?> observePaymentPlacement;

        public NotificationPaymentPlacementModel(Dictionary<int, CreditNotificationDomainModel> notifications, int notificationId, DateTime transactionDate, Action<PaymentOrderItem, decimal, int?> observePaymentPlacement = null)
        {
            this.notificationId = notificationId;
            this.transactionDate = transactionDate;
            this.observePaymentPlacement = observePaymentPlacement;
            this.notifications = notifications;
        }

        public DateTime DueDate
        {
            get
            {
                return notifications[notificationId].DueDate;
            }
        }

        public int NotificationId
        {
            get
            {
                return notifications[notificationId].NotificationId;
            }
        }

        public decimal GetRemainingBalance(CreditDomainModel.AmountType amountType)
        {
            return notifications[notificationId].GetRemainingBalance(transactionDate, amountType);
        }

        public decimal GetRemainingBalance(PaymentOrderItem item)
        {
            return notifications[notificationId].GetRemainingBalance(transactionDate, item);
        }

        public decimal GetTotalRemainingBalance()
        {
            return notifications[notificationId].GetRemainingBalance(transactionDate);
        }

        public void PlacePayment(CreditDomainModel.AmountType amountType, decimal amount)
        {
            observePaymentPlacement?.Invoke(new PaymentOrderItem
            {
                IsBuiltin = true,
                Code = amountType.ToString()
            }, amount, this.notificationId);
        }

        public void PlacePayment(PaymentOrderItem item, decimal amount)
        {
            observePaymentPlacement?.Invoke(item, amount, this.notificationId);
        }
    }
}