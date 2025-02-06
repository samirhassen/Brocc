using nCredit.DomainModel;
using NTech.Core.Credit.Shared.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NTech.Core.Credit.Shared.BusinessEvents.Utilities
{
    public class NotificationAmountTextPrintContextModel
    {
        public string uniqueId { get; set; }
        public string text { get; set; }
        public bool isBuiltinCapital { get; set; }
        public bool isBuiltinInterest { get; set; }
        public bool isBuiltinNotificationFee { get; set; }
        public bool isBuiltinReminderFee { get; set; }
        public bool isCustom { get; set; }

        public static List<NotificationAmountTextPrintContextModel> GetAmountTypes(List<PaymentOrderUiItem> paymentOrder) => 
            GetAmountTextItems<NotificationAmountTextPrintContextModel>(paymentOrder, (_, __) => { });

        public static List<TItem> GetAmountTextItems<TItem>(List<PaymentOrderUiItem> paymentOrder, Action<PaymentOrderUiItem, TItem> appendExtras) where TItem : NotificationAmountTextPrintContextModel, new()
        {
            return paymentOrder.Select(x =>
            {
                var item = new TItem
                {
                    text = x.Text,
                    uniqueId = x.UniqueId,
                    isCustom = !x.OrderItem.IsBuiltin,
                    isBuiltinCapital = x.OrderItem.IsCreditDomainModelAmountType(CreditDomainModel.AmountType.Capital),
                    isBuiltinInterest = x.OrderItem.IsCreditDomainModelAmountType(CreditDomainModel.AmountType.Interest),
                    isBuiltinNotificationFee = x.OrderItem.IsCreditDomainModelAmountType(CreditDomainModel.AmountType.NotificationFee),
                    isBuiltinReminderFee = x.OrderItem.IsCreditDomainModelAmountType(CreditDomainModel.AmountType.ReminderFee)
                };
                appendExtras(x, item);
                return item;
            }).ToList(); ;
        }
    }

    public class NotificationAmountPrintContextModel : NotificationAmountTextPrintContextModel
    {
        public string amount { get; set; }
        public bool? nonZero { get; set; }

        public static List<NotificationAmountPrintContextModel> GetAmountsListPrintContext(CreditNotificationDomainModel notification, List<PaymentOrderUiItem> paymentOrder, CultureInfo formattingCulture, ICoreClock clock) =>
            GetAmountsListPrintContext(paymentOrder, formattingCulture, x => notification.GetRemainingBalance(clock.Today, x.OrderItem));

        public static List<NotificationAmountPrintContextModel> GetAmountsListPrintContext(List<PaymentOrderUiItem> paymentOrder, CultureInfo formattingCulture, Func<PaymentOrderUiItem, decimal> getAmount) =>
            GetAmountTextItems<NotificationAmountPrintContextModel>(paymentOrder, (orderItem, printItem) =>
            {
                var amount = getAmount(orderItem);
                printItem.amount = amount.ToString("C", formattingCulture);
                printItem.nonZero = amount != 0m ? true : new bool?();
            });
    }
}
