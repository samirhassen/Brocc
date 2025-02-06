using nCredit.DomainModel;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class NotificationWriteOffBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        private readonly bool isForMortgageLoan;
        private readonly CreditContextFactory creditContextFactory;
        private readonly ICreditEnvSettings envSettings;
        private readonly PaymentOrderService paymentOrderService;

        public NotificationWriteOffBusinessEventManager(INTechCurrentUserMetadata currentUser, bool isForMortgageLoan, CreditContextFactory creditContextFactory,
            ICoreClock clock, IClientConfigurationCore clientConfiguration, ICreditEnvSettings envSettings, PaymentOrderService paymentOrderService) : base(currentUser, clock, clientConfiguration)
        {
            this.isForMortgageLoan = isForMortgageLoan;
            this.creditContextFactory = creditContextFactory;
            this.envSettings = envSettings;
            this.paymentOrderService = paymentOrderService;
        }

        public class WriteOffInstruction
        {
            public int NotificationId { get; set; }
            public IDictionary<string, decimal> PartialWriteOffAmountTypeUniqueIdsAndAmounts { get; set; }
            public bool? WriteOffEntireNotification { get; set; }
            public IList<string> FullWriteOffAmountTypeUniqueIds { get; set; }
        }

        private void ValidateWriteOffInstructions(IList<WriteOffInstruction> writeOffs, Func<int, string> getErrorTag, List<string> errors)
        {
            foreach (var i in writeOffs)
            {
                if (i == null || i.NotificationId <= 0)
                {
                    errors.Add("Missing NotificationId");
                }
            }
            if (errors.Count > 0)
                return;

            foreach (var i in writeOffs)
            {
                i.FullWriteOffAmountTypeUniqueIds = i.FullWriteOffAmountTypeUniqueIds ?? new List<string>();
                i.PartialWriteOffAmountTypeUniqueIdsAndAmounts = i.PartialWriteOffAmountTypeUniqueIdsAndAmounts ?? new Dictionary<string, decimal>();
                if (i.WriteOffEntireNotification.GetValueOrDefault() && (i.FullWriteOffAmountTypeUniqueIds.Count > 0 || i.PartialWriteOffAmountTypeUniqueIdsAndAmounts.Count > 0))
                {
                    errors.Add($"{getErrorTag(i.NotificationId)}: WriteOffEntireNotification cannot be combined with other options");
                }
            }
            if (errors.Count > 0)
                return;

            if (writeOffs.GroupBy(x => x.NotificationId).Count() != writeOffs.Count)
            {
                errors.Add("There a duplicates of the same notification in the writeOffs-list");
            }
            if (errors.Count > 0)
                return;

            var possiblePaymentOrderItems = paymentOrderService.GetPaymentOrderItems();
            var possibleUniqueIds = possiblePaymentOrderItems.Select(x => x.GetUniqueId()).ToHashSetShared();
            foreach (var i in writeOffs)
            {
                if (i.PartialWriteOffAmountTypeUniqueIdsAndAmounts.Keys.Any(x => !possibleUniqueIds.Contains(x)))
                {
                    errors.Add($"{getErrorTag(i.NotificationId)}: Invalid amountTypes in PartialWriteOffAmountTypesAndAmounts");
                }
                if (i.FullWriteOffAmountTypeUniqueIds.Any(x => !possibleUniqueIds.Contains(x)))
                {
                    errors.Add($"{getErrorTag(i.NotificationId)}: Invalid amountTypes in FullWriteOffAmountTypeUniqueIds");
                }
            }
        }

        public bool TryWriteOffNotifications(IList<WriteOffInstruction> writeOffs, CreditTerminationLettersInactivationBusinessEventManager creditTerminationLettersInactivationBusinessEventManager,
            out List<string> errors, out BusinessEvent evt)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                if (TryWriteOffNotificationsOnly(writeOffs, context, out errors, out evt))
                {
                    context.SaveChanges();

                    var notificationIds = writeOffs.Select(x => x.NotificationId).ToList();
                    var creditNrs = context.CreditNotificationHeadersQueryable.Where(x => notificationIds.Contains(x.Id)).Select(x => x.CreditNr).ToList();
                    creditTerminationLettersInactivationBusinessEventManager.InactivateTerminationLettersWhereNotificationsPaid(context,
                        creditNrs.ToHashSetShared(), businessEvent: evt);

                    context.SaveChanges();

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }


        private bool TryWriteOffNotificationsOnly(IList<WriteOffInstruction> writeOffs, ICreditContextExtended context, out List<string> errors, out BusinessEvent evt)
        {
            var notificationIds = writeOffs.Select(x => x?.NotificationId).Where(x => x.HasValue).Select(x => x.Value).Distinct().ToList();

            var notifications = new Lazy<Dictionary<int, CreditNotificationHeader>>(
                () => context
                    .CreditNotificationHeadersQueryable
                    .Where(x => notificationIds.Contains(x.Id))
                    .ToDictionary(x => x.Id, x => x));

            var notificationDomainModels = new Lazy<Dictionary<int, CreditNotificationDomainModel>>(
                () => CreditNotificationDomainModel.CreateForNotifications(notificationIds, context, paymentOrderService.GetPaymentOrderItems()));

            Func<int, CreditNotificationHeader> getNotification = nid => notifications.Value[nid];
            Func<int, PaymentOrderItem, decimal> getNotificationAmountTypeBalance = (nid, t) => notificationDomainModels.Value[nid].GetRemainingBalance(Clock.Today, t);
            Func<int, string> getCreditNr = nid => getNotification(nid).CreditNr;
            Func<int, decimal> getCreditBalance = nid =>
                {
                    return CreditDomainModel.PreFetchForSingleCredit(getCreditNr(nid), context, envSettings).GetBalance(Clock.Today);
                };

            return TryWriteOffNotifications(writeOffs, getNotificationAmountTypeBalance, getCreditBalance, getCreditNr, getNotification, context, out errors, out evt);
        }

        private bool TryWriteOffNotifications(IList<WriteOffInstruction> writeOffs, Func<int, PaymentOrderItem, decimal> getNotificationAmountTypeBalance, Func<int, decimal> getCreditBalance, Func<int, string> getCreditNr, Func<int, CreditNotificationHeader> getNotification, ICreditContextExtended context, out List<string> errors, out BusinessEvent evt)
        {
            errors = new List<string>();
            evt = this.AddBusinessEvent(BusinessEventType.NotificationWriteOff, context);
            var writeOffHeader = new WriteoffHeader
            {
                BookKeepingDate = evt.BookKeepingDate,
                ChangedById = UserId,
                ChangedDate = Clock.Now,
                InformationMetaData = InformationMetadata,
                TransactionDate = evt.TransactionDate
            };
            context.AddWriteoffHeaders(writeOffHeader);

            Func<int, string> getErrorTag = nid => $"Notification {getNotification(nid).DueDate.ToString("yyyy-MM-dd")}";

            ValidateWriteOffInstructions(writeOffs, getErrorTag, errors);
            if (errors.Count > 0)
                return false;

            var allWriteOffAmounts = new Dictionary<int, IDictionary<string, decimal>>();
            foreach (var i in writeOffs)
            {
                IDictionary<string, decimal> writeOffAmounts = new Dictionary<string, decimal>();
                if (i.WriteOffEntireNotification.GetValueOrDefault())
                {
                    var paymentOrder = paymentOrderService.GetPaymentOrderItems();
                    foreach (var item in paymentOrderService.GetPaymentOrderItems())
                    {
                        var writeOffAmount = getNotificationAmountTypeBalance(i.NotificationId, item);
                        if(writeOffAmount > 0m)
                            writeOffAmounts[item.GetUniqueId()] = writeOffAmount;
                    }
                    if (!paymentOrder.Any(x => x.IsBuiltin))
                        throw new NotImplementedException();
                }
                else
                {
                    foreach (var p in i.PartialWriteOffAmountTypeUniqueIdsAndAmounts)
                    {
                        writeOffAmounts[p.Key] = Math.Round(p.Value, 2);
                    }

                    foreach (var itemUniqueId in i.FullWriteOffAmountTypeUniqueIds)
                    {
                        if (writeOffAmounts.ContainsKey(itemUniqueId))
                        {
                            errors.Add($"{getErrorTag(i.NotificationId)}: {itemUniqueId} is in both the full and partial writeoff list");
                        }
                        else
                        {                            
                            writeOffAmounts[itemUniqueId] = getNotificationAmountTypeBalance(i.NotificationId, paymentOrderService.GetItemByUniqueId(itemUniqueId));
                        }
                    }
                }
                allWriteOffAmounts[i.NotificationId] = writeOffAmounts;
            }
            if (errors.Count > 0)
                return false;

            var trs = new List<AccountTransaction>();
            foreach (var wo in allWriteOffAmounts)
            {
                var notificationId = wo.Key;
                var creditNr = getCreditNr(notificationId);
                if (wo.Value.Any(x => x.Value < 0m))
                {
                    errors.Add($"{getErrorTag(notificationId)}: There are negative writeoff amounts");
                }

                var paymentOrder = paymentOrderService.GetPaymentOrderItems();

                var balanceBefore = paymentOrder.Sum(x => getNotificationAmountTypeBalance(notificationId, x));
                var balanceAfter = balanceBefore - wo.Value.Sum(y => y.Value);
                if (balanceBefore == balanceAfter)
                {
                    errors.Add($"{getErrorTag(notificationId)}: Nothing to write off");
                }
                else if (balanceAfter < 0)
                {
                    errors.Add($"{getErrorTag(notificationId)}: Balance after would be negative");
                }
                else
                {
                    var notification = getNotification(notificationId);
                    var commentText = $"Writeoff on the notification with due date {notification.DueDate.ToString("yyyy-MM-dd")}.";
                    foreach (var wt in wo.Value)
                    {
                        var itemUniqueId = wt.Key;
                        var item = paymentOrderService.GetItemByUniqueId(itemUniqueId);
                        var writeOffAmount = wt.Value;

                        commentText += $" {(item.IsBuiltin ? item.GetBuiltinAmountType().ToString() : item.Code)}: {writeOffAmount.ToString(CommentFormattingCulture)}";

                        if (item.IsCreditDomainModelAmountType(CreditDomainModel.AmountType.Capital))
                        {
                            trs.Add(CreateTransaction( //NOTE: No minus is not an error since not notified rather than capital debt
                                    TransactionAccountType.NotNotifiedCapital, writeOffAmount, evt.BookKeepingDate, evt,
                                    creditNr: creditNr,
                                    writeOff: writeOffHeader,
                                    notificationId: notificationId));
                        }
                        else if(item.IsBuiltin)
                        {
                            var transactionType = CreditDomainModel.MapNonCapitalAmountTypeToAccountType(item.GetBuiltinAmountType());
                            trs.Add(CreateTransaction(
                                transactionType, -writeOffAmount, evt.BookKeepingDate, evt,
                                creditNr: creditNr,
                                writeOff: writeOffHeader,
                                notificationId: notificationId));
                        }
                        else
                        {
                            trs.Add(CreateTransaction(
                                TransactionAccountType.NotificationCost, -writeOffAmount, evt.BookKeepingDate, evt,
                                creditNr: creditNr,
                                writeOff: writeOffHeader,
                                notificationId: notificationId,
                                subAccountCode: item.Code));
                        }
                    }
                    
                    if (balanceAfter == 0m)
                    {
                        var capitalUniqueId = PaymentOrderItem.GetUniqueId(CreditDomainModel.AmountType.Capital);
                        //Flag the notification as closed                        
                        notification.ClosedTransactionDate = evt.TransactionDate;
                        commentText += ". This caused the notification to be closed.";

                        if (getCreditBalance(notificationId) == balanceBefore && !wo.Value.ContainsKey(capitalUniqueId))
                        {
                            //We wrote off all the remaining debt on the credit and no capital was returned to unplace so this credit is now settled
                            errors.Add($"{getErrorTag(notificationId)}: Cannot writeoff since that would writeoff all remaining debt on the credit. Settle instead");
                        }
                    }
                    AddComment(commentText, BusinessEventType.NotificationWriteOff, context, creditNr: creditNr);
                }
            }
            context.AddAccountTransactions(trs.ToArray());

            if (errors.Count > 0)
                return false;

            return errors.Count == 0;
        }
    }
}