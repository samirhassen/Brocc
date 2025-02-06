using nCredit.DomainModel;
using nCredit;
using NTech.Core.Credit.Shared.Database;
using System;
using System.Linq;
using NTech.Core.Module.Shared.Infrastructure;
using nCredit.DbModel.DomainModel;
using System.Collections.Generic;
using nCredit.DbModel.BusinessEvents;

namespace NTech.Core.Credit.Shared.Services
{
    public class AmortizationPlanService
    {
        private readonly CreditContextFactory creditContextFactory;
        private readonly ICreditEnvSettings envSettings;
        private readonly ICoreClock clock;
        private readonly INotificationProcessSettingsFactory notificationProcessSettingsFactory;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly INTechCurrentUserMetadata currentUser;

        public AmortizationPlanService(CreditContextFactory creditContextFactory, ICreditEnvSettings envSettings, ICoreClock clock,
            INotificationProcessSettingsFactory notificationProcessSettingsFactory, IClientConfigurationCore clientConfiguration, 
            INTechCurrentUserMetadata currentUser)
        {
            this.creditContextFactory = creditContextFactory;
            this.envSettings = envSettings;
            this.clock = clock;
            this.notificationProcessSettingsFactory = notificationProcessSettingsFactory;
            this.clientConfiguration = clientConfiguration;
            this.currentUser = currentUser;
        }

        public static (AmortizationPlan Plan, HistoricalCreditModel Model) GetAmortizationPlanAndModelShared(string creditNr, int? customerId, CreditContextFactory creditContextFactory,
            ICreditEnvSettings envSettings, INotificationProcessSettingsFactory notificationProcessSettingsFactory, IClientConfigurationCore clientConfiguration)
        {
            HistoricalCreditModel model;
            using (var context = creditContextFactory.CreateContext())
            {
                model = AmortizationPlan.GetHistoricalCreditModel(creditNr, context, envSettings.IsMortgageLoansEnabled);

                //Check Customer authorized
                if (customerId.HasValue)
                {
                    var creditCustomers = context
                        .CreditCustomersQueryable.Where(x => x.CustomerId == customerId && x.CreditNr == creditNr)
                        .ToList();
                    if (creditCustomers.Count == 0)
                        throw new NTechCoreWebserviceException("Customer not authorized")
                        {
                            ErrorHttpStatusCode = 401,
                            IsUserFacing = true
                        };
                }

                string failedMessage;
                AmortizationPlan p;
                if (envSettings.HasPerLoanDueDay)
                {
                    throw new NotImplementedException();
                }

                if (!AmortizationPlan.TryGetAmortizationPlan(
                    model, notificationProcessSettingsFactory.GetByCreditType(model.GetCreditType()),
                    out p, out failedMessage, context.CoreClock, clientConfiguration,
                    CreditDomainModel.GetInterestDividerOverrideByCode(envSettings.ClientInterestModel)))
                {
                    throw new NTechCoreWebserviceException(failedMessage)
                    {
                        ErrorHttpStatusCode = 400,
                        IsUserFacing = true
                    };
                }
                return (Plan: p, Model: model);
            }
        }

        public AmortizationPlanUiModel GetAmortizationPlan(string creditNr)
        {
            HistoricalCreditModel model;
            using (var context = creditContextFactory.CreateContext())
            {
                model = AmortizationPlan.GetHistoricalCreditModel(creditNr, context, envSettings.IsMortgageLoansEnabled);
            }
            string failedMessage;
            if (TryGetAmortizationPlanWithPaymentFreeMonths(model, out var p, out failedMessage))
            {
                return p;
            }
            else
            {
                throw new NTechCoreWebserviceException(failedMessage) { ErrorHttpStatusCode = 400, IsUserFacing = true };
            }
        }

        public AmortizationPlanUiModel AddFuturePaymentFreeMonth(string creditNr, DateTime? forMonth, bool? returningAmortizationPlan)
        {
            if (string.IsNullOrWhiteSpace(creditNr))
                throw new NTechCoreWebserviceException("Missing creditNr") { ErrorHttpStatusCode = 400, IsUserFacing = true };
            if (!forMonth.HasValue)
                throw new NTechCoreWebserviceException("Missing forMonth") { ErrorHttpStatusCode = 400, IsUserFacing = true };

            var bm = new FuturePaymentFreeMonthBusinessEventManager(currentUser, clock, clientConfiguration);
            using (var context = creditContextFactory.CreateContext())
            {
                if (bm.HasPendingFuturePaymentFreeMonth(context, creditNr, forMonth.Value))
                {
                    throw new NTechCoreWebserviceException("That month already has a pending paymentfree month") { ErrorHttpStatusCode = 400, IsUserFacing = true };
                }
                bm.AddFuturePaymentFreeMonth(context, creditNr, forMonth.Value);
                context.SaveChanges();
            }
            if (returningAmortizationPlan.GetValueOrDefault())
            {
                using (var context = creditContextFactory.CreateContext())
                {
                    var model = AmortizationPlan.GetHistoricalCreditModel(creditNr, context, envSettings.IsMortgageLoansEnabled);
                    string failedMessage;
                    AmortizationPlanUiModel p;
                    if (TryGetAmortizationPlanWithPaymentFreeMonths(model, out p, out failedMessage))
                    {
                        return p;
                    }
                    else
                    {
                        throw new NTechCoreWebserviceException(failedMessage) { ErrorHttpStatusCode = 400, IsUserFacing = true };
                    }
                }
            }
            else
            {
                return null;
            }
        }

        public AmortizationPlanUiModel CancelFuturePaymentFreeMonth(string creditNr, DateTime? forMonth, bool? returningAmortizationPlan)
        {
            if (string.IsNullOrWhiteSpace(creditNr))
                throw new NTechCoreWebserviceException("Missing creditNr") { ErrorHttpStatusCode = 400, IsUserFacing = true };
            if (!forMonth.HasValue)
                throw new NTechCoreWebserviceException("Missing forMonth") { ErrorHttpStatusCode = 400, IsUserFacing = true };

            var bm = new FuturePaymentFreeMonthBusinessEventManager(currentUser, clock, clientConfiguration);
            using (var context = creditContextFactory.CreateContext())
            {
                if (!bm.HasPendingFuturePaymentFreeMonth(context, creditNr, forMonth.Value))
                {
                    throw new NTechCoreWebserviceException("That has no pending paymentfree months") { ErrorHttpStatusCode = 400, IsUserFacing = true };
                }
                bm.CancelFuturePaymentFreeMonth(context, creditNr, forMonth.Value);
                context.SaveChanges();
            }
            if (returningAmortizationPlan.GetValueOrDefault())
            {
                using (var context = creditContextFactory.CreateContext())
                {
                    var model = AmortizationPlan.GetHistoricalCreditModel(creditNr, context, envSettings.IsMortgageLoansEnabled);
                    string failedMessage;
                    AmortizationPlanUiModel p;
                    if (TryGetAmortizationPlanWithPaymentFreeMonths(model, out p, out failedMessage))
                    {
                        return p;
                    }
                    else
                    {
                        throw new NTechCoreWebserviceException(failedMessage) { ErrorHttpStatusCode = 400, IsUserFacing = true };
                    }
                }
            }
            else
            {
                return null;
            }
        }

        public bool TryGetAmortizationPlanWithPaymentFreeMonths(HistoricalCreditModel model, out AmortizationPlanUiModel uiModel, out string failedMessage)
        {
            AmortizationPlan p;
            var processSettings = notificationProcessSettingsFactory.GetByCreditType(model.GetCreditType());
            if (AmortizationPlan.TryGetAmortizationPlan(model, processSettings, out p, out failedMessage, clock, clientConfiguration, 
                CreditDomainModel.GetInterestDividerOverrideByCode(envSettings.ClientInterestModel)))
            {
                var items = p.GetPossibleFuturePaymentFreeMonths(model.NrOfPaidNotifications, processSettings);
                uiModel = new AmortizationPlanUiModel
                {
                    SinglePaymentLoanRepaymentDays = model.SinglePaymentLoanRepaymentDays,
                    FirstNotificationCostsAmount = model.FirstNotificationCostsAmount,
                    Annuity = model.AmortizationModel.UsingActualAnnuityOrFixedMonthlyCapital(a => new decimal?(a), _ => new decimal?()),
                    MonthlyFixedCapitalAmount = model.AmortizationModel.UsingActualAnnuityOrFixedMonthlyCapital(_ => new decimal?(), b => new decimal?(b)),
                    NotificationFee = p.NotificationFee,
                    NrOfRemainingPayments = p.NrOfRemainingPayments,
                    Items = items.Select(x => new AmortizationPlanUiModel.AmortizationPlanUiItem
                    {
                        IsPaymentFreeMonthAllowed = x.IsPossible,
                        CapitalBefore = x.Item.CapitalBefore,
                        CapitalTransaction = x.Item.CapitalTransaction,
                        EventTransactionDate = x.Item.EventTransactionDate,
                        EventTypeCode = x.Item.EventTypeCode,
                        InterestTransaction = x.Item.InterestTransaction,
                        IsFutureItem = x.Item.IsFutureItem,
                        IsWriteOff = x.Item.IsWriteOff,
                        NotificationFeeTransaction = x.Item.NotificationFeeTransaction,
                        TotalTransaction = x.Item.TotalTransaction,
                        BusinessEventRoleCode = x.Item.BusinessEventRoleCode,
                        FutureItemDueDate = x.Item.FutureItemDueDate,
                        IsTerminationLetterProcessReActivation = x.Item.IsTerminationLetterProcessReActivation,
                        IsTerminationLetterProcessSuspension = x.Item.IsTerminationLetterProcessSuspension
                    }).ToList()
                };
                return true;
            }
            else
            {
                uiModel = null;
                return false;
            }
        }        
    }

    public class AmortizationPlanUiModel
    {
        public decimal? Annuity { get; set; }
        public decimal? MonthlyFixedCapitalAmount { get; set; }
        public decimal NotificationFee { get; set; }
        public int NrOfRemainingPayments { get; set; }
        public List<AmortizationPlanUiItem> Items { get; set; }
        public int? SinglePaymentLoanRepaymentDays { get; set; }
        public decimal FirstNotificationCostsAmount { get; set; }

        public class AmortizationPlanUiItem
        {
            public bool? IsPaymentFreeMonthAllowed { get; set; }
            public decimal CapitalBefore { get; set; }
            public decimal CapitalTransaction { get; set; }
            public DateTime EventTransactionDate { get; set; }
            public string EventTypeCode { get; set; }
            public decimal? InterestTransaction { get; set; }
            public bool IsFutureItem { get; set; }
            public bool IsWriteOff { get; set; }
            public decimal NotificationFeeTransaction { get; set; }
            public decimal TotalTransaction { get; set; }
            public string BusinessEventRoleCode { get; set; }
            public DateTime? FutureItemDueDate { get; set; }
            public bool IsTerminationLetterProcessReActivation { get; set; }
            public bool IsTerminationLetterProcessSuspension { get; set; }
        }
    }
}
