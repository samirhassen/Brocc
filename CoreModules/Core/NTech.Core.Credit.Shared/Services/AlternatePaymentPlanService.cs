using nCredit;
using nCredit.DbModel.BusinessEvents;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using Newtonsoft.Json;
using NTech.Banking.Conversion;
using NTech.Banking.LoanModel;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DbModel;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Infrastructure.CoreValidation;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services
{
    public class AlternatePaymentPlanService : BusinessEventManagerOrServiceBase
    {
        private readonly CreditContextFactory contextFactory;
        private readonly INotificationProcessSettingsFactory notificationProcessSettingsFactory;
        private readonly ICreditEnvSettings envSettings;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly CachedSettingsService settingsService;
        private readonly ICustomerClient customerClient;
        private readonly IMustacheTemplateRenderingService templateRenderingService;
        private readonly PaymentOrderService paymentOrderService;

        public AlternatePaymentPlanService(CreditContextFactory contextFactory, INotificationProcessSettingsFactory notificationProcessSettingsFactory, ICreditEnvSettings envSettings,
            IClientConfigurationCore clientConfiguration, CachedSettingsService settingsService, ICustomerClient customerClient, 
            INTechCurrentUserMetadata user, ICoreClock clock, IMustacheTemplateRenderingService templateRenderingService, PaymentOrderService paymentOrderService) : base(user, clock, clientConfiguration)
        {
            this.contextFactory = contextFactory;
            this.notificationProcessSettingsFactory = notificationProcessSettingsFactory;
            this.envSettings = envSettings;
            this.settingsService = settingsService;
            this.customerClient = customerClient;
            this.templateRenderingService = templateRenderingService;
            this.paymentOrderService = paymentOrderService;
            this.clientConfiguration = clientConfiguration;
        }

        public CreditAlternatePaymentPlanState GetPaymentPlanState(string creditNr, ICreditContextExtended context)
        {
            var credit = context.CreditHeadersQueryable.Where(x => x.CreditNr == creditNr).Select(x => new
            {
                HasActiveTerminationLetter = x.TerminationLetters.Any(y => y.SuspendsCreditProcess == true && y.InactivatedByBusinessEventId == null),
                x.Status,
                HasActivePaymentPlan = x.AlternatePaymentPlans.Any(y => y.CancelledByEventId == null && y.FullyPaidByEventId == null),
                HasActiveFuturePaymentFreeMonth = x.CreditFuturePaymentFreeMonths.Any(y => y.CancelledByBusinessEventId == null && y.CommitedByEventBusinessEventId == null),
                NotificationDueDay = x.DatedCreditValues.Where(y => y.Name == DatedCreditValueCode.NotificationDueDay.ToString()).OrderByDescending(y => y.Id).Select(y => (decimal?)y.Value).FirstOrDefault(),
            }).SingleOrDefault();

            var paymentPlanPaidState = new PaymentPlanState();
            if (credit != null && credit.HasActivePaymentPlan)
            {
                paymentPlanPaidState = GetPaymentPlanPaidState(creditNr, context);
            }

            if (credit == null)
                throw new NTechCoreWebserviceException("No such credit exists") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            var isNewPaymentPlanPossible = true;

            if (credit.Status != CreditStatus.Normal.ToString())
                isNewPaymentPlanPossible = false;

            if (credit.HasActivePaymentPlan)
                isNewPaymentPlanPossible = false;

            if (credit.HasActiveTerminationLetter)
                isNewPaymentPlanPossible = false;

            if (credit.HasActiveFuturePaymentFreeMonth)
                isNewPaymentPlanPossible = false;

            var notifications = CreditNotificationDomainModel.CreateForCredit(creditNr, context, paymentOrderService.GetPaymentOrderItems(), onlyFetchOpen: true).Values;
            if (!notifications.Any())
                isNewPaymentPlanPossible = false;

            return new CreditAlternatePaymentPlanState
            {
                CreditNr = creditNr,
                IsNewPaymentPlanPossible = isNewPaymentPlanPossible,
                PaymentPlanState = credit.HasActivePaymentPlan ? paymentPlanPaidState : null
            };
        }

        public StartPaymentPlanResponse StartPaymentPlan(ValidateOrStartAlternatePaymentPlanRequest request)
        {
            if (!IsPaymentPlanEnabled)
                throw new NTechCoreWebserviceException("Payment plans are not enabled. Add the feature ntech.feature.paymentplan to enable them");

            using (var context = contextFactory.CreateContext())
            {
                var credit = context.CreditHeadersQueryable.Where(x => x.CreditNr == request.CreditNr).Select(x => new
                {
                    HasActiveTerminationLetter = x.TerminationLetters.Any(y => y.SuspendsCreditProcess == true && y.InactivatedByBusinessEventId == null),
                    x.Status,
                    HasActivePaymentPlan = x.AlternatePaymentPlans.Any(y => y.CancelledByEventId == null && y.FullyPaidByEventId == null),
                    HasActiveFuturePaymentFreeMonth = x.CreditFuturePaymentFreeMonths.Any(y => y.CancelledByBusinessEventId == null && y.CommitedByEventBusinessEventId == null),
                    NotificationDueDay = x.DatedCreditValues.Where(y => y.Name == DatedCreditValueCode.NotificationDueDay.ToString()).OrderByDescending(y => y.Id).Select(y => (decimal?)y.Value).FirstOrDefault()
                }).SingleOrDefault();

                if (!IsPaymentPlanValid(request.CreditNr, request.PaymentPlan.Count, out string failedMessage))
                {
                    throw new NTechCoreWebserviceException(failedMessage) { IsUserFacing = true, ErrorHttpStatusCode = 400 };
                }

                var notifications = CreditNotificationDomainModel.CreateForCredit(request.CreditNr, context, paymentOrderService.GetPaymentOrderItems(), onlyFetchOpen: true).Values;

                if (!notifications.Any())
                    throw new NTechCoreWebserviceException("Credit has no unpaid notifications") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                var paymentPlanMonths = request.PaymentPlan.Select(result => new AlternatePaymentPlanSpecificationMonth
                {
                    DueDate = result.DueDate,
                    MonthAmount = result.PaymentPlanAmount
                }).ToList();

                return StartPaymentPlanFromSpecification(new AlternatePaymentPlanSpecification
                {
                    CreditNr = request.CreditNr,
                    Months = paymentPlanMonths,
                    RequiredPaymentPlanSum = paymentPlanMonths.Sum(x => x.MonthAmount)
                });
            } 
        }

        public StartPaymentPlanResponse StartPaymentPlanFromSpecification(AlternatePaymentPlanSpecification paymentPlan)
        {
            if (!IsPaymentPlanEnabled)
                throw new NTechCoreWebserviceException("Payment plans are not enabled. Add the feature ntech.feature.paymentplan to enable them");

            //If the amount check is removed, refactor to keep the checks from suggest for credit exists, active payment plan and so on.
            var suggestedPaymentPlan = GetSuggestedPaymentPlan(new GetPaymentPlanSuggestedRequest
            {
                CreditNr = paymentPlan.CreditNr,
                NrOfPayments = paymentPlan.Months.Count,
                ForceStartNextMonth = false //Does not matter. We wont use the dates from the suggestion for anything
            });

            var suggestedTotalAmount = suggestedPaymentPlan.Months.Sum(x => x.MonthAmount);

            if (paymentPlan.Months.Sum(x => x.MonthAmount) < suggestedTotalAmount) //Allow paying more than required but not less
                throw new NTechCoreWebserviceException("Payment plan amount too low") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            using (var context = contextFactory.CreateContext())
            {
                var credit = CreditDomainModel.PreFetchForSingleCredit(paymentPlan.CreditNr, context, envSettings);
                var today = context.CoreClock.Today;
                var remainingPaymentCount = credit.GetAmortizationModel(today).UsingActualAnnuityOrFixedMonthlyCapital(
                    annuityAmount =>
                        PaymentPlanCalculation.BeginCreateWithAnnuity(
                            credit.GetNotNotifiedCapitalBalance(today),
                            annuityAmount,
                            credit.GetInterestRatePercent(today), null, envSettings.CreditsUse360DayInterestYear)
                        .EndCreate().Payments.Count,
                    fixedMonthlyAmount =>
                        PaymentPlanCalculation.BeginCreateWithFixedMonthlyCapitalAmount(
                            credit.GetNotNotifiedCapitalBalance(today),
                            fixedMonthlyAmount,
                            credit.GetInterestRatePercent(today), null, null, envSettings.CreditsUse360DayInterestYear)
                        .EndCreate().Payments.Count
                );

                var evt = AddBusinessEvent(BusinessEventType.AlternatePaymentPlanCreated, context);
                context.AddDatedCreditString(context.FillInfrastructureFields(new DatedCreditString
                {
                    BusinessEvent = evt,
                    TransactionDate = evt.TransactionDate,
                    CreditNr = paymentPlan.CreditNr,
                    Name = DatedCreditStringCode.IsStandardDefaultProcessSuspended.ToString(),
                    Value = "true"
                }));
                var plan = context.FillInfrastructureFields(new Credit.Shared.DbModel.AlternatePaymentPlanHeader
                {
                    CreatedByEvent = evt,
                    CreditNr = paymentPlan.CreditNr,
                    FuturePaymentPlanMonthCount = remainingPaymentCount,
                    Months = new List<Credit.Shared.DbModel.AlternatePaymentPlanMonth>()
                });
                context.AddAlternatePaymentPlanHeaders(plan);
                var totalAmount = 0m;
                foreach (var monthlyPayment in paymentPlan.Months)
                {
                    totalAmount += monthlyPayment.MonthAmount;
                    plan.Months.Add(context.FillInfrastructureFields(new Credit.Shared.DbModel.AlternatePaymentPlanMonth
                    {
                        MonthAmount = monthlyPayment.MonthAmount,
                        TotalAmount = totalAmount,
                        DueDate = monthlyPayment.DueDate,
                        PaymentPlan = plan
                    }));
                }

                if (IsNotificationCapitalizationEnabled)
                {
                    var notifications = CreditNotificationDomainModel.CreateForCredit(paymentPlan.CreditNr, context, paymentOrderService.GetPaymentOrderItems(), onlyFetchOpen: true).Values;
                    var newCapitalAmount = 0m;
                    var writeOff = new Lazy<WriteoffHeader>(() =>
                    {
                        var h = context.FillInfrastructureFields(new WriteoffHeader { TransactionDate = today, BookKeepingDate = today });
                        context.AddWriteoffHeaders(h);
                        return h;
                    });
                    foreach (var notification in notifications)
                    {
                        foreach (var amountType in Enums.GetAllValues<CreditDomainModel.AmountType>())
                        {
                            if (amountType == CreditDomainModel.AmountType.Capital)
                            {
                                var amount = notification.GetRemainingBalance(today, amountType);
                                context.AddAccountTransactions(CreateTransaction(TransactionAccountType.NotNotifiedCapital, amount, today, evt, creditNr: paymentPlan.CreditNr,
                                    notificationId: notification.NotificationId, writeOff: writeOff.Value));
                            }
                            else
                            {
                                var amount = notification.GetRemainingBalance(today, amountType);
                                var transactionType = CreditDomainModel.MapNonCapitalAmountTypeToAccountType(amountType);
                                context.AddAccountTransactions(CreateTransaction(transactionType, -amount, today, evt, creditNr: paymentPlan.CreditNr,
                                    notificationId: notification.NotificationId, writeOff: writeOff.Value));
                                newCapitalAmount += amount;
                            }
                        }
                    }
                    if (newCapitalAmount > 0m)
                    {
                        context.AddAccountTransactions(CreateTransaction(TransactionAccountType.CapitalDebt, newCapitalAmount, today, evt, creditNr: paymentPlan.CreditNr));
                        context.AddAccountTransactions(CreateTransaction(TransactionAccountType.NotNotifiedCapital, newCapitalAmount, today, evt, creditNr: paymentPlan.CreditNr));
                    }
                    var notificationIds = notifications.Select(x => x.NotificationId).ToList();
                    foreach (var dbNotification in context.CreditNotificationHeadersQueryable.Where(x => notificationIds.Contains(x.Id)).ToList())
                    {
                        dbNotification.ClosedTransactionDate = today;
                    }
                    plan.MinCapitalizedDueDate = notifications.Min(x => (DateTime?)x.DueDate);
                }

                //send secure message to applicant1 if enabled
                var settings = settingsService.LoadSettings("altPaymentPlanSecureMessageTemplates");
                var isSendMessageOnCreatedEnabled = settings["onCreated"] == "true";

                if (isSendMessageOnCreatedEnabled)
                {
                    var customerId = context.CreditCustomersQueryable
                        .Where(customer => customer.CreditNr == paymentPlan.CreditNr && customer.ApplicantNr == 1)
                        .Select(customer => customer.CustomerId)
                        .FirstOrDefault();

                    var mines = GetOnCreatedPrintContext(context, plan);
                    AlternatePaymentPlanSecureMessagesService.SendSecureMessageWithSettingsTemplate(settings, "onCreatedTemplateText",
                        customerId, credit.CreditNr, mines, customerClient, envSettings, templateRenderingService);
                }

                var amountCommentPart = $" Amounts: " + string.Join(" ", plan.Months.Select(x => x.MonthAmount.ToString("C", CommentFormattingCulture)));
                AddComment($"Alternate payment plan started. {plan.Months.Count()} payments totalling {plan.Months.Sum(x => x.MonthAmount).ToString("C", CommentFormattingCulture)} starting {plan.Months.Min(x => x.DueDate).ToString("yyyy-MM-dd")} and ending {plan.Months.Max(x => x.DueDate).ToString("yyyy-MM-dd")}. {amountCommentPart}",
                    BusinessEventType.AlternatePaymentPlanCreated, context, creditNr: credit.CreditNr, evt: evt);

                context.SaveChanges();

                return new StartPaymentPlanResponse
                {
                    AlternatePaymentPlanId = plan.Id
                };
            }
        }

        public AlternatePaymentPlanSpecification GetSuggestedPaymentPlan(GetPaymentPlanSuggestedRequest request)
        {
            if (!IsPaymentPlanEnabled)
                throw new NTechCoreWebserviceException("Payment plans are not enabled. Add the feature ntech.feature.paymentplan to enable them");

            var creditNr = request.CreditNr;
            var nrOfPayments = request.NrOfPayments ?? 6;
            var forceStartNextMonth = request.ForceStartNextMonth ?? false;

            using (var context = contextFactory.CreateContext())
            {
                var credit = context.CreditHeadersQueryable.Where(x => x.CreditNr == creditNr).Select(x => new
                {
                    HasActiveTerminationLetter = x.TerminationLetters.Any(y => y.SuspendsCreditProcess == true && y.InactivatedByBusinessEventId == null),
                    x.Status,
                    HasActivePaymentPlan = x.AlternatePaymentPlans.Any(y => y.CancelledByEventId == null && y.FullyPaidByEventId == null),
                    HasActiveFuturePaymentFreeMonth = x.CreditFuturePaymentFreeMonths.Any(y => y.CancelledByBusinessEventId == null && y.CommitedByEventBusinessEventId == null),
                    NotificationDueDay = x.DatedCreditValues.Where(y => y.Name == DatedCreditValueCode.NotificationDueDay.ToString()).OrderByDescending(y => y.Id).Select(y => (decimal?)y.Value).FirstOrDefault()
                }).SingleOrDefault();

                if (!IsPaymentPlanValid(creditNr, nrOfPayments, out string failedMessage))
                {
                    throw new NTechCoreWebserviceException(failedMessage) { IsUserFacing = true, ErrorHttpStatusCode = 400 };
                }

                var notifications = CreditNotificationDomainModel.CreateForCredit(creditNr, context, paymentOrderService.GetPaymentOrderItems(), onlyFetchOpen: true).Values;

                if (!notifications.Any())
                    throw new NTechCoreWebserviceException("Credit has no unpaid notifications") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                var thisMonth = Month.ContainingDate(context.CoreClock.Today);

                var dueDay = credit.NotificationDueDay.HasValue
                    ? (int)credit.NotificationDueDay.Value
                    : notificationProcessSettingsFactory.GetByCreditType(envSettings.ClientCreditType).NotificationDueDay;

                //If there is at least a week until the normal due date this month we suggest starting this month otherwise we suggest next month
                var startThisMonth = forceStartNextMonth
                    ? false
                    : thisMonth.GetDayDate(dueDay) > context.CoreClock.Today.AddDays(7);

                var firstDueDate = (startThisMonth ? thisMonth : thisMonth.NextMonth).GetDayDate(dueDay);

                var unpaidNotificationsAmount = notifications.Sum(x => x.GetRemainingBalance(context.CoreClock.Today));
                var monthAmount = Math.Floor(unpaidNotificationsAmount / ((decimal)nrOfPayments));

                if (monthAmount < 1m)
                    throw new NTechCoreWebserviceException("Monthly payment too low") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                var lastMonthAmount = unpaidNotificationsAmount - (monthAmount * (nrOfPayments - 1));

                var paymentPlanMonths = Enumerable.Range(1, nrOfPayments - 1).Select(monthNr => new AlternatePaymentPlanSpecificationMonth
                {
                    DueDate = firstDueDate.AddMonths(monthNr - 1),
                    MonthAmount = monthAmount
                }).Concat(Enumerables.Singleton(new AlternatePaymentPlanSpecificationMonth
                {
                    DueDate = firstDueDate.AddMonths(nrOfPayments - 1),
                    MonthAmount = lastMonthAmount
                })).ToList();

                return new AlternatePaymentPlanSpecification
                {
                    CreditNr = creditNr,
                    Months = paymentPlanMonths,
                    RequiredPaymentPlanSum = paymentPlanMonths.Sum(x => x.MonthAmount)
                };
            }
        }
                
        public bool IsPaymentPlanValid(string creditNr, int nrOfPayments, out string failedMessage, List<PaymentPlanPaidAmountsModel> paymentPlan = null)
        {
            if (!IsPaymentPlanEnabled)
                throw new NTechCoreWebserviceException("Payment plans are not enabled. Add the feature ntech.feature.paymentplan to enable them");

            using (var context = contextFactory.CreateContext())
            {
                var credit = context.CreditHeadersQueryable.Where(x => x.CreditNr == creditNr).Select(x => new
                {
                    HasActiveTerminationLetter = x.TerminationLetters.Any(y => y.SuspendsCreditProcess == true && y.InactivatedByBusinessEventId == null),
                    x.Status,
                    HasActivePaymentPlan = x.AlternatePaymentPlans.Any(y => y.CancelledByEventId == null && y.FullyPaidByEventId == null),
                    HasActiveFuturePaymentFreeMonth = x.CreditFuturePaymentFreeMonths.Any(y => y.CancelledByBusinessEventId == null && y.CommitedByEventBusinessEventId == null),
                    NotificationDueDay = x.DatedCreditValues.Where(y => y.Name == DatedCreditValueCode.NotificationDueDay.ToString()).OrderByDescending(y => y.Id).Select(y => (decimal?)y.Value).FirstOrDefault()
                }).SingleOrDefault();

                if (credit == null)
                {
                    failedMessage = "No such credit exists";
                    return false;
                }

                if (credit.Status != CreditStatus.Normal.ToString())
                {
                    failedMessage = "Credit is not active";
                    return false;
                }

                if (credit.HasActivePaymentPlan)
                {
                    failedMessage = "Credit already has an active alternate payment plan";
                    return false;
                }

                if (credit.HasActiveTerminationLetter)
                {
                    failedMessage = "Credit has an active termination letter";
                    return false;
                }

                if (credit.HasActiveFuturePaymentFreeMonth)
                {
                    failedMessage = "Credit has scheduled payment free months";
                    return false;
                }

                if (nrOfPayments < 2)
                {
                    failedMessage = "At least two payments required";
                    return false;
                }

                if (nrOfPayments > 12)
                {
                    failedMessage = "Max 12 payments";
                    return false;
                }


                if (paymentPlan != null)
                {
                    //arbitrary max amount, will be replaced with loan max amount requirements
                    if (paymentPlan.Sum(x => x.PaymentPlanAmount) >= 1000000000000000m)
                    {
                        failedMessage = "Payment plan amount too high";
                        return false;
                    }

                    //If the amount check is removed, refactor to keep the checks from suggest for credit exists, active payment plan and so on.
                    var suggestedPaymentPlan = GetSuggestedPaymentPlan(new GetPaymentPlanSuggestedRequest
                    {
                        CreditNr = creditNr,
                        NrOfPayments = nrOfPayments,
                        ForceStartNextMonth = false //Does not matter. We wont use the dates from the suggestion for anything
                    });

                    var suggestedTotalAmount = suggestedPaymentPlan.Months.Sum(x => x.MonthAmount);

                    if (paymentPlan.Sum(x => x.PaymentPlanAmount) < suggestedTotalAmount) //Allow paying more than required but not less
                    {
                        failedMessage = "Payment plan amount too low";
                        return false;
                    }
                }
            }

            failedMessage = "";
            return true;
        }

        public void CancelDefaultedOrCompleteFullyPaidPaymentsPlans(ICreditContextExtended context,
            Lazy<BusinessEvent> completeEvent, Lazy<BusinessEvent> cancelEvent, List<string> onlyTheseCreditNrs = null)
        {
            CompleteFullyPaidPaymentPlans(context, completeEvent, onlyTheseCreditNrs: onlyTheseCreditNrs);
            CancelDefaultedPaymentPlans(context, cancelEvent, onlyTheseCreditNrs: onlyTheseCreditNrs);
        }

        public void CancelDefaultedPaymentPlans(ICreditContextExtended context, Lazy<BusinessEvent> evt, List<string> onlyTheseCreditNrs = null)
        {
            var today = context.CoreClock.Today;
            var plans = GetActivePaymentPlansCompleteOrCancelData(context, onlyTheseCreditNrs: onlyTheseCreditNrs);
            var comments = new Dictionary<string, string>();
            var annuityPlansToRecalculate = new List<(int PaidMonthCount, PaymentPlanDataCompleteOrCancelData Plan)>();

            var processSettings = notificationProcessSettingsFactory.GetByCreditType(envSettings.ClientCreditType);
            var graceDays = processSettings.FirstReminderDaysBefore ?? 7;

            foreach (var plan in plans)
            {
                if (plan.IsLateOnPayments(today, graceDays))
                {
                    var lastOverdueMonth = plan.GetLastOverdueMonth(today, graceDays);
                    comments[plan.PlanHeader.CreditNr] = $"Alternate payment plan cancelled. Expected payments of at least {lastOverdueMonth.TotalAmount.ToString("C", CommentFormattingCulture)} by {lastOverdueMonth.DueDate.ToString("yyyy-MM-dd")} but got only {plan.GetTotalPaidAmount().ToString("C", CommentFormattingCulture)}";
                    plan.PlanHeader.CancelledByEvent = evt.Value;
                    AddDatedCreditString(DatedCreditStringCode.IsStandardDefaultProcessSuspended.ToString(), "false", plan.PlanHeader.CreditNr, evt.Value, context);
                    var paidMonths = plan.PlanMonths.Where(x => x.DueDate < lastOverdueMonth.DueDate).ToList();
                    if (paidMonths.Any())
                    {
                        var lastPaidDueDate = paidMonths.Max(x => x.DueDate);
                        AddDatedCreditString(DatedCreditStringCode.NextInterestFromDate.ToString(),
                            lastPaidDueDate.AddDays(1).ToString("yyyy-MM-dd"), plan.PlanHeader.CreditNr, evt.Value, context);
                    }
                    if (plan.AmortizationModel == AmortizationModelCode.MonthlyAnnuity.ToString())
                    {
                        annuityPlansToRecalculate.Add((PaidMonthCount: paidMonths.Count, Plan: plan));
                    }
                }
            }
            foreach (var planGroup in annuityPlansToRecalculate.ToArray().SplitIntoGroupsOfN(20))
            {
                var credits = CreditDomainModel.PreFetchForCredits(context, planGroup.Select(x => x.Plan.PlanHeader.CreditNr).ToArray(), envSettings);
                foreach (var plan in planGroup)
                {
                    var credit = credits[plan.Plan.PlanHeader.CreditNr];
                    var newCommentPart = RecalculateAnnuity(credit, plan.Plan, context, evt, plan.PaidMonthCount);
                    comments[plan.Plan.PlanHeader.CreditNr] += newCommentPart;
                }
            }
            foreach (var comment in comments)
                AddComment(comment.Value, evt.Value.EventType, null, context, creditNr: comment.Key, evt: evt.Value);
        }

        public void CancelPaymentPlan(ICreditContextExtended context, string creditNr, bool isManualCancel)
        {
            var b = new BusinessEventManagerOrServiceBase(context.CurrentUser, context.CoreClock, clientConfiguration);
            var evt = new Lazy<BusinessEvent>(() => b.AddBusinessEvent(isManualCancel ? BusinessEventType.AlternatePaymentPlanCancelledManually : BusinessEventType.AlternatePaymentPlanCancelled, context));

            var today = context.CoreClock.Today;
            var plans = GetActivePaymentPlansCompleteOrCancelData(context, onlyTheseCreditNrs: new List<string> { creditNr });
            var comments = new Dictionary<string, string>();
            foreach (var plan in plans)
            {
                comments[plan.PlanHeader.CreditNr] = $"Alternate payment plan {(isManualCancel ? "manually " : "")}cancelled.";
                plan.PlanHeader.CancelledByEvent = evt.Value;
                AddDatedCreditString(DatedCreditStringCode.IsStandardDefaultProcessSuspended.ToString(), "false", plan.PlanHeader.CreditNr, evt.Value, context);
            }

            foreach (var comment in comments)
                AddComment(comment.Value, evt.Value.EventType, null, context, creditNr: comment.Key, evt: evt.Value);

            context.SaveChanges();
        }

        public void CompleteFullyPaidPaymentPlans(ICreditContextExtended context, Lazy<BusinessEvent> evt, List<string> onlyTheseCreditNrs = null)
        {
            var today = context.CoreClock.Today;
            var plans = GetActivePaymentPlansCompleteOrCancelData(context, onlyTheseCreditNrs: onlyTheseCreditNrs);
            var annuityPlansToRecalculate = new List<PaymentPlanDataCompleteOrCancelData>();
            var comments = new Dictionary<string, string>();
            foreach (var plan in plans)
            {
                var totalPaidAmount = plan.TotalPaidNotifiedAmount + plan.ExtraAmortizationAmount;
                if (totalPaidAmount >= plan.GetLastMonth().TotalAmount)
                {
                    comments[plan.PlanHeader.CreditNr] = $"Alternate payment plan completed. Total paid during plan {totalPaidAmount.ToString("C", CommentFormattingCulture)}";
                    plan.PlanHeader.FullyPaidByEvent = evt.Value;
                    AddDatedCreditString(DatedCreditStringCode.IsStandardDefaultProcessSuspended.ToString(), "false", plan.PlanHeader.CreditNr, evt.Value, context);

                    /*
                     * If the customer prepays we start counting interest again from when they pay rather than from the end of the plan which is a future date.
                     * This is since clients want notifications to start up early if the customer prepays rather than wait out the full plan.
                     * The logic used here feels really shaky and will likely need to be revisited.
                     * Note that when we cancel we dont move NextInterestFromDate at all and the customer start paying it from the day after the last previous notification.
                     * That would really be the most correct in this case also but would cause the first notification after the plan starts up to have 6 months of interest so
                     * probably not what the clients want.
                     */
                    var newInterestFromDate = Dates.Min(today, plan.GetLastMonth().DueDate.AddDays(1));

                    AddDatedCreditString(DatedCreditStringCode.NextInterestFromDate.ToString(),
                        newInterestFromDate.ToString("yyyy-MM-dd"), plan.PlanHeader.CreditNr, evt.Value, context);
                    if (plan.AmortizationModel == AmortizationModelCode.MonthlyAnnuity.ToString())
                    {
                        annuityPlansToRecalculate.Add(plan);
                    }
                }
            }
            foreach (var completedPlanGroup in annuityPlansToRecalculate.ToArray().SplitIntoGroupsOfN(20))
            {
                var credits = CreditDomainModel.PreFetchForCredits(context, completedPlanGroup.Select(x => x.PlanHeader.CreditNr).ToArray(), envSettings);
                foreach (var plan in completedPlanGroup)
                {
                    var credit = credits[plan.PlanHeader.CreditNr];
                    var newCommentPart = RecalculateAnnuity(credit, plan, context, evt, plan.PlanMonths.Count);
                    comments[plan.PlanHeader.CreditNr] += newCommentPart;
                }
            }
            foreach (var comment in comments)
                AddComment(comment.Value, evt.Value.EventType, null, context, creditNr: comment.Key, evt: evt.Value);
        }

        private string RecalculateAnnuity(CreditDomainModel credit, PaymentPlanDataCompleteOrCancelData plan, ICreditContextExtended context, Lazy<BusinessEvent> evt, int paidCount)
        {
            var today = Clock.Today;
            var annuityAmount = credit.GetAmortizationModel(today).GetActualAnnuityOrException();
            var notNotifiedAmount = credit.GetNotNotifiedCapitalBalance(today);
            if (notNotifiedAmount <= annuityAmount)
            {
                //sanity check so we dont set up some crazy low annuity if large extra payments have come in
                return " . Repayment time not recalculated due to low balance";
            }

            var newRepaymentTime = plan.PlanHeader.FuturePaymentPlanMonthCount;
            var newAnnuityAmount = PaymentPlanCalculation
                .BeginCreateWithRepaymentTime(notNotifiedAmount, newRepaymentTime, credit.GetInterestRatePercent(today),
                    true, null, envSettings.CreditsUse360DayInterestYear)
                .WithMonthlyFee(credit.GetNotificationFee(today))
                .EndCreate()
                .AnnuityAmount;

            AddDatedCreditValue(DatedCreditValueCode.AnnuityAmount, newAnnuityAmount, evt.Value, context, creditNr: plan.PlanHeader.CreditNr);
            return $" . Repayment time recalculated to {newRepaymentTime}, new annuity {newAnnuityAmount.ToString("C", CommentFormattingCulture)}";
        }

        public List<PaymentPlanDataCompleteOrCancelData> GetActivePaymentPlansCompleteOrCancelData(ICreditContextExtended context, List<string> onlyTheseCreditNrs = null, bool? hasPerLoanDueDay = null)
        {
            return GetActivePaymentPlansCompleteOrCancelDataQueryable(context, onlyTheseCreditNrs: onlyTheseCreditNrs, hasPerLoanDueDay: hasPerLoanDueDay).ToList();
        }

        public static IQueryable<PaymentPlanDataCompleteOrCancelData> GetActivePaymentPlansCompleteOrCancelDataQueryable(
            ICreditContextExtended context,
            List<string> onlyTheseCreditNrs = null,
            bool? hasPerLoanDueDay = null)
        {
            var today = context.CoreClock.Today;
            var query = context.AlternatePaymentPlanHeadersQueryable.Where(x => x.CancelledByEventId == null && x.FullyPaidByEventId == null && x.Credit.Status == CreditStatus.Normal.ToString());
            if (onlyTheseCreditNrs != null && onlyTheseCreditNrs.Count > 0)
                query = query.Where(x => onlyTheseCreditNrs.Contains(x.CreditNr));
            if (hasPerLoanDueDay.HasValue)
                query = query.Where(x => x.Credit.DatedCreditValues.Any(y => y.Name == DatedCreditValueCode.NotificationDueDay.ToString()) == hasPerLoanDueDay.Value);
            return query
                .Select(x => new PaymentPlanDataCompleteOrCancelData
                {
                    PlanHeader = x,
                    PlanMonths = x.Months,
                    TotalPaidNotifiedAmount = -x.Credit.Transactions.Where(y => y.IncomingPaymentId != null && y.WriteoffId == null && y.CreditNotificationId != null && y.BusinessEventId > x.CreatedByEventId).Sum(y => (decimal?)y.Amount) ?? 0m,
                    ExtraAmortizationAmount = -x.Credit.Transactions.Where(y => y.IncomingPaymentId != null && y.WriteoffId == null && y.CreditNotificationId == null && y.AccountCode == TransactionAccountType.CapitalDebt.ToString() && y.BusinessEventId > x.CreatedByEventId).Sum(y => (decimal?)y.Amount) ?? 0m,
                    AmortizationModel = x.Credit.DatedCreditStrings
                        .Where(y => y.Name == DatedCreditStringCode.AmortizationModel.ToString())
                        .OrderByDescending(y => y.Id).Select(y => y.Value).FirstOrDefault() ?? "MonthlyAnnuity",
                    MainApplicantCustomerId = x.Credit.CreditCustomers.Where(y => y.ApplicantNr == 1).Select(y => y.CustomerId).FirstOrDefault()
                });
        }

        //Todo: there is probably a better way to calculate this
        public PaymentPlanState GetPaymentPlanPaidState(string creditNr, ICreditContextExtended context)
        {
            var accountCodes = GetIncludedAccountCodes();
            var paymentPlan = context.CreditHeadersQueryable
                    .Where(x => x.CreditNr == creditNr)
                    .Select(x => new
                    {
                        PaymentPlanMonths = x.AlternatePaymentPlans.Where(y => y.CancelledByEventId == null).OrderByDescending(y => y.ChangedDate).Select(p => p.Months).FirstOrDefault(),
                        PaidAmounts = x.Transactions
                            .Where(y =>
                                y.BusinessEvent.EventDate >= (x.AlternatePaymentPlans.Where(z => z.CancelledByEventId == null).OrderByDescending(c => c.ChangedDate).Select(p => p.ChangedDate).FirstOrDefault()) &&
                                y.IncomingPaymentId != null &&
                                y.WriteoffId == null &&
                                accountCodes.Contains(y.AccountCode))
                            .ToList()
                    })
                    .SingleOrDefault();

            var paymentPlanMonths = paymentPlan.PaymentPlanMonths;
            var paidAmountsList = paymentPlan.PaidAmounts;

            var matchingPayments = new List<PaymentPlanPaidAmountsModel>();
            int paidIndex = 0;
            decimal excessPayment = 0;
            List<(int planMonthI, DateTime? fulfilledDate)> arr = new List<(int planMonthI, DateTime? fulfilledDate)>();

            foreach (var (planMonth, i) in paymentPlanMonths.OrderBy(plan => plan.DueDate).Select((plan, index) => (plan, index)))
            {
                decimal paidThisMonth = 0;

                while (paidIndex < paidAmountsList.Count)
                {
                    paidThisMonth += -paidAmountsList[paidIndex].Amount;

                    if (paidThisMonth >= planMonth.MonthAmount)
                    {
                        arr.Add((planMonthI: paidIndex, fulfilledDate: paidAmountsList[paidIndex].TransactionDate));
                    }

                    paidIndex++;
                }

                paidThisMonth += excessPayment; // Add the carried over excess payment

                var paymentExcess = paidThisMonth - planMonth.MonthAmount;

                if (paymentExcess >= 0)
                {
                    matchingPayments.Add(new PaymentPlanPaidAmountsModel
                    {
                        DueDate = planMonth.DueDate,
                        PaymentPlanAmount = planMonth.MonthAmount,
                        PaidAmount = planMonth.MonthAmount,
                        LatestPaymentDate = arr.Count > i ? (arr[i].fulfilledDate) : paidAmountsList.OrderByDescending(t => t.TransactionDate).Select(p => p.TransactionDate).FirstOrDefault()
                    });

                    excessPayment = paymentExcess; // Carry over the excess payment to the next month
                }
                else
                {
                    matchingPayments.Add(new PaymentPlanPaidAmountsModel
                    {
                        DueDate = planMonth.DueDate,
                        PaymentPlanAmount = planMonth.MonthAmount,
                        PaidAmount = paidThisMonth > 0 ? paidThisMonth : (decimal?)null,
                        LatestPaymentDate = paidThisMonth > 0 ? paidAmountsList.OrderByDescending(d => d.TransactionDate).Select(p => p.TransactionDate).FirstOrDefault() : (DateTime?)null
                    });

                    excessPayment = 0; // Reset excess payment for the next month
                }
            }

            return new PaymentPlanState
            {
                PaymentPlanPaidAmountsResult = matchingPayments,
                AlternatePaymentPlanMonths = paymentPlanMonths
            };
        }

        public bool IsPaymentPlanEnabled => IsPaymentPlanEnabledShared(clientConfiguration);

        public static bool IsPaymentPlanEnabledShared(IClientConfigurationCore clientConfiguration) => clientConfiguration.IsFeatureEnabled("ntech.feature.paymentplan");

        private string[] GetIncludedAccountCodes()
        {
            //Update after testing and briefing which AccountCodes to include
            return new[]
            {
                TransactionAccountType.CapitalDebt.ToString(),
                TransactionAccountType.NotNotifiedCapital.ToString(),
                TransactionAccountType.InterestDebt.ToString(),
                TransactionAccountType.NotificationFeeDebt.ToString(),
                TransactionAccountType.ReminderFeeDebt.ToString()
            };
        }

        private bool IsNotificationCapitalizationEnabled => clientConfiguration.IsFeatureEnabled("ntech.feature.paymentplan.capitalize");

        private Dictionary<string, object> GetOnCreatedPrintContext(ICreditContextExtended context, AlternatePaymentPlanHeader plan)
        {
            var paymentAccountService = new PaymentAccountService(settingsService, envSettings, clientConfiguration);
            var incomingPaymentBankAccountNr = paymentAccountService.GetIncomingPaymentBankAccountNr();
            string payToBankAccount;
            if (clientConfiguration.Country.BaseCountry == "FI")
                payToBankAccount = paymentAccountService.FormatIncomingBankAccountNrForDisplay(incomingPaymentBankAccountNr);
            else if (clientConfiguration.Country.BaseCountry == "SE")
                payToBankAccount = paymentAccountService.FormatIncomingBankAccountNrForDisplay(incomingPaymentBankAccountNr);
            else
                throw new NotImplementedException();

            var months = plan.Months
                        .Select(m => new { dueDate = m.DueDate.ToString("d", PrintFormattingCulture), monthlyAmount = m.MonthAmount.ToString("f2", PrintFormattingCulture) })
                        .ToList();
            var totalAmountToPay = plan.Months.Sum(m => m.MonthAmount).ToString("f2", PrintFormattingCulture);
            var lastDueDate = plan.Months.OrderByDescending(m => m.DueDate).FirstOrDefault().DueDate.ToString("d", PrintFormattingCulture);

            var credit = CreditDomainModel.PreFetchForSingleCredit(plan.CreditNr, context, envSettings);
            var ocrReference = credit.GetOcrPaymentReference(Clock.Today);
            return new Dictionary<string, object>
                {
                    { "payToBankAccount",  payToBankAccount },
                    { "ocrReference", ocrReference
                    },
                    {   
                        "sharedOcrReference", 
                        credit.GetDatedCreditString(Clock.Today, DatedCreditStringCode.SharedOcrPaymentReference, null, allowMissing: true) ?? ocrReference },
                    { "creditNr", plan.CreditNr },
                    { "months", months },
                    { "totalAmountToPay", totalAmountToPay },
                    { "lastDueDate", lastDueDate }
                };
        }

        public class PaymentPlanDataCompleteOrCancelData
        {
            public AlternatePaymentPlanHeader PlanHeader { get; set; }
            public decimal TotalPaidNotifiedAmount { get; set; }
            public decimal ExtraAmortizationAmount { get; set; }
            public string AmortizationModel { get; set; }
            public int MainApplicantCustomerId { get; set; }
            public List<AlternatePaymentPlanMonth> PlanMonths { get; set; }
            public AlternatePaymentPlanMonth GetLastOverdueMonth(DateTime today, int graceDays) => PlanMonths.Where(y => y.DueDate.AddDays(graceDays) < today).OrderByDescending(y => y.DueDate).Select(y => y).FirstOrDefault();
            public decimal GetTotalPaidAmount() => TotalPaidNotifiedAmount + ExtraAmortizationAmount;
            public bool IsLateOnPayments(DateTime today, int graceDays)
            {
                var lastOverdueMonth = GetLastOverdueMonth(today, graceDays);
                return lastOverdueMonth != null && GetTotalPaidAmount() < lastOverdueMonth.TotalAmount;
            }
            public decimal GetMissingPaymentAmount(DateTime today, int graceDays)
            {
                var lastOverdueMonth = GetLastOverdueMonth(today, graceDays);
                return IsLateOnPayments(today, graceDays) ? lastOverdueMonth.TotalAmount - GetTotalPaidAmount() : 0m;
            }
            public AlternatePaymentPlanMonth GetLastMonth() => PlanMonths.OrderByDescending(y => y.DueDate).Select(y => y).FirstOrDefault();
            public AlternatePaymentPlanMonth GetNextMonth(DateTime today) => PlanMonths.Where(y => y.DueDate >= today).OrderBy(y => y.DueDate).Select(y => y).FirstOrDefault();
        }
    }

    public class CreateAlternatePaymentPlanSuggestionRequest
    {
        [Required]
        public string CreditNr { get; set; }

        [ListSizeRange(2, 180)]
        [Required]
        public int NrOfPayments { get; set; }

        public bool? ForceStartNextMonth { get; set; }
        public List<PaymentPlanPaidAmountsModel> PaymentPlanPaidAmounts { get; set; }
    }

    public class AlternatePaymentPlanSpecification
    {
        [Required]
        public string CreditNr { get; set; }
        [Required]
        public List<AlternatePaymentPlanSpecificationMonth> Months { get; set; }
        public decimal RequiredPaymentPlanSum { get; set; }
        public bool ForceStartNextMonth { get; set; }
    }

    public class AlternatePaymentPlanSpecificationMonth
    {
        [Required]
        public DateTime DueDate { get; set; }
        [Required]
        public decimal MonthAmount { get; set; }
    }

    public class StartPaymentPlanResponse
    {
        public int AlternatePaymentPlanId { get; set; }
    }

    public class ValidatePaymentPlanResponse
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class CreditAlternatePaymentPlanState
    {
        public string CreditNr { get; set; }
        public bool IsNewPaymentPlanPossible { get; set; }
        public PaymentPlanState PaymentPlanState { get; set; }
    }

    public class PaymentPlanState
    {
        public List<AlternatePaymentPlanMonth> AlternatePaymentPlanMonths { get; set; }
        public List<PaymentPlanPaidAmountsModel> PaymentPlanPaidAmountsResult { get; set; }
    }

    public class PaymentPlanPaidAmountsModel
    {
        public DateTime DueDate { get; set; }
        public decimal PaymentPlanAmount { get; set; }
        public decimal? PaidAmount { get; set; }
        public DateTime? LatestPaymentDate { get; set; }
    }

    public class GetPaymentPlanSuggestedRequest
    {
        [Required]
        public string CreditNr { get; set; }
        public int? NrOfPayments { get; set; }
        public bool? ForceStartNextMonth { get; set; }
    }

    public class ValidateOrStartAlternatePaymentPlanRequest
    {
        [Required]
        public string CreditNr { get; set; }
        [Required]
        public List<PaymentPlanPaidAmountsModel> PaymentPlan { get; set; }
    }
}
