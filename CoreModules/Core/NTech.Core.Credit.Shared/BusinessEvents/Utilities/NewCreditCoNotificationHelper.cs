using nCredit.Code.Services;
using nCredit.DomainModel;
using NTech.Core.Credit.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class NewCreditCoNotificationHelper
    {
        public static Dictionary<string, NewCreditNotificationBusinessEventManager.NotificationAmountsModel> GetAmounts(
            List<UngroupedNotificationService.CreditNotificationStatusCommon> credits,
                   Func<string, CreditDomainModel> getCreditModelByCreditNr,
                   Func<string, Dictionary<int, CreditNotificationDomainModel>> getOldNotificationsModelByCreditNr,
                   DateTime today,
                   DateTime sharedDueDate,
                   List<PaymentOrderUiItem> paymentOrder)
        {
            var r = new Dictionary<string, NewCreditNotificationBusinessEventManager.NotificationAmountsModel>();
            foreach (var credit in credits)
            {
                //NOTE: model and notificationsModel here include the current notification unlike for the main credit since the child credit has already been notified
                var notificationsModel = getOldNotificationsModelByCreditNr(credit.CreditNr);
                var currentNotification = notificationsModel.Single(x => x.Value.DueDate == sharedDueDate).Value;
                var oldNotifications = notificationsModel.Where(x => x.Value.DueDate != sharedDueDate).Select(x => x.Value).ToList();
                var currentCreditModel = getCreditModelByCreditNr(credit.CreditNr);

                Func<CreditDomainModel.AmountType, decimal> getOverdueAmount = c =>
                   oldNotifications.Select(x => x.GetRemainingBalance(today, c)).Sum();

                var m = new NewCreditNotificationBusinessEventManager.NotificationAmountsModel
                {
                    OverdueCapitalAmount = getOverdueAmount(CreditDomainModel.AmountType.Capital),
                    OverdueInterestAmount = getOverdueAmount(CreditDomainModel.AmountType.Interest),
                    OverdueNotificationFeeAmount = getOverdueAmount(CreditDomainModel.AmountType.NotificationFee),
                    OverdueReminderFeeAmount = getOverdueAmount(CreditDomainModel.AmountType.ReminderFee),
                    CapitalAmount = currentNotification.GetRemainingBalance(today, CreditDomainModel.AmountType.Capital),
                    InterestAmount = currentNotification.GetRemainingBalance(today, CreditDomainModel.AmountType.Interest),
                    NotificationFeeAmount = currentNotification.GetRemainingBalance(today, CreditDomainModel.AmountType.NotificationFee),
                    TotalUnpaidcreditCapitalAmount = currentCreditModel.GetBalance(CreditDomainModel.AmountType.Capital, today),
                    CurrentInterestRatePercent = currentCreditModel.GetInterestRatePercent(today),
                    OtherCosts = currentCreditModel.GetNotNotifiedNotificationCosts(today),
                    OverdueOtherCosts = paymentOrder
                        .Select(x => x.OrderItem)
                        .Where(x => !x.IsBuiltin)
                        .ToDictionary(
                            x => x.Code,
                            x => oldNotifications.Select(y => y.GetRemainingBalance(today, x)).Sum())
                };

                m.TotalOverdueAmount = m.OverdueCapitalAmount + m.OverdueInterestAmount + m.OverdueNotificationFeeAmount + m.OverdueReminderFeeAmount + m.OverdueOtherCosts.Values.Sum();
                r[credit.CreditNr] = m;
            }
            return r;
        }
    }
}