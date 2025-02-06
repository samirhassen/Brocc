using nCredit.Code.Services;
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
    public class CreditCorrectAndCloseBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        private readonly CreditContextFactory contextFactory;
        private readonly ICreditEnvSettings envSettings;
        private readonly ICustomerRelationsMergeService customerRelationsMergeService;
        private readonly PaymentOrderService paymentOrderService;

        public CreditCorrectAndCloseBusinessEventManager(INTechCurrentUserMetadata currentUser, CreditContextFactory contextFactory,
            ICoreClock clock, IClientConfigurationCore clientConfiguration, ICreditEnvSettings envSettings, ICustomerRelationsMergeService customerRelationsMergeService,
            PaymentOrderService paymentOrderService) : base(currentUser, clock, clientConfiguration)
        {
            this.contextFactory = contextFactory;
            this.envSettings = envSettings;
            this.customerRelationsMergeService = customerRelationsMergeService;
            this.paymentOrderService = paymentOrderService;
        }

        public bool TryCorrectAndCloseCredit(string creditNr, bool simulateOnly, out decimal writtenOffCapitalAmount, out decimal writtenOffNonCapitalAmount, out string failedMessage)
        {
            if (string.IsNullOrWhiteSpace(creditNr))
            {
                failedMessage = $"creditNr missing";
                writtenOffCapitalAmount = 0m;
                writtenOffNonCapitalAmount = 0m;
                return false;
            }

            using (var context = contextFactory.CreateContext())
            {
                var credit = context.CreditHeadersQueryable.SingleOrDefault(x => x.CreditNr == creditNr);

                if (credit == null)
                {
                    failedMessage = $"No such credit exists";
                    writtenOffCapitalAmount = 0m;
                    writtenOffNonCapitalAmount = 0m;
                    return false;
                }

                var creditModel = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, envSettings);

                var status = creditModel.GetStatus();
                if (status != CreditStatus.Normal)
                {
                    failedMessage = $"Credit has status {status}";
                    writtenOffCapitalAmount = 0m;
                    writtenOffNonCapitalAmount = 0m;
                    return false;
                }

                writtenOffCapitalAmount = creditModel.GetBalance(CreditDomainModel.AmountType.Capital, Clock.Today);

                if (writtenOffCapitalAmount < 0m)
                {
                    failedMessage = $"Credit has negative capital debt";
                    writtenOffCapitalAmount = 0m;
                    writtenOffNonCapitalAmount = 0m;
                    return false;
                }

                var evt = AddBusinessEvent(BusinessEventType.CreditCorrectAndClose, context);

                WriteoffHeader wo = new WriteoffHeader
                {
                    BookKeepingDate = evt.BookKeepingDate,
                    ChangedById = evt.ChangedById,
                    ChangedDate = evt.ChangedDate,
                    InformationMetaData = evt.InformationMetaData,
                    TransactionDate = evt.TransactionDate
                };

                context.AddWriteoffHeaders(wo);

                var capitalWriteOffTransaction = CreateTransaction(
                        TransactionAccountType.CapitalDebt,
                        -writtenOffCapitalAmount,
                        evt.BookKeepingDate,
                        evt,
                        creditNr: creditNr,
                        writeOff: wo);

                context.AddAccountTransactions(capitalWriteOffTransaction);

                var paymentOrder = paymentOrderService.GetPaymentOrderItems();

                var notifications = CreditNotificationDomainModel.CreateForCredit(creditNr, context, paymentOrder, onlyFetchOpen: false);

                var movedBackNotNotifiedCapitalAmount = 0m;
                writtenOffNonCapitalAmount = 0m;
                foreach (var n in notifications.Where(x => !x.Value.GetClosedDate(Clock.Today).HasValue).OrderBy(x => x.Value.DueDate))
                {
                    var notification = n.Value;

                    foreach (var amt in paymentOrder.Where(x => x.IsBuiltin))
                    {
                        if (amt.IsCreditDomainModelAmountType(CreditDomainModel.AmountType.Capital))
                        {
                            var capitalBalance = notification.GetRemainingBalance(Clock.Today, amt);
                            if (capitalBalance > 0m)
                            {
                                //Move it back to not notified capital
                                movedBackNotNotifiedCapitalAmount += capitalBalance;

                                context.AddAccountTransactions(CreateTransaction(
                                    TransactionAccountType.NotNotifiedCapital,
                                    capitalBalance,
                                    evt.BookKeepingDate,
                                    evt,
                                    creditNr: creditNr,
                                    notificationId: notification.NotificationId,
                                    writeOff: wo));
                            }
                        }
                        else
                        {
                            var otherBalance = notification.GetRemainingBalance(Clock.Today, amt);
                            writtenOffNonCapitalAmount += otherBalance;
                            if (otherBalance > 0m)
                            {
                                var transactionAccountType = CreditNotificationDomainModel.MapNonCapitalAmountTypeToAccountType(amt.GetBuiltinAmountType());
                                context.AddAccountTransactions(CreateTransaction(
                                    transactionAccountType,
                                    -otherBalance,
                                    evt.BookKeepingDate,
                                    evt,
                                    creditNr: creditNr,
                                    notificationId: notification.NotificationId,
                                    writeOff: wo));
                            }
                        }
                    }

                    foreach(var customType in paymentOrder.Where(x => !x.IsBuiltin))
                    {
                        var costBalance = notification.GetRemainingBalance(Clock.Today, customType);
                        if(costBalance > 0m)
                        {
                            context.AddAccountTransactions(CreateTransaction(
                                TransactionAccountType.NotificationCost,
                                -costBalance,
                                evt.BookKeepingDate,
                                evt,
                                creditNr: creditNr,
                                notificationId: notification.NotificationId,
                                writeOff: wo,
                                subAccountCode: customType.Code));
                        }
                    }
                }

                //Write off not notified capital
                var notNotifiedCapitalBalance = creditModel.GetNotNotifiedCapitalBalance(Clock.Today) + movedBackNotNotifiedCapitalAmount;
                if (notNotifiedCapitalBalance > 0m)
                {
                    context.AddAccountTransactions(CreateTransaction(
                        TransactionAccountType.NotNotifiedCapital,
                        -notNotifiedCapitalBalance,
                        evt.BookKeepingDate,
                        evt,
                        creditNr: creditNr,
                        writeOff: wo));
                }

                var notNotifiedCosts = creditModel.GetNotNotifiedNotificationCosts(Clock.Today);
                foreach (var customType in paymentOrder.Where(x => !x.IsBuiltin))
                {
                    var notNotifiedBalance = notNotifiedCosts.OptS(customType.Code) ?? 0m;
                    if(notNotifiedBalance > 0m)
                    {
                        context.AddAccountTransactions(CreateTransaction(
                            TransactionAccountType.NotNotifiedNotificationCost,
                            -notNotifiedBalance,
                            evt.BookKeepingDate,
                            evt,
                            creditNr: creditNr,
                            writeOff: wo,
                            subAccountCode: customType.Code));
                    }
                }

                if (!simulateOnly)
                {
                    SetStatus(credit, CreditStatus.Settled, evt, context);
                    credit.ChangedById = UserId;
                    credit.ChangedDate = Clock.Now;

                    //Flag notifications as closed
                    var nIds = notifications.Where(x => !x.Value.GetClosedDate(Clock.Today).HasValue).Select(x => x.Value.NotificationId).ToList();
                    foreach (var n in context.CreditNotificationHeadersQueryable.Where(x => nIds.Contains(x.Id)))
                    {
                        n.ClosedTransactionDate = Clock.Today;
                    }

                    AddComment(
                        writtenOffNonCapitalAmount == 0m
                        ? string.Format(
                            CommentFormattingCulture,
                            "Credit manually corrected and closed. {0:C} remaining capital debt and any open notifications were written off.",
                            writtenOffCapitalAmount)
                        : string.Format(
                            CommentFormattingCulture,
                            "Credit manually corrected and closed. {0:C} remaining capital debt and any open notifications were written off totalling {1:C} of non capital debt.",
                            writtenOffCapitalAmount, writtenOffNonCapitalAmount),
                        BusinessEventType.CreditCorrectAndClose, context, credit: credit);

                    context.SaveChanges();

                    try
                    {
                        customerRelationsMergeService.MergeLoansToCustomerRelations(onlyTheseCreditNrs: new HashSet<string> { creditNr });
                    }
                    catch
                    {
                        /* Ignored. Will get corrected by the daily maintainance job */
                    }
                }

                failedMessage = null;
                return true;
            }
        }
    }
}