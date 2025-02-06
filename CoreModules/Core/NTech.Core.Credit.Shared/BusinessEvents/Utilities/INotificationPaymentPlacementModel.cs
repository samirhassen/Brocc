using nCredit.DomainModel;
using NTech.Core.Credit.Shared.Services;
using System;

namespace nCredit.DbModel.BusinessEvents
{
    public interface INotificationPaymentPlacementModel
    {
        DateTime DueDate { get; }
        int NotificationId { get; }
        decimal GetRemainingBalance(CreditDomainModel.AmountType amountType);
        decimal GetRemainingBalance(PaymentOrderItem item);
        decimal GetTotalRemainingBalance();
        void PlacePayment(PaymentOrderItem item, decimal amount);
    }
}