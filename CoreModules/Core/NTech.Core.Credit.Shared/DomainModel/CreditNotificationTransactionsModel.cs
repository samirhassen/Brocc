using nCredit.DomainModel;
using nCredit;
using NTech.Core.Credit.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.Credit.Shared.DomainModel
{
    public class CreditNotificationTransactionsModel
    {
        protected readonly List<PaymentOrderItem> paymentOrder;
        protected readonly List<AccountTransaction> transactions;

        public CreditNotificationTransactionsModel(IEnumerable<AccountTransaction> transactions, List<PaymentOrderItem> paymentOrder)
        {
            this.transactions = transactions.ToList();
            this.paymentOrder = paymentOrder;
        }

        public static CreditNotificationTransactionsModel AppendTransactions(CreditNotificationTransactionsModel source, IEnumerable<AccountTransaction> transactions) =>
            new CreditNotificationTransactionsModel(source.transactions.Concat(transactions), source.paymentOrder);

        public DateTime? GetLastPaymentTransactionDate(DateTime transactionDate)
        {
            return GetTransactions(transactionDate).Where(x => IsIncomingPaymentTransaction(x)).Select(x => (DateTime?)x.TransactionDate).Max();
        }

        public decimal GetInitialAmount(DateTime transactionDate)
        {
            return paymentOrder.Aggregate(0m, (acc, x) => acc + GetInitialAmount(transactionDate, x));
        }

        public decimal GetInitialAmount(DateTime transactionDate, PaymentOrderItem paymentOrderItem)
        {
            if (paymentOrderItem.IsBuiltin)
                return GetInitialAmount(transactionDate, paymentOrderItem.GetBuiltinAmountType());
            else
                return GetTransactions(transactionDate)
                        .Where(x =>
                            x.BusinessEvent.EventType == BusinessEventType.NewNotification.ToString() &&
                            x.AccountCode == TransactionAccountType.NotificationCost.ToString() &&
                            x.SubAccountCode == paymentOrderItem.Code)
                        .Aggregate(0m, (acc, x) => acc + x.Amount);
        }

        private decimal GetInitialAmount(DateTime transactionDate, CreditDomainModel.AmountType amountType)
        {
            if (amountType == CreditDomainModel.AmountType.Capital)
            {
                return -GetTransactions(transactionDate)
                        .Where(x =>
                            x.BusinessEvent.EventType == BusinessEventType.NewNotification.ToString() &&
                            x.AccountCode == TransactionAccountType.NotNotifiedCapital.ToString())
                        .Aggregate(0m, (acc, x) => acc + x.Amount);
            }
            else
            {
                var code = CreditDomainModel.MapNonCapitalAmountTypeToAccountType(amountType).ToString();
                var amt = GetTransactions(transactionDate)
                    .Where(x =>
                    (x.BusinessEvent.EventType == BusinessEventType.NewNotification.ToString() || x.BusinessEvent.EventType == BusinessEventType.NewReminder.ToString()) &&
                    x.AccountCode == code)
                    .Aggregate(0m, (acc, x) => acc + x.Amount);
                return amt;
            }
        }

        public decimal GetWrittenOffAmount(DateTime transactionDate)
        {
            return paymentOrder.Aggregate(0m, (acc, x) => acc + GetWrittenOffAmount(transactionDate, x));
        }

        public decimal GetWrittenOffAmount(DateTime transactionDate, PaymentOrderItem paymentOrderItem)
        {
            if (paymentOrderItem.IsBuiltin)
                return GetWrittenOffAmount(transactionDate, paymentOrderItem.GetBuiltinAmountType());
            else
                return -GetTransactions(transactionDate)
                    .Where(x =>
                        IsWriteOffTransaction(x) &&
                        x.AccountCode == TransactionAccountType.NotificationCost.ToString())
                        .Aggregate(0m, (acc, x) => acc + x.Amount);
        }

        private decimal GetWrittenOffAmount(DateTime transactionDate, CreditDomainModel.AmountType amountType)
        {
            if (amountType == CreditDomainModel.AmountType.Capital)
            {
                return GetTransactions(transactionDate)
                    .Where(x =>
                        IsWriteOffTransaction(x) &&
                        x.AccountCode == TransactionAccountType.NotNotifiedCapital.ToString())
                        .Aggregate(0m, (acc, x) => acc + x.Amount);
            }
            else
            {
                var code = CreditDomainModel.MapNonCapitalAmountTypeToAccountType(amountType).ToString();
                return -GetTransactions(transactionDate)
                    .Where(x =>
                        IsWriteOffTransaction(x) &&
                        x.AccountCode == code)
                        .Aggregate(0m, (acc, x) => acc + x.Amount);
            }
        }

        public decimal GetPaidAmount(DateTime transactionDate)
        {
            return paymentOrder.Aggregate(0m, (acc, x) => acc + GetPaidAmount(transactionDate, x));
        }

        public decimal GetRemainingBalance(DateTime transactionDate)
        {
            return GetInitialAmount(transactionDate) - GetPaidAmount(transactionDate) - GetWrittenOffAmount(transactionDate);
        }

        public decimal GetRemainingBalance(DateTime transactionDate, CreditDomainModel.AmountType amountType)
        {
            var initialAmount = GetInitialAmount(transactionDate, amountType);
            var paidAmount = GetPaidAmount(transactionDate, amountType);
            var writtenOffAmount = GetWrittenOffAmount(transactionDate, amountType);
            return initialAmount - paidAmount - writtenOffAmount;
        }

        public decimal GetRemainingBalance(DateTime transactionDate, PaymentOrderItem paymentOrderItem)
        {
            var initialAmount = GetInitialAmount(transactionDate, paymentOrderItem);
            var paidAmount = GetPaidAmount(transactionDate, paymentOrderItem);
            var writtenOffAmount = GetWrittenOffAmount(transactionDate, paymentOrderItem);
            return initialAmount - paidAmount - writtenOffAmount;
        }

        public decimal GetPaidAmount(DateTime transactionDate, PaymentOrderItem paymentOrderItem)
        {
            if (paymentOrderItem.IsBuiltin)
                return GetPaidAmount(transactionDate, paymentOrderItem.GetBuiltinAmountType());
            else
                return -GetTransactions(transactionDate)
                    .Where(x =>
                    IsIncomingPaymentTransaction(x) &&
                    x.AccountCode == TransactionAccountType.NotificationCost.ToString() &&
                    x.SubAccountCode == paymentOrderItem.Code)
                    .Aggregate(0m, (acc, x) => acc + x.Amount);
        }

        //Remember: NotNotifiedCapital is notified and written off while actual capital debt is paid
        public static TransactionAccountType MapNonCapitalAmountTypeToAccountType(CreditDomainModel.AmountType t)
        {
            switch (t)
            {
                case CreditDomainModel.AmountType.Interest: return TransactionAccountType.InterestDebt;
                case CreditDomainModel.AmountType.NotificationFee: return TransactionAccountType.NotificationFeeDebt;
                case CreditDomainModel.AmountType.ReminderFee: return TransactionAccountType.ReminderFeeDebt;
                default: throw new NotImplementedException();
            }
        }

        private decimal GetPaidAmount(DateTime transactionDate, CreditDomainModel.AmountType amountType)
        {
            string code;
            if (amountType == CreditDomainModel.AmountType.Capital)
            {
                code = TransactionAccountType.CapitalDebt.ToString();
            }
            else
            {
                code = CreditDomainModel.MapNonCapitalAmountTypeToAccountType(amountType).ToString();
            }
            return -GetTransactions(transactionDate)
                .Where(x =>
                IsIncomingPaymentTransaction(x) &&
                x.AccountCode == code)
                .Aggregate(0m, (acc, x) => acc + x.Amount);
        }

        private bool IsIncomingPaymentTransaction(AccountTransaction transaction) => transaction.IncomingPayment != null || transaction.IncomingPaymentId.HasValue;
        private bool IsWriteOffTransaction(AccountTransaction transaction) => transaction.Writeoff != null || transaction.WriteoffId.HasValue;

        private IEnumerable<AccountTransaction> GetTransactions(DateTime transactionDate)
        {
            return transactions.Where(x => x.TransactionDate <= transactionDate);
        }
    }
}