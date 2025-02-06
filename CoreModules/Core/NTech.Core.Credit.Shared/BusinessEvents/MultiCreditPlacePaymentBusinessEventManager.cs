using Dapper;
using nCredit.DomainModel;
using NTech.Banking.IncomingPaymentFiles;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Infrastructure.CoreValidation;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;


namespace nCredit.DbModel.BusinessEvents
{
    public class MultiCreditPlacePaymentBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        private readonly ILoggingService loggingService;
        private readonly CreditContextFactory creditContextFactory;
        private readonly PaymentOrderService paymentOrderService;
        private readonly PaymentAccountService paymentAccountService;
        private readonly ICreditEnvSettings envSettings;
        private readonly EncryptionService encryptionService;
        private readonly IDocumentClient documentClient;
        private readonly CreditTerminationLettersInactivationBusinessEventManager terminationLetterManager;
        private readonly AlternatePaymentPlanService alternatePaymentPlanService;

        public MultiCreditPlacePaymentBusinessEventManager(INTechCurrentUserMetadata currentUser, ICoreClock clock,
            IClientConfigurationCore clientConfiguration, ILoggingService loggingService, CreditContextFactory creditContextFactory,
            PaymentOrderService paymentOrderService, PaymentAccountService paymentAccountService, ICreditEnvSettings envSettings,
            EncryptionService encryptionService, IDocumentClient documentClient, CreditTerminationLettersInactivationBusinessEventManager terminationLetterManager,
            AlternatePaymentPlanService alternatePaymentPlanService) : base(currentUser, clock, clientConfiguration)
        {
            this.loggingService = loggingService;
            this.creditContextFactory = creditContextFactory;
            this.paymentOrderService = paymentOrderService;
            this.paymentAccountService = paymentAccountService;
            this.envSettings = envSettings;
            this.encryptionService = encryptionService;
            this.documentClient = documentClient;
            this.terminationLetterManager = terminationLetterManager;
            this.alternatePaymentPlanService = alternatePaymentPlanService;
        }

        public PaymentPlacementInitialDataResponse GetPlacementInitialData(PaymentPlacementInitialDataRequest request)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var unplacedPayment = PaymentDomainModel.CreateForSinglePayment(request.PaymentId, context, encryptionService, IncomingPaymentHeaderItemCode.OcrReference, IncomingPaymentHeaderItemCode.NoteText);
                var ocr = unplacedPayment.GetItem(IncomingPaymentHeaderItemCode.OcrReference);

                FindPaymentPlacementCreditNrsResponse matched = null;
                if (ocr != null)
                {
                    matched = this.FindPaymentPlacementCreditNrs(new FindPaymentPlacementCreditNrsRequest { SearchString = ocr });
                }

                var paymentItems = context
                    .IncomingPaymentHeadersQueryable
                    .Where(x => x.Id == request.PaymentId)
                    .SelectMany(x => x.Items.Select(y => new
                    {
                        Id = y.Id,
                        Name = y.Name,
                        Value = y.Value,
                        IsEncrypted = y.IsEncrypted
                    }))
                    .ToList();

                return new PaymentPlacementInitialDataResponse
                {
                    Id = unplacedPayment.PaymentId,
                    Items = paymentItems.Select(x =>
                    {
                        var item = new PaymentPlacementInitialDataItem
                        {
                            ItemId = x.Id,
                            Name = x.Name,
                            //We always want the NoteText to be visible
                            IsEncrypted = x.Name == IncomingPaymentHeaderItemCode.NoteText.ToString()
                                ? false
                                : x.IsEncrypted,
                            Value = x.Name == IncomingPaymentHeaderItemCode.NoteText.ToString()
                                ? unplacedPayment.GetItem(IncomingPaymentHeaderItemCode.NoteText)
                                : (x.IsEncrypted ? null : x.Value)
                        };

                        if (item.IsEncrypted)
                        {
                            item.DecryptionId = long.Parse(x.Value);
                        }

                        return item;
                    }).ToList(),
                    MatchedCreditNrs = matched?.CreditNrs ?? new List<string>(),
                    PaymentDate = unplacedPayment.PaymentDate,
                    UnplacedAmount = unplacedPayment.GetUnplacedAmount(Clock.Today)
                };
            }
        }

        public PaymentPlacementSuggestionResponse ComputeMultiCreditPlacementInstruction(PaymentPlacementSuggestionRequest request)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var batchDataSource = new PaymentPlacementBatchDataSource(envSettings, paymentOrderService);
                var creditPaymentModels = CreditPaymentPlacementModel.LoadBatch(request.CreditNrs.ToHashSetShared(), context, envSettings, ClientCfg, batchDataSource);
                var payment = PaymentDomainModel.CreateForSinglePayment(request.PaymentId, context, encryptionService);
                PaymentOrderItem onlyPlaceAgainstOrderItem = null;
                if (request.OnlyPlaceAgainstPaymentOrderItemUniqueId != null)
                {
                    onlyPlaceAgainstOrderItem = paymentOrderService.GetItemByUniqueId(request.OnlyPlaceAgainstPaymentOrderItemUniqueId, allowRse: true);
                    if (onlyPlaceAgainstOrderItem == null)
                        throw new NTechCoreWebserviceException("OnlyPlaceAgainstPaymentOrderItemUniqueId: Invalid payment order item id") { IsUserFacing = true, ErrorHttpStatusCode = 400, ErrorCode = "invalidOnlyPlaceAgainstPaymentOrderItemUniqueId" };
                }

                return ComputeMultiCreditPlacementInstruction(request.CreditNrs.Select(x => creditPaymentModels[x]).ToList(), batchDataSource, payment.GetUnplacedAmount(context.CoreClock.Today),
                    onlyPlaceAgainstNotified: request.OnlyPlaceAgainstNotified.GetValueOrDefault(),
                    onlyPlaceAgainstOrderItem: onlyPlaceAgainstOrderItem,
                    maxPlacedAmount: request.MaxPlacedAmount);
            }
        }

        private PaymentPlacementSuggestionResponse ComputeMultiCreditPlacementInstruction(List<CreditPaymentPlacementModel> credits,
            PaymentPlacementBatchDataSource placementBatchDataSource,
            decimal paymentInitialAmount,
            bool onlyPlaceAgainstNotified = false,
            PaymentOrderItem onlyPlaceAgainstOrderItem = null,
            decimal? maxPlacedAmount = null)
        {
            var paymentOrder = paymentOrderService.GetPaymentOrderItems();

            var initialLeftToPlaceAmount = maxPlacedAmount.HasValue ? Math.Min(maxPlacedAmount.Value, paymentInitialAmount) : paymentInitialAmount;
            var paymentLeftToPlaceAmount = initialLeftToPlaceAmount;

            List<PaymentPlacementItem> notificationPlacementItems = new List<PaymentPlacementItem>();
            List<PaymentPlacementItem> notNotifiedPlacementItems = new List<PaymentPlacementItem>();
            var today = Clock.Today;

            void AddPlaceOrWriteoffItem(string creditNr, decimal currentAmount, decimal writtenOffAmount, decimal placedAmount, PaymentOrderItem paymentType, int? notificationId, DateTime? notificationDueDate)
            {
                (notificationId.HasValue ? notificationPlacementItems : notNotifiedPlacementItems).Add(new PaymentPlacementItem
                {
                    ItemType = PaymentPlacementItemCode.PlaceOrWriteoff.ToString(),
                    AmountCurrent = currentAmount,
                    AmountWrittenOff = writtenOffAmount,
                    AmountPlaced = placedAmount,
                    CostTypeUniqueId = paymentType.GetUniqueId(),
                    CreditNr = creditNr,
                    ItemId = PaymentPlacementItem.CreateItemId(creditNr, paymentType.GetUniqueId(), notificationId),
                    MoveToUnplacedItemId = null,
                    NotificationId = notificationId,
                    NotificationDueDate = notificationDueDate
                });
            }
            void AddMoveToUnplacedOrPlaceItem(string creditNr, decimal currentAmount, decimal writtenOffAmount, decimal placedAmount, PaymentOrderItem paymentType, int notificationId, DateTime notificationDueDate)
            {
                notificationPlacementItems.Add(new PaymentPlacementItem
                {
                    ItemType = PaymentPlacementItemCode.MoveToUnplacedOrPlace.ToString(),
                    AmountCurrent = currentAmount,
                    AmountWrittenOff = writtenOffAmount,
                    AmountPlaced = placedAmount,
                    CostTypeUniqueId = paymentType.GetUniqueId(),
                    CreditNr = creditNr,
                    ItemId = PaymentPlacementItem.CreateItemId(creditNr, paymentType.GetUniqueId(), notificationId),
                    MoveToUnplacedItemId = PaymentPlacementItem.CreateItemId(creditNr, paymentType.GetUniqueId(), null),
                    NotificationId = notificationId,
                    NotificationDueDate = notificationDueDate
                });
            }

            decimal GetPlacedAmount(PaymentOrderItem t, decimal currentAmount, bool isNotifiedAmount)
            {
                if (onlyPlaceAgainstNotified && !isNotifiedAmount) return 0m;
                if (onlyPlaceAgainstOrderItem != null && !t.Equals(onlyPlaceAgainstOrderItem)) return 0m;
                return Math.Min(currentAmount, paymentLeftToPlaceAmount);
            }

            ////////////////////
            // Notifications ///
            ////////////////////
            foreach (var creditNotification in credits.SelectMany(c => c.GetOpenNotifications().Select(n => new { c, n })).OrderBy(x => x.n.DueDate).ThenBy(x => x.c.CreditNr))
            {
                var credit = creditNotification.c;
                var notification = creditNotification.n;

                foreach (var paymentType in paymentOrder)
                {
                    var currentAmount = notification.GetRemainingBalance(paymentType);

                    decimal extraWriteOffAmount = 0;
                    if (paymentType.IsCreditDomainModelAmountType(CreditDomainModel.AmountType.Interest) && placementBatchDataSource.HasActiveSettlementOffer(credit.CreditNr) && notification.DueDate > today)
                    {
                        //Potentially there is interest into the future here which should then be written off
                        var futureInterestAmount = credit.ComputeInterestAmountIgnoringInterestFromDate(today, Tuple.Create(today.AddDays(1), notification.DueDate));
                        var futureInterestWriteOffAmount = Math.Min(futureInterestAmount, currentAmount);
                        extraWriteOffAmount = futureInterestWriteOffAmount;
                        currentAmount -= extraWriteOffAmount; //Simulate a world where there is no future interest on the notification
                    }

                    if (currentAmount < 0m)
                    {
                        throw new NTechCoreWebserviceException($"Notification {notification.NotificationId} has negative balance for {paymentType.GetDebugText()}");
                    }
                    var placedAmount = GetPlacedAmount(paymentType, currentAmount, isNotifiedAmount: true);
                    paymentLeftToPlaceAmount -= placedAmount;

                    var writtenOffAmount = 0m;
                    writtenOffAmount += extraWriteOffAmount;
                    currentAmount += extraWriteOffAmount; //Restore current if it was counted down

                    if (paymentType.IsCreditDomainModelAmountType(CreditDomainModel.AmountType.Capital))
                    {
                        AddMoveToUnplacedOrPlaceItem(credit.CreditNr, currentAmount, writtenOffAmount, placedAmount, paymentType, notification.NotificationId, notification.DueDate);
                    }
                    else if (paymentType.IsCreditDomainModelAmountType(CreditDomainModel.AmountType.NotificationFee))
                    {
                        AddPlaceOrWriteoffItem(credit.CreditNr, currentAmount, writtenOffAmount, placedAmount, paymentType, notification.NotificationId, notification.DueDate);
                    }
                    else if (paymentType.IsCreditDomainModelAmountType(CreditDomainModel.AmountType.ReminderFee))
                    {
                        AddPlaceOrWriteoffItem(credit.CreditNr, currentAmount, writtenOffAmount, placedAmount, paymentType, notification.NotificationId, notification.DueDate);
                    }
                    else if (paymentType.IsCreditDomainModelAmountType(CreditDomainModel.AmountType.Interest))
                    {
                        AddPlaceOrWriteoffItem(credit.CreditNr, currentAmount, writtenOffAmount, placedAmount, paymentType, notification.NotificationId, notification.DueDate);
                    }
                    else if (!paymentType.IsBuiltin)
                    {
                        AddPlaceOrWriteoffItem(credit.CreditNr, currentAmount, writtenOffAmount, placedAmount, paymentType, notification.NotificationId, notification.DueDate);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            ///////////////////
            // Not notified ///
            ///////////////////            
            void AddCreateAndPlaceAmount(string creditNr, decimal amount, PaymentOrderItem paymentType, decimal? computedCurrentAmount = null)
            {
                notNotifiedPlacementItems.Add(new PaymentPlacementItem
                {
                    ItemType = PaymentPlacementItemCode.CreateAndPlace.ToString(),
                    AmountCurrent = amount,
                    AmountWrittenOff = 0m,
                    AmountPlaced = amount,
                    CostTypeUniqueId = paymentType.GetUniqueId(),
                    CreditNr = creditNr,
                    ItemId = PaymentPlacementItem.CreateItemId(creditNr, paymentType.GetUniqueId(), null),
                    MoveToUnplacedItemId = null,
                    NotificationId = null,
                    NotificationDueDate = null,
                    HasAmountCurrentComputed = computedCurrentAmount.HasValue,
                    AmountCurrentComputed = computedCurrentAmount
                });
            }

            var rsePaymentOrderItem = PaymentOrderItem.FromSwedishRse();
            var capitalPaymentOrderItem = PaymentOrderItem.FromAmountType(CreditDomainModel.AmountType.Capital);
            var interestPaymentOrderItem = PaymentOrderItem.FromAmountType(CreditDomainModel.AmountType.Interest);
            foreach (var credit in credits)
            {
                //Swedish rse
                var computedSwedishRseAmount = credit.GetSettlementOfferSwedishRseEstimatedAmount() ?? 0m;
                var placedSwedishRseAmount = GetPlacedAmount(rsePaymentOrderItem, computedSwedishRseAmount, isNotifiedAmount: false);
                paymentLeftToPlaceAmount -= placedSwedishRseAmount;
                AddCreateAndPlaceAmount(credit.CreditNr, placedSwedishRseAmount, rsePaymentOrderItem, computedCurrentAmount: computedSwedishRseAmount);

                //Not notified capital
                var notNotifiedCapitalItemId = PaymentPlacementItem.CreateItemId(credit.CreditNr, capitalPaymentOrderItem.GetUniqueId(), null);
                var initialNotNotifiedCapitalAmount = credit.GetNotNotifiedCapitalBalance();
                var movedBackCapitalAmount = notificationPlacementItems.Where(x => x.MoveToUnplacedItemId == notNotifiedCapitalItemId).Sum(x => x.AmountWrittenOff);
                var currentNotNotifiedCapitalAmount = initialNotNotifiedCapitalAmount + movedBackCapitalAmount;
                if (currentNotNotifiedCapitalAmount < 0m)
                {
                    throw new NTechCoreWebserviceException($"Credit {credit.CreditNr} has negative not notified capital");
                }
                var placedNotNotifedCapitalAmount = GetPlacedAmount(capitalPaymentOrderItem, currentNotNotifiedCapitalAmount, isNotifiedAmount: false);
                paymentLeftToPlaceAmount -= placedNotNotifedCapitalAmount;
                AddPlaceOrWriteoffItem(credit.CreditNr,
                    currentNotNotifiedCapitalAmount,
                    0m,
                    placedNotNotifedCapitalAmount,
                    PaymentOrderItem.FromAmountType(CreditDomainModel.AmountType.Capital), null, null);

                //Not notified interest rate
                var notNotifiedInterestBalance = credit.ComputeNotNotifiedInterestUntil(today, out _);
                var placedNotNotifiedInterestAmount = GetPlacedAmount(interestPaymentOrderItem, notNotifiedInterestBalance, isNotifiedAmount: false);
                paymentLeftToPlaceAmount -= placedNotNotifiedInterestAmount;
                AddCreateAndPlaceAmount(credit.CreditNr, placedNotNotifiedInterestAmount, interestPaymentOrderItem, computedCurrentAmount: notNotifiedInterestBalance);
            }

            var totalPlacedAmount = initialLeftToPlaceAmount - paymentLeftToPlaceAmount;

            return new PaymentPlacementSuggestionResponse
            {
                Instruction = new MultiCreditPaymentPlacementInstruction
                {
                    InitialPaymentAmount = paymentInitialAmount,
                    LeaveUnplacedAmount = paymentInitialAmount - totalPlacedAmount,
                    NotificationPlacementItems = notificationPlacementItems,
                    NotNotifiedPlacementItems = notNotifiedPlacementItems
                }
            };
        }

        private decimal PlaceMultiCreditPaymentPlacementInstruction(ICreditContextExtended context, MultiCreditPaymentPlacementInstruction instruction, IncomingPaymentHeader payment,
            BusinessEvent evt, PaymentPlacementBatchDataSource batchDataSource, DateTime? bookKeepingDateOverride, decimal paymentBalanceBeforePlacement)
        {
            DateTime bookKeepingDate = bookKeepingDateOverride ?? payment.BookKeepingDate;

            var writeoffs = new Dictionary<string, WriteoffHeader>();
            WriteoffHeader EnsureWriteoff(string creditNr)
            {
                if (!writeoffs.ContainsKey(creditNr))
                {
                    var writeoff = new WriteoffHeader
                    {
                        BookKeepingDate = bookKeepingDate,
                        ChangedById = evt.ChangedById,
                        ChangedDate = evt.ChangedDate,
                        InformationMetaData = evt.InformationMetaData,
                        TransactionDate = evt.TransactionDate
                    };
                    context.AddWriteoffHeader(writeoff);
                    writeoffs[creditNr] = writeoff;
                }
                return writeoffs[creditNr];
            }

            //Javascript uses floats so to avoid having to deal with all the edge cases of like 1.01 != 1.0100000001 we just round it away before doing anything.
            instruction.RoundEverything();

            var paymentOrderItems = paymentOrderService.GetPaymentOrderItems();

            List<AccountTransaction> trs = new List<AccountTransaction>();
            Dictionary<string, decimal> creditBalanceAfter = new Dictionary<string, decimal>();
            void AddTransaction(bool adjustCreditBalance, AccountTransaction tr)
            {
                if (adjustCreditBalance && tr.CreditNr != null)
                {
                    if (!creditBalanceAfter.ContainsKey(tr.CreditNr))
                        creditBalanceAfter[tr.CreditNr] = batchDataSource.GetCreditDomainModel(tr.CreditNr, context).GetBalance(context.CoreClock.Today);
                    creditBalanceAfter[tr.CreditNr] += tr.Amount;
                }
                trs.Add(tr);
            }

            foreach (var notificationItem in instruction.NotificationPlacementItems)
            {
                if (!notificationItem.NotificationId.HasValue)
                    throw new Exception($"Missing NotificationId on payment item for credit {notificationItem.CreditNr}");
                var paymentOrderItem = paymentOrderItems.Single(x => x.GetUniqueId() == notificationItem.CostTypeUniqueId);
                if (notificationItem.ItemType == PaymentPlacementItemCode.PlaceOrWriteoff.ToString())
                {
                    TransactionAccountType accountType = paymentOrderItem.IsBuiltin
                        ? CreditDomainModel.MapNonCapitalAmountTypeToAccountType(paymentOrderItem.GetBuiltinAmountType())
                        : TransactionAccountType.NotificationCost;
                    string subAccountCode = paymentOrderItem.IsBuiltin ? null : paymentOrderItem.Code;

                    if (notificationItem.AmountPlaced > 0m)
                        AddTransaction(
                            adjustCreditBalance: true,
                            CreateTransaction(
                                accountType, -notificationItem.AmountPlaced, bookKeepingDate, evt,
                                creditNr: notificationItem.CreditNr,
                                incomingPayment: payment,
                                notificationId: notificationItem.NotificationId,
                                subAccountCode: subAccountCode));

                    if (notificationItem.AmountWrittenOff > 0m)
                        AddTransaction(
                            adjustCreditBalance: true,
                            CreateTransaction(
                                accountType, -notificationItem.AmountWrittenOff, bookKeepingDate, evt,
                                creditNr: notificationItem.CreditNr,
                                writeOff: EnsureWriteoff(notificationItem.CreditNr),
                                notificationId: notificationItem.NotificationId,
                                subAccountCode: subAccountCode));
                }
                else if (notificationItem.ItemType == PaymentPlacementItemCode.MoveToUnplacedOrPlace.ToString())
                {
                    if (!paymentOrderItem.IsCreditDomainModelAmountType(CreditDomainModel.AmountType.Capital))
                        throw new NotImplementedException();
                    if (notificationItem.AmountPlaced > 0m)
                        AddTransaction(
                            adjustCreditBalance: true,
                            CreateTransaction(
                                TransactionAccountType.CapitalDebt, -notificationItem.AmountPlaced, bookKeepingDate, evt,
                                creditNr: notificationItem.CreditNr,
                                incomingPayment: payment,
                                notificationId: notificationItem.NotificationId));

                    if (notificationItem.AmountWrittenOff > 0m)
                        AddTransaction(
                            adjustCreditBalance: false,
                            CreateTransaction( //NOTE: No minus is not an error since not notified rather than capital debt
                                TransactionAccountType.NotNotifiedCapital, notificationItem.AmountWrittenOff, bookKeepingDate, evt,
                                creditNr: notificationItem.CreditNr,
                                writeOff: EnsureWriteoff(notificationItem.CreditNr),
                                notificationId: notificationItem.NotificationId));
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            var capitalItemId = PaymentOrderItem.FromAmountType(CreditDomainModel.AmountType.Capital).GetUniqueId();
            var interestItemId = PaymentOrderItem.FromAmountType(CreditDomainModel.AmountType.Interest).GetUniqueId();
            var swedishRseItemId = PaymentOrderItem.FromSwedishRse().GetUniqueId();

            foreach (var creditItem in instruction.NotNotifiedPlacementItems)
            {
                if (creditItem.NotificationId.HasValue)
                    throw new Exception($"NotificationId on payment item for credit {creditItem.CreditNr}");

                if (creditItem.CostTypeUniqueId == capitalItemId)
                {
                    //Not notified capital
                    if (creditItem.AmountPlaced > 0m)
                    {
                        AddTransaction(
                            adjustCreditBalance: true,
                            CreateTransaction(
                                TransactionAccountType.CapitalDebt, -creditItem.AmountPlaced, bookKeepingDate, evt,
                                creditNr: creditItem.CreditNr,
                                incomingPayment: payment));
                        AddTransaction(
                            adjustCreditBalance: false,
                            CreateTransaction(
                                TransactionAccountType.NotNotifiedCapital, -creditItem.AmountPlaced, bookKeepingDate, evt,
                                creditNr: creditItem.CreditNr));
                    }

                    if (creditItem.AmountWrittenOff > 0m)
                    {
                        AddTransaction(
                            adjustCreditBalance: true,
                            CreateTransaction(
                                TransactionAccountType.CapitalDebt, -creditItem.AmountWrittenOff, bookKeepingDate, evt,
                                creditNr: creditItem.CreditNr,
                                writeOff: EnsureWriteoff(creditItem.CreditNr)));
                        AddTransaction(
                            adjustCreditBalance: false,
                            CreateTransaction(
                                TransactionAccountType.NotNotifiedCapital, -creditItem.AmountWrittenOff, bookKeepingDate, evt,
                                creditNr: creditItem.CreditNr,
                                writeOff: EnsureWriteoff(creditItem.CreditNr)));
                    }
                }
                else
                {
                    TransactionAccountType accountType;
                    if (creditItem.CostTypeUniqueId == interestItemId)
                        accountType = TransactionAccountType.InterestDebt;
                    else if (creditItem.CostTypeUniqueId == swedishRseItemId)
                        accountType = TransactionAccountType.SwedishRseDebt;
                    else
                        throw new NotImplementedException();

                    if (creditItem.AmountPlaced > 0m)
                    {
                        AddTransaction(
                            adjustCreditBalance: false,
                            CreateTransaction(
                                accountType, creditItem.AmountPlaced, bookKeepingDate, evt,
                                creditNr: creditItem.CreditNr));
                        AddTransaction(
                            adjustCreditBalance: false,
                            CreateTransaction(
                                accountType, -creditItem.AmountPlaced, bookKeepingDate, evt,
                                creditNr: creditItem.CreditNr,
                                incomingPayment: payment));
                    }


                    if (creditItem.AmountWrittenOff > 0m)
                        throw new NotImplementedException();
                }
            }

            var totalPlacedAmountByCreditNr = trs
                .Where(x => x.IncomingPayment != null)
                .GroupBy(x => x.CreditNr)
                .ToDictionary(x => x.Key, x => -x.Sum(y => (decimal?)y.Amount) ?? 0m);

            var totalPlacedAmount = totalPlacedAmountByCreditNr.Values.Sum();
            if (totalPlacedAmount > paymentBalanceBeforePlacement)
            {
                throw new NTechCoreWebserviceException($"Placed amount exceeds payment amount for payment: {payment.Id}");
            }

            var paymentBalanceAfterPlacement = paymentBalanceBeforePlacement - totalPlacedAmount;

            if (evt.EventType == BusinessEventType.PlacedUnplacedIncomingPayment.ToString())
            {
                foreach (var placedAmountOnCredit in totalPlacedAmountByCreditNr.Where(x => x.Value > 0m))
                {
                    //For payments that are not from unplaced we never add balance to unplaced at all so it should not be removed either
                    trs.Add(CreateTransaction(TransactionAccountType.UnplacedPayment, -placedAmountOnCredit.Value, bookKeepingDate, evt,
                        creditNr: placedAmountOnCredit.Key,
                        incomingPayment: payment));
                }
            }

            if (paymentBalanceAfterPlacement == 0m)
            {
                payment.IsFullyPlaced = true;
            }

            foreach (var creditNr in totalPlacedAmountByCreditNr.Keys)
            {
                var credit = batchDataSource.GetCreditDomainModel(creditNr, context);
                var creditBalanceAfterPayment = creditBalanceAfter.OptS(creditNr) ?? credit.GetBalance(context.CoreClock.Today);
                var placedAmount = totalPlacedAmountByCreditNr[creditNr];

                if (credit.GetStatus() != CreditStatus.Normal)
                    throw new NTechCoreWebserviceException($"Credit {creditNr} is not active") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                if (creditBalanceAfterPayment < 0m)
                    throw new NTechCoreWebserviceException($"Credit {creditNr} would have negative balance {creditBalanceAfterPayment} if this payment was placed!") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                var hasActiveSettlementOffer = batchDataSource.HasActiveSettlementOffer(creditNr);
                if (creditBalanceAfterPayment == 0m)
                {
                    var header = batchDataSource.GetCreditHeader(creditNr, context);
                    SetStatus(header, CreditStatus.Settled, evt, context);
                    header.ChangedById = UserId;
                    header.ChangedDate = Clock.Now;

                    if (hasActiveSettlementOffer)
                    {
                        var activeOffer = batchDataSource.GetActiveSettlementOffer(creditNr);
                        activeOffer.CommitedByEvent = evt;
                        activeOffer.ChangedById = UserId;
                        activeOffer.ChangedDate = Clock.Now;
                        AddComment($"Credit settled by payment. Total placed amount was {totalPlacedAmount.ToString("C", CommentFormattingCulture)}.", "CreditSettledByPayment", header, context);
                    }
                }
                else if (hasActiveSettlementOffer)
                {
                    var activeOffer = batchDataSource.GetActiveSettlementOffer(creditNr);
                    activeOffer.CancelledByEvent = evt;
                    activeOffer.ChangedById = UserId;
                    activeOffer.ChangedDate = Clock.Now;
                    AddComment("Credit settlement offer cancelled due to placing a non-settling payment.", "CreditSettlmentOfferCancelledByPayment", null, context, creditNr: creditNr);
                }
            }

            foreach (var creditNr in totalPlacedAmountByCreditNr.Keys)
            {
                var transactionsByNotificationId = trs
                    .Where(x => x.CreditNotificationId.HasValue && x.CreditNr == creditNr)
                    .GroupBy(x => x.CreditNotificationId.Value)
                    .ToDictionary(x => x.Key, x => x.ToList());
                foreach (var notificationId in transactionsByNotificationId.Keys)
                {
                    var notificationBalanceAfterPayment = batchDataSource.GetCurrentBalanceOnOpenNotification(creditNr, notificationId, context, transactionsByNotificationId[notificationId]);

                    if (notificationBalanceAfterPayment <= 0m)
                    {
                        var header = batchDataSource.GetCreditNotificationHeader(creditNr, notificationId, context);
                        NewCreditNotificationBusinessEventManager.UpdateNotificationOnFullyPaid(header, context.CoreClock, context.CurrentUser.UserId);
                    }
                }
            }

            context.AddAccountTransactions(trs.ToArray());

            return paymentBalanceAfterPlacement;
        }

        public bool TryImportFile(IncomingPaymentFileWithOriginal file, bool? overrideDuplicateCheck, bool? overrideIbanCheck, out string failedMessage, out string placementMessage)
        {
            placementMessage = null;

            using (var context = creditContextFactory.CreateContext())
            {
                context.IsChangeTrackingEnabled = false;
                context.BeginTransaction();
                try
                {
                    if (!overrideDuplicateCheck.HasValue || !overrideDuplicateCheck.Value)
                    {
                        if (context.IncomingPaymentFileHeadersQueryable.Any(x => x.ExternalId == file.ExternalId))
                        {
                            failedMessage = "File has already been imported. Override with overrideDuplicateCheck.";
                            return false;
                        }
                    }

                    if (!overrideIbanCheck.GetValueOrDefault())
                    {
                        var expectedAccountNr = paymentAccountService.GetIncomingPaymentBankAccountNr().FormatFor(null);
                        var otherIbans = file
                            .Accounts
                            .Where(x => x.AccountNr.NormalizedValue != expectedAccountNr)
                            .Select(x => x.AccountNr.NormalizedValue)
                            .Distinct()
                            .ToList();
                        if (otherIbans.Any())
                        {
                            failedMessage = "File has payments to unexpected accounts. Override with overrideIbanCheck.";
                            return false;
                        }
                    }

                    var batchDataSource = new PaymentPlacementBatchDataSource(envSettings, paymentOrderService);

                    var createResult = CreateIncomingPaymentFile(context, file, out placementMessage, batchDataSource);

                    var createdFile = createResult.Header;
                    context.DetectChanges();
                    context.SaveChanges();

                    if (createResult.PlacedAgainstCreditNrs.Count > 0)
                    {
                        AfterPaymentsPlaced(createResult.PlacedAgainstCreditNrs, context, createResult.Header.CreatedByEvent,
                            terminationLetterManager, alternatePaymentPlanService);
                        context.DetectChanges();
                        context.SaveChanges();
                    }
                    context.CommitTransaction();

                    failedMessage = null;
                    return true;
                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }
            }
        }

        public static void AfterPaymentsPlaced(HashSet<string> creditNrs, ICreditContextExtended context, BusinessEvent evt,
            CreditTerminationLettersInactivationBusinessEventManager terminationLetterManager, AlternatePaymentPlanService alternatePaymentPlanService)
        {
            terminationLetterManager.InactivateTerminationLettersWhereNotificationsPaid(context, creditNrs, businessEvent: evt);
            if (alternatePaymentPlanService.IsPaymentPlanEnabled)
            {
                alternatePaymentPlanService.CompleteFullyPaidPaymentPlans(context, new Lazy<BusinessEvent>(() => evt), onlyTheseCreditNrs: creditNrs.ToList());
            }
        }

        public void PlaceFromUnplaced(PaymentPlacementRequest request)
        {
            if (!TryPlaceFromUnplaced(request.Instruction, request.PaymentId, out var failedMessage))
                throw new NTechCoreWebserviceException(failedMessage) { IsUserFacing = true, ErrorHttpStatusCode = 400 };
        }

        public bool TryPlaceFromUnplaced(MultiCreditPaymentPlacementInstruction instruction, int paymentId, out string failedMessage)
        {
            instruction.RoundEverything();
            failedMessage = instruction.GetInvalidReason(paymentOrderService);
            if (failedMessage != null)
                return false;

            using (var context = creditContextFactory.CreateContext())
            {
                context.BeginTransaction();
                try
                {
                    var evt = new BusinessEvent
                    {
                        BookKeepingDate = Clock.Today,
                        ChangedById = UserId,
                        ChangedDate = Clock.Now,
                        EventType = BusinessEventType.PlacedUnplacedIncomingPayment.ToString(),
                        InformationMetaData = InformationMetadata,
                        TransactionDate = Clock.Today,
                        EventDate = Clock.Now
                    };
                    context.AddBusinessEvent(evt);

                    var paymentModel = PaymentDomainModel.CreateForSinglePayment(paymentId, context, encryptionService);
                    var initialPaymentAmount = paymentModel.GetUnplacedAmount(context.CoreClock.Today);
                    if (instruction.InitialPaymentAmount != initialPaymentAmount)
                    {
                        failedMessage = "InitialPaymentAmount differs from actual initial amount";
                        return false;
                    }
                    var paymentHeader = context.IncomingPaymentHeadersQueryable.Single(x => x.Id == paymentId);
                    var batchDataSource = new PaymentPlacementBatchDataSource(envSettings, paymentOrderService);
                    var creditNrs = instruction.GetCreditNrs();
                    batchDataSource.EnsurePreloaded(creditNrs.ToHashSetShared(), context);

                    PlaceMultiCreditPaymentPlacementInstruction(context, instruction, paymentHeader, evt, batchDataSource, context.CoreClock.Today, instruction.InitialPaymentAmount);

                    context.SaveChanges();

                    if (creditNrs.Count > 0)
                    {
                        AfterPaymentsPlaced(creditNrs.ToHashSetShared(), context, evt, terminationLetterManager, alternatePaymentPlanService);
                        context.SaveChanges();
                    }

                    context.CommitTransaction();

                    failedMessage = null;
                    return true;

                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }
            }
        }

        private (IncomingPaymentFileHeader Header, HashSet<string> PlacedAgainstCreditNrs) CreateIncomingPaymentFile(ICreditContextExtended context, IncomingPaymentFileWithOriginal paymentfile,
            out string placementMessage, PaymentPlacementBatchDataSource batchDataSource)
        {
            context.EnsureCurrentTransaction();

            var evt = new BusinessEvent
            {
                EventDate = Now,
                EventType = BusinessEventType.NewIncomingPaymentFile.ToString(),
                BookKeepingDate = Now.ToLocalTime().Date,
                TransactionDate = Now.ToLocalTime().Date,
                ChangedById = UserId,
                ChangedDate = Now,
                InformationMetaData = InformationMetadata,
            };
            context.AddBusinessEvent(evt);

            var h = new IncomingPaymentFileHeader
            {
                BookKeepingDate = Now.ToLocalTime().Date,
                TransactionDate = Now.ToLocalTime().Date,
                ChangedById = UserId,
                ChangedDate = Now,
                ExternalId = paymentfile.ExternalId,
                CreatedByEvent = evt,
                InformationMetaData = InformationMetadata,
                Payments = new List<IncomingPaymentHeader>()
            };
            context.AddIncomingPaymentFileHeader(h);

            var creditNrsByOcr = GetPlacedCreditNrsByOcrForIncomingPaymentFile(paymentfile, context);

            var pms = GetPaymentsPlacementTempData(paymentfile, creditNrsByOcr);

            var placedAgainstCreditNrs = new HashSet<string>();
            var placedCount = 0;
            int unplacedCount = 0;
            foreach (var paymentGroup in pms.Where(x => x.PlaceAgainstCreditNrs != null && x.PlaceAgainstCreditNrs.Count > 0).ToArray().SplitIntoGroupsOfN(150))
            {
                var groupCreditNrs = paymentGroup.SelectMany(x => x.PlaceAgainstCreditNrs).ToHashSetShared();
                var paymentModels = CreditPaymentPlacementModel.LoadBatch(groupCreditNrs, context, envSettings,
                    ClientCfg, batchDataSource);
                batchDataSource.EnsurePreloaded(groupCreditNrs, context);
                foreach (var payment in paymentGroup)
                {
                    var paymentHeader = CreateIncomingPaymentHeader(context, h, payment);
                    string notPlacedReasonsMessage;
                    decimal leftUnplacedAmount;
                    if (payment.PlaceAgainstCreditNrs.Any(x => placedAgainstCreditNrs.Contains(x)))
                    {
                        notPlacedReasonsMessage = "Multiple payments against the same loan";
                        leftUnplacedAmount = payment.Amount;
                    }
                    else
                    {
                        var creditPlacementModels = payment.PlaceAgainstCreditNrs.Select(x => paymentModels[x]).ToList();
                        var computeResult = ComputeMultiCreditPlacementInstruction(creditPlacementModels,
                            batchDataSource, payment.Amount);
                        var instruction = computeResult.Instruction;
                        notPlacedReasonsMessage = GetNotAutoPlacedReasonsMessage(instruction, creditPlacementModels);
                        if (notPlacedReasonsMessage != null)
                        {
                            leftUnplacedAmount = payment.Amount;
                        }
                        else
                        {
                            leftUnplacedAmount = PlaceMultiCreditPaymentPlacementInstruction(context, instruction, paymentHeader, evt, batchDataSource, context.CoreClock.Today, instruction.InitialPaymentAmount);
                        }
                    }

                    if (notPlacedReasonsMessage != null)
                    {
                        unplacedCount += 1;
                        var notPlacedReasonsMessageItem = new IncomingPaymentHeaderItem
                        {
                            ChangedById = UserId,
                            ChangedDate = Now,
                            Payment = paymentHeader,
                            InformationMetaData = InformationMetadata,
                            IsEncrypted = false,
                            Name = IncomingPaymentHeaderItemCode.NotAutoPlacedReasonMessage.ToString(),
                            Value = notPlacedReasonsMessage
                        };
                        paymentHeader.Items.Add(notPlacedReasonsMessageItem);
                        context.AddIncomingPaymentHeaderItem(notPlacedReasonsMessageItem);
                    }
                    else
                    {
                        placedCount += 1;
                        placedAgainstCreditNrs.AddRange(payment.PlaceAgainstCreditNrs);
                    }

                    if (leftUnplacedAmount > 0m)
                    {
                        context.AddAccountTransactions(CreateTransaction(TransactionAccountType.UnplacedPayment, leftUnplacedAmount, paymentHeader.BookKeepingDate, evt, incomingPayment: paymentHeader));
                    }
                }
            }

            foreach (var unmatchedPayment in pms.Where(x => x.PlaceAgainstCreditNrs == null || x.PlaceAgainstCreditNrs.Count == 0))
            {
                unplacedCount += 1;
                var paymentHeader = CreateIncomingPaymentHeader(context, h, unmatchedPayment);
                var notPlacedReasonsMessageItem = new IncomingPaymentHeaderItem
                {
                    ChangedById = UserId,
                    ChangedDate = Now,
                    Payment = paymentHeader,
                    InformationMetaData = InformationMetadata,
                    IsEncrypted = false,
                    Name = IncomingPaymentHeaderItemCode.NotAutoPlacedReasonMessage.ToString(),
                    Value = "No match on reference"
                };
                paymentHeader.Items.Add(notPlacedReasonsMessageItem);
                context.AddIncomingPaymentHeaderItem(notPlacedReasonsMessageItem);
                context.AddAccountTransactions(CreateTransaction(TransactionAccountType.UnplacedPayment, unmatchedPayment.Amount, paymentHeader.BookKeepingDate, evt, incomingPayment: paymentHeader));
            }

            var itemsToEncrypt = h.Payments.SelectMany(x => x.Items).Where(x => x.IsEncrypted).ToArray();

            encryptionService.SaveEncryptItems(itemsToEncrypt, x => x.Value, (x, encVal) => x.Value = encVal.ToString(), context);

            h.FileArchiveKey = documentClient.ArchiveStore(paymentfile.OriginalFileData, "application/xml", paymentfile.OriginalFileName);

            placementMessage = $"Placed: {placedCount}, Left unplaced: {unplacedCount}";

            return (Header: h, PlacedAgainstCreditNrs: placedAgainstCreditNrs);
        }

        private string GetNotAutoPlacedReasonsMessage(MultiCreditPaymentPlacementInstruction instruction, List<CreditPaymentPlacementModel> creditPlacementModels)
        {
            if (creditPlacementModels.Any(x => x.GetCreditStatus() != CreditStatus.Normal))
                return "Credit status";

            if (instruction.LeaveUnplacedAmount == instruction.InitialPaymentAmount)
                return "Nothing to place";

            if (creditPlacementModels.Any(x => x.ActiveSettlementOfferAmount.HasValue))
                return "Has settlement offer";

            var capitalUniqueId = PaymentOrderItem.FromAmountType(CreditDomainModel.AmountType.Capital).GetUniqueId();
            foreach (var credit in creditPlacementModels)
            {
                if (!credit.ActiveSettlementOfferAmount.HasValue && instruction.IsSettledByPayment(credit))
                {
                    var notifiedAmount = instruction.NotificationPlacementItems.Sum(x => x.AmountCurrent);
                    var placedOrWrittenOffAmount = instruction.GetCreditPlacedOrWrittenOffAmount(credit.CreditNr);
                    if (placedOrWrittenOffAmount > Math.Ceiling(notifiedAmount))
                    {
                        /*
                         * When everything has been notified we allow settle without unplaced as loan as the payment is no more than the notified balance rounded up
                         * so the customer can pay say 500 if notified 499.30.
                         * This check is only enough in combination for the global checks against placing or writing of not notified capital. If those are removed or changed, add them here
                         */
                        return "Settlement attempt with no active settlement offer";
                    }
                }
            }

            var capitalCode = PaymentOrderItem.FromAmountType(CreditDomainModel.AmountType.Capital).GetUniqueId();

            if (instruction.NotNotifiedPlacementItems.Any(x => x.AmountPlaced != 0m && x.CostTypeUniqueId != capitalCode))
                return "Would place against not notified non capital";

            if (instruction.NotNotifiedPlacementItems.Any(x => x.AmountWrittenOff != 0m && x.CostTypeUniqueId == capitalCode))
                return "Would lead to capital writeoff";

            if (instruction.NotificationPlacementItems.Any(x => x.AmountWrittenOff != 0m))
                return "Would lead to notification writeoffs";

            foreach (var credit in creditPlacementModels)
            {
                if (credit.RemainingActivePaymentPlanAmount.HasValue)
                {
                    //We allow a small overpayment here since the customer might do some rounding.
                    //This should be safe as really large overpayments get stopped by 'Settlement attempt with no active settlment offer'
                    if (instruction.GetCreditPlacedOrWrittenOffAmount(credit.CreditNr) > credit.RemainingActivePaymentPlanAmount.Value + 5m)
                    {
                        return "Paid amount exceeds the remaining alternate payment plan amount.";
                    }
                }
                else
                {
                    var placedNotNotifiedCapitalAmount = instruction.NotNotifiedPlacementItems.Where(x => x.CostTypeUniqueId == capitalCode && x.CreditNr == credit.CreditNr).Sum(x => x.AmountPlaced);

                    if (placedNotNotifiedCapitalAmount > 0m)
                    {
                        //NOTE: We may want to add a setting here to allow clients to set a higher threshold here.
                        return "Extra amortization";
                    }
                }
            }

            return null;
        }

        private IncomingPaymentHeader CreateIncomingPaymentHeader(ICreditContextExtended context, IncomingPaymentFileHeader h, ExternalPaymentData externalPayment)
        {
            var pmt = new IncomingPaymentHeader
            {
                BookKeepingDate = externalPayment.BookKeepingDate,
                TransactionDate = Now.ToLocalTime().Date,
                ChangedById = UserId,
                ChangedDate = Now,
                InformationMetaData = InformationMetadata,
                IsFullyPlaced = false,
                IncomingPaymentFile = h
            };
            h.Payments.Add(pmt);
            context.AddIncomingPaymentHeader(pmt);

            foreach (var item in externalPayment.Items)
            {
                if (pmt.Items == null)
                    pmt.Items = new List<IncomingPaymentHeaderItem>();
                var pmtItem = new IncomingPaymentHeaderItem
                {
                    ChangedById = UserId,
                    ChangedDate = Now,
                    Payment = pmt,
                    InformationMetaData = InformationMetadata,
                    IsEncrypted = item.IsSensitive,
                    Name = item.Code.ToString(),
                    Value = item.Value
                };
                pmt.Items.Add(pmtItem);
                context.AddIncomingPaymentHeaderItem(pmtItem);
            }

            return pmt;
        }

        private static string cachedCreditDatabaseCollator = null;

        private Dictionary<string, List<string>> GetPlacedCreditNrsByOcrForIncomingPaymentFile(IncomingPaymentFileWithOriginal paymentfile, ICreditContextExtended context)
        {
            if (!context.HasCurrentTransaction)
                throw new Exception("Requires an active transaction");

            var ocrs = paymentfile
                .Accounts
                .SelectMany(account => account.DateBatches
                .SelectMany(date => date.Payments.Where(x => x.OcrReference != null)
                .Select(payment => payment.OcrReference)))
                .Distinct()
                .ToArray();

            var connection = context.GetConnection();
            var transaction = context.CurrentTransaction;

            string collatorName;
            if (cachedCreditDatabaseCollator == null)
            {
                collatorName = connection.Query<string>("SELECT DATABASEPROPERTYEX(@dbName, 'Collation')", param: new { dbName = connection.Database }, transaction: transaction).Single();
                if (collatorName == null)
                    throw new Exception("Failed to lookup collator for local database");
                cachedCreditDatabaseCollator = collatorName;
            }
            else
                collatorName = cachedCreditDatabaseCollator;

            connection.Execute($"create table #Ocrs(Ocr nvarchar(100) COLLATE {collatorName} not null)", transaction: transaction);
            foreach (var ocrGroup in ocrs.SplitIntoGroupsOfN(200))
            {
                var parameters = new Dictionary<string, object>();
                var query = "";
                var index = 0;
                foreach (var ocr in ocrGroup)
                {
                    var paramName = $"@p{index}";
                    query += $"insert into #Ocrs values ({paramName}); ";
                    parameters[paramName] = ocr;
                    index++;
                }
                connection.Execute(query, param: new DynamicParameters(parameters), commandTimeout: 30, transaction: transaction);
            }

            var result = new Dictionary<string, List<string>>();

            void AddToResult(List<FileImportMatchedCredit> matchedCredits)
            {
                //Matched by shared ocr
                result.AddOrReplaceFrom(matchedCredits
                    .Where(x => x.IsSharedOcr == 1)
                    .GroupBy(x => x.MatchedOcr)
                    .ToDictionary(x => x.Key, x => x.Select(y => y.CreditNr).ToList()));

                //Matched by non shared ocr
                foreach (var matchedCredit in matchedCredits.Where(x => x.IsSharedOcr != 1))
                {
                    result[matchedCredit.MatchedOcr] = new List<string> { matchedCredit.CreditNr };
                }
            }

            const string QueryBase = @"With RankedDatedCreditString
as
(
	select	s.*,
			RANK() OVER (PARTITION BY s.CreditNr, s.[Name] ORDER BY s.Id desc) as LatestFirstRank
	from	DatedCreditString s	
) ";

            var creditQuery = QueryBase + @"
select  d.CreditNr, d.[Value] as MatchedOcr, 1 as IsSharedOcr 
from    RankedDatedCreditString d 
where   d.[Name] = 'SharedOcrPaymentReference' and d.LatestFirstRank = 1 and d.[Value] in(select Ocr from #Ocrs)
union all
select  d.CreditNr, d.[Value] as MatchedOcr, 0 as IsSharedOcr 
from    RankedDatedCreditString d 
where   d.[Name] = 'OcrPaymentReference' and d.LatestFirstRank = 1 and d.[Value] in(select Ocr from #Ocrs)";

            //Matched by credit ocr
            var matchedByCreditOcr = connection.Query<FileImportMatchedCredit>(creditQuery, commandTimeout: 60, transaction: transaction).ToList();
            AddToResult(matchedByCreditOcr);

            /*
             * Matched by open notification
             * NOTE: We expect IsSharedOcr = 0 always here since shared ocr should never be used on a notification but in case that gets messed up we check anyway
             * NOTE: We only map open notifications since we cant place anyway against closed ones and it makes recovering from having screwed up ocrs on notifications easier since
             *       the stop causing problems as soon as the are paid.
             */
            var openNotificationQuery = QueryBase + @"
select	distinct c.CreditNr, 
		c.OcrPaymentReference as MatchedOcr,
		case when exists(select 1 from RankedDatedCreditString d where d.CreditNr = c.CreditNr and d.LatestFirstRank = 1 and d.[Name] = 'SharedOcrPaymentReference') then 1 else 0 end as IsSharedOcr
from	CreditNotificationHeader c
where	c.OcrPaymentReference in(select n.Ocr from #Ocrs n)
and		c.ClosedTransactionDate is null";
            var matchedByNotificationOcr = connection.Query<FileImportMatchedCredit>(openNotificationQuery, commandTimeout: 60, transaction: transaction).ToList();
            AddToResult(matchedByNotificationOcr);
            return result;
        }

        private class FileImportMatchedCredit
        {
            public string CreditNr { get; set; }
            public string MatchedOcr { get; set; }
            public int IsSharedOcr { get; set; }
        }

        public FindPaymentPlacementCreditNrsResponse FindPaymentPlacementCreditNrs(FindPaymentPlacementCreditNrsRequest request)
        {
            var searchString = request.SearchString.NormalizeNullOrWhitespace();

            FindPaymentPlacementCreditNrsResponse Fail(string failedMessage) => new FindPaymentPlacementCreditNrsResponse { CreditNrs = new List<string>(), FailedMessage = failedMessage };

            if (string.IsNullOrWhiteSpace(searchString))
                return Fail("SearchString missing");

            using (var context = creditContextFactory.CreateContext())
            {
                FindPaymentPlacementCreditNrsResponse VerifyCreditNrs(List<string> creditNrs)
                {
                    if (creditNrs.Count == 0)
                        return Fail("No credit nr given");

                    var existingCredits = context.CreditHeadersQueryable.Where(x => creditNrs.Contains(x.CreditNr)).Select(x => new
                    {
                        x.CreditNr,
                        x.Status,
                        CustomerIds = x.CreditCustomers.Select(y => y.CustomerId),
                        CollateralId = x.CollateralHeaderId,
                        MortgageLoanAgreementNr = x.DatedCreditStrings.Where(y => y.Name == DatedCreditStringCode.MortgageLoanAgreementNr.ToString()).OrderByDescending(y => y.Id).Select(y => y.Value).FirstOrDefault()
                    })
                    .ToList()
                    .ToDictionary(x => x.CreditNr, x => x);

                    if (creditNrs.Any(x => !existingCredits.ContainsKey(x)))
                        return Fail("Credit does not exist");

                    var anyCredit = existingCredits[creditNrs[0]];
                    var collateralIds = new HashSet<int>();
                    var mortgageLoanAgreementNrs = new HashSet<string>();
                    foreach (var creditNr in creditNrs)
                    {
                        var credit = existingCredits[creditNr];
                        bool hasCommonReference = false;

                        if (credit.CollateralId.HasValue)
                        {
                            collateralIds.Add(credit.CollateralId.Value);
                            hasCommonReference = true;
                        }
                        else if (credit.MortgageLoanAgreementNr != null)
                        {
                            mortgageLoanAgreementNrs.Add(credit.MortgageLoanAgreementNr);
                            hasCommonReference = true;
                        }
                        else
                            hasCommonReference = anyCredit.CustomerIds.Intersect(credit.CustomerIds).Any();

                        if (!hasCommonReference)
                            return Fail("Credits need to have a shared customer, collateral or agreement nr to enable placing across them");
                    }

                    if (collateralIds.Count > 1)
                        return Fail("Cannot place across multiple collaterals");

                    if (mortgageLoanAgreementNrs.Count > 1)
                        return Fail("Cannot place across multiple agreement nrs");

                    var activeCreditNrs = creditNrs.Where(x => existingCredits[x].Status == CreditStatus.Normal.ToString()).ToList();

                    if (activeCreditNrs.Count == 0)
                        return Fail("None of the credits are active");

                    return new FindPaymentPlacementCreditNrsResponse
                    {
                        CreditNrs = activeCreditNrs
                    };
                }

                if (searchString.Contains(","))
                    return VerifyCreditNrs(searchString.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).DistinctPreservingOrder().ToList());

                var matchedCredits = context
                    .CreditHeadersQueryable
                    .Select(x => new
                    {
                        Credit = x,
                        MortgageLoanAgreementNr = x.DatedCreditStrings.Where(y => y.Name == DatedCreditStringCode.MortgageLoanAgreementNr.ToString()).OrderByDescending(y => y.Id).Select(y => y.Value).FirstOrDefault(),
                        SharedOcrPaymentReference = x.DatedCreditStrings.Where(y => y.Name == DatedCreditStringCode.SharedOcrPaymentReference.ToString()).OrderByDescending(y => y.Id).Select(y => y.Value).FirstOrDefault(),
                        OcrPaymentReference = x.DatedCreditStrings.Where(y => y.Name == DatedCreditStringCode.OcrPaymentReference.ToString()).OrderByDescending(y => y.Id).Select(y => y.Value).FirstOrDefault(),
                    })
                    .Select(x => new FindPaymentPlacementMatchItem
                    {
                        CreditNr = x.Credit.CreditNr,
                        IsMatchedByCreditNr = x.Credit.CreditNr == searchString,
                        IsMatchedBySharedOcrPaymentReference = x.SharedOcrPaymentReference == searchString,
                        IsMatchedByMortgageLoanAgreementNr = x.MortgageLoanAgreementNr == searchString,
                        IsMatchedByOcrPaymentReference = x.OcrPaymentReference == searchString,
                        IsMatchedByNotificationOcrPaymentReference = x.Credit.Notifications.Any(y => y.OcrPaymentReference == searchString)
                    })
                    .Where(x => x.IsMatchedByCreditNr || x.IsMatchedByMortgageLoanAgreementNr || x.IsMatchedBySharedOcrPaymentReference || x.IsMatchedByOcrPaymentReference || x.IsMatchedByNotificationOcrPaymentReference)
                    .ToList();

                var isMatchedFilters = new List<Func<FindPaymentPlacementMatchItem, bool>>
                {
                    x => x.IsMatchedByCreditNr,
                    x => x.IsMatchedBySharedOcrPaymentReference,
                    x => x.IsMatchedByMortgageLoanAgreementNr,
                    x => x.IsMatchedByOcrPaymentReference,
                    x => x.IsMatchedByNotificationOcrPaymentReference
                };

                foreach (var isMatched in isMatchedFilters)
                {
                    var matchedCreditNrs = matchedCredits.Select(x => x.CreditNr).ToList();
                    if (matchedCreditNrs.Count > 0)
                        return VerifyCreditNrs(matchedCreditNrs);
                }

                return new FindPaymentPlacementCreditNrsResponse
                {
                    CreditNrs = new List<string>()
                };
            }
        }

        private class FindPaymentPlacementMatchItem
        {
            public string CreditNr { get; set; }
            public bool IsMatchedByCreditNr { get; set; }
            public bool IsMatchedBySharedOcrPaymentReference { get; set; }
            public bool IsMatchedByMortgageLoanAgreementNr { get; set; }
            public bool IsMatchedByOcrPaymentReference { get; set; }
            public bool IsMatchedByNotificationOcrPaymentReference { get; set; }
        }

        private List<ExternalPaymentData> GetPaymentsPlacementTempData(IncomingPaymentFileWithOriginal paymentfile, Dictionary<string, List<string>> creditNrsByOcr)
        {
            Func<string, IncomingPaymentHeaderItemCode, bool, ItemTmp> optItem = (v, c, s) =>
                string.IsNullOrWhiteSpace(v) ? null : new ItemTmp { Code = c, Value = v, IsSensitive = s };

            return paymentfile.Accounts.SelectMany(account => account.DateBatches.SelectMany(date => date.Payments.Select(payment =>
                new ExternalPaymentData
                {
                    ExternalId = payment.ExternalId,
                    Amount = payment.Amount,
                    BookKeepingDate = date.BookKeepingDate,
                    OcrReference = payment.OcrReference,
                    Items = new ItemTmp[]
                    {
                            optItem(payment.ExternalId, IncomingPaymentHeaderItemCode.ExternalId, false),
                            optItem(payment.OcrReference, IncomingPaymentHeaderItemCode.OcrReference, false),
                            optItem(account.AccountNr.NormalizedValue, IncomingPaymentHeaderItemCode.ClientAccountIban, false),
                            optItem(payment.CustomerName, IncomingPaymentHeaderItemCode.CustomerName, true),
                            optItem(payment.CustomerAddressTownName, IncomingPaymentHeaderItemCode.CustomerAddressTownName, true),
                            optItem(payment.CustomerAddressStreetName, IncomingPaymentHeaderItemCode.CustomerAddressStreetName, true),
                            optItem(payment.CustomerAddressBuildingNumber, IncomingPaymentHeaderItemCode.CustomerAddressBuildingNumber, true),
                            optItem(payment.CustomerAddressCountry, IncomingPaymentHeaderItemCode.CustomerAddressCountry, true),
                            optItem(payment.CustomerAddressPostalCode, IncomingPaymentHeaderItemCode.CustomerAddressPostalCode, true),
                            optItem(payment.AutogiroPayerNumber, IncomingPaymentHeaderItemCode.AutogiroPayerNumber, false),
                            optItem(payment.InformationText, IncomingPaymentHeaderItemCode.NoteText, true),
                            optItem(payment.CustomerOrgnr?.NormalizedValue, IncomingPaymentHeaderItemCode.CustomerOrgnr, true),
                            (payment.CustomerAddressLines == null || payment.CustomerAddressLines.Count == 0) ? null : new ItemTmp { Code = IncomingPaymentHeaderItemCode.CustomerAddressLines, Value = string.Join(", ", payment.CustomerAddressLines), IsSensitive = true }
                    }.Where(x => x != null).ToArray(),
                    PlaceAgainstCreditNrs = string.IsNullOrWhiteSpace(payment.OcrReference) ? null : creditNrsByOcr?.Opt(payment.OcrReference)
                }))).ToList();
        }

        private class ItemTmp
        {
            public IncomingPaymentHeaderItemCode Code { get; set; }
            public string Value { get; set; }
            public bool IsSensitive { get; set; }


        }

        private class ExternalPaymentData
        {
            public string ExternalId { get; set; }
            public decimal Amount { get; set; }
            public DateTime BookKeepingDate { get; set; }
            public string OcrReference { get; set; }
            public ItemTmp[] Items { get; set; }
            public List<string> PlaceAgainstCreditNrs { get; set; }
        }
    }

    public class PaymentPlacementSuggestionRequest
    {
        [Required]
        public int PaymentId { get; set; }

        [Required]
        public List<string> CreditNrs { get; set; }

        public bool? OnlyPlaceAgainstNotified { get; set; }
        /// <summary>
        /// For builtin types:
        /// b_Capital, b_Interest, b_NotificationFee, b_ReminderFee
        /// For custom types:
        /// c_[customCode]
        /// </summary>
        public string OnlyPlaceAgainstPaymentOrderItemUniqueId { get; set; }

        [NonNegativeNumber()]
        public decimal? MaxPlacedAmount { get; set; }
    }

    public class PaymentPlacementRequest
    {
        [Required]
        public int PaymentId { get; set; }

        [Required]
        public MultiCreditPaymentPlacementInstruction Instruction { get; set; }
    }

    public class FindPaymentPlacementCreditNrsRequest
    {
        /// <summary>
        /// Can be shared ocr, notification ocr, agreement nr, credit nr or a comma separated list of credit nrs.
        /// </summary>
        [Required]
        public string SearchString { get; set; }
    }

    public class FindPaymentPlacementCreditNrsResponse
    {
        public List<string> CreditNrs { get; set; }
        public string FailedMessage { get; set; }
    }

    public class PaymentPlacementInitialDataRequest
    {
        [Required]
        public int PaymentId { get; set; }
    }

    public class PaymentPlacementInitialDataResponse
    {
        public List<PaymentPlacementInitialDataItem> Items { get; set; }
        public int Id { get; internal set; }
        public List<string> MatchedCreditNrs { get; internal set; }
        public DateTime PaymentDate { get; internal set; }
        public decimal UnplacedAmount { get; internal set; }
    }

    public class PaymentPlacementInitialDataItem
    {
        public int ItemId { get; internal set; }
        public string Name { get; internal set; }
        public bool IsEncrypted { get; internal set; }
        public string Value { get; internal set; }
        public long? DecryptionId { get; set; }
    }
}