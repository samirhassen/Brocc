using nCredit.DomainModel;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class CreditPaymentPlacementModel : ICreditPaymentPlacementModel
    {
        private readonly string creditNr;
        private Dictionary<int, CreditNotificationDomainModel> openNotifications;
        private CreditDomainModel credit;
        private decimal? activeSettlementOfferAmount;
        private decimal? activeSettlementOfferSwedishRseEstimatedAmount;

        private readonly ICoreClock clock;

        private CreditPaymentPlacementModel(
            string creditNr,
            ICreditContextExtended context,
            CreditDomainModel credit,
            Dictionary<int, CreditNotificationDomainModel> openNotifications,
            decimal? remainingActivePaymentPlanAmount,
            decimal? activeSettlementOfferAmount, 
            decimal? activeSettlementOfferSwedishRseEstimatedAmount)
        {
            this.openNotifications = openNotifications;
            this.credit = credit;
            this.activeSettlementOfferAmount = activeSettlementOfferAmount;
            this.activeSettlementOfferSwedishRseEstimatedAmount = activeSettlementOfferSwedishRseEstimatedAmount;
            clock = context.CoreClock;
            this.creditNr = creditNr;

            RemainingActivePaymentPlanAmount = remainingActivePaymentPlanAmount;
        }

        public static CreditPaymentPlacementModel LoadSingle(string creditNr,ICreditContextExtended context,
            ICreditEnvSettings envSettings, IClientConfigurationCore clientConfiguration, PaymentOrderService paymentOrderService) => LoadBatch(new HashSet<string> { creditNr }, 
                context, envSettings, clientConfiguration, new PaymentPlacementBatchDataSource(envSettings, paymentOrderService))[creditNr];

        public static IDictionary<string, CreditPaymentPlacementModel> LoadBatch(
            ISet<string> creditNrs,
            ICreditContextExtended context,
            ICreditEnvSettings envSettings,
            IClientConfigurationCore clientConfiguration,
            PaymentPlacementBatchDataSource batchDataSource)
        {
            batchDataSource.EnsurePreloaded(creditNrs, context);

            /*
             Load settlement offers
             */
            decimal? ParseDecimal(string s) => s == null ? new decimal?() : decimal.Parse(s, NumberStyles.Number, CultureInfo.InvariantCulture);
            var activeSettlementOfferPerCredit = context
                .CreditSettlementOfferHeadersQueryable
                .Where(x => creditNrs.Contains(x.CreditNr) && !x.CancelledByEventId.HasValue && !x.CommitedByEventId.HasValue)
                .Select(x => new
                {
                    x.CreditNr,
                    SettlementAmount = x
                        .Items
                        .Where(y => y.Name == CreditSettlementOfferItem.CreditSettlementOfferItemCode.SettlementAmount.ToString())
                        .Select(y => y.Value)
                        .FirstOrDefault(),
                    SwedishRseEstimatedAmount = x
                        .Items
                        .Where(y => y.Name == CreditSettlementOfferItem.CreditSettlementOfferItemCode.SwedishRseEstimatedAmount.ToString())
                        .Select(y => y.Value)
                        .FirstOrDefault()
                })
                .ToList()
                .ToDictionary(x => x.CreditNr, x => new
                {
                    SettlementOfferAmount = ParseDecimal(x?.SettlementAmount),
                    SwedishRseEstimatedAmount = ParseDecimal(x?.SwedishRseEstimatedAmount)
                });

            /*
             Load alternate payment plans
             */
            Dictionary<string, decimal?> remainingActivePaymentPlanPerCreditNr = null;
            if (AlternatePaymentPlanService.IsPaymentPlanEnabledShared(clientConfiguration))
            {
                remainingActivePaymentPlanPerCreditNr = new Dictionary<string, decimal?>(creditNrs.Count);
                var activePlans = AlternatePaymentPlanService
                    .GetActivePaymentPlansCompleteOrCancelDataQueryable(context, onlyTheseCreditNrs: creditNrs.ToList())
                    .Select(x => new
                    {
                        x.PlanHeader.CreditNr,
                        x.TotalPaidNotifiedAmount,
                        x.ExtraAmortizationAmount,
                        TotalAmount = x.PlanMonths.OrderByDescending(y => y.DueDate).Select(y => y.TotalAmount).FirstOrDefault()
                    }).ToList();
                foreach (var activePaymentPlan in activePlans)
                    remainingActivePaymentPlanPerCreditNr[activePaymentPlan.CreditNr] = Math.Max(
                        activePaymentPlan.TotalAmount - activePaymentPlan.TotalPaidNotifiedAmount - activePaymentPlan.ExtraAmortizationAmount, 
                        0m);
            }

            var result = new Dictionary<string, CreditPaymentPlacementModel>(creditNrs.Count);
            foreach (var creditNr in creditNrs)
            {
                var activeSettlementOffer = activeSettlementOfferPerCredit.Opt(creditNr);
                result[creditNr] = new CreditPaymentPlacementModel(creditNr, context,
                    batchDataSource.GetCreditDomainModel(creditNr, context), 
                    batchDataSource.GetOpenNotificationDomainModels(creditNr, context),
                    remainingActivePaymentPlanPerCreditNr?.OptSDefaultValue(creditNr),
                    activeSettlementOffer?.SettlementOfferAmount, activeSettlementOffer?.SwedishRseEstimatedAmount);
            }

            return result;
        }

        public decimal ComputeInterestAmountIgnoringInterestFromDate(DateTime transactionDate, Tuple<DateTime, DateTime> dateInterval)
        {
            return credit.ComputeInterestAmountIgnoringInterestFromDate(transactionDate, dateInterval);
        }

        public decimal ComputeNotNotifiedInterestUntil(DateTime untilDate, out int nrOfInterestDays)
        {
            return credit.ComputeNotNotifiedInterestUntil(clock.Today, untilDate, out nrOfInterestDays);
        }

        public decimal GetBalance()
        {
            return credit.GetBalance(clock.Today);
        }

        public List<INotificationPaymentPlacementModel> GetOpenNotifications()
        {
            return openNotifications
                .Where(x => x.Value.GetRemainingBalance(clock.Today) > 0m)
                .Select(x => (INotificationPaymentPlacementModel)new NotificationPaymentPlacementModel(openNotifications, x.Key, clock.Today,
                    observePaymentPlacement: (t, amt, id) => Place(t, amt, id)))
                .ToList();
        }

        public decimal GetNotNotifiedCapitalBalance()
        {
            return credit.GetNotNotifiedCapitalBalance(clock.Today);
        }

        public CreditStatus GetCreditStatus()
        {
            return credit.GetStatus(clock.Today);
        }

        public TResult UsingActualAnnuityOrFixedMonthlyCapital<TResult>(Func<decimal, TResult> onAnnuity, Func<decimal, TResult> onFixedMonthlyPayment)
        {
            return credit.GetAmortizationModel(clock.Today).UsingActualAnnuityOrFixedMonthlyCapital(onAnnuity, onFixedMonthlyPayment);
        }

        public void PlaceExtraAmortizationPayment(decimal amount)
        {
            Place(new PaymentOrderItem { IsBuiltin = true, Code = CreditDomainModel.AmountType.Capital.ToString() }, amount, null);
        }

        public string CreditNr
        {
            get
            {
                return this.creditNr;
            }
        }

        public decimal? ActiveSettlementOfferAmount
        {
            get
            {
                return this.activeSettlementOfferAmount;
            }
        }

        public decimal? GetSettlementOfferSwedishRseEstimatedAmount() => activeSettlementOfferSwedishRseEstimatedAmount;

        public CreditType CreditType => credit.GetCreditType();
        public bool IsMainCredit => credit.GetDatedCreditString(clock.Today, DatedCreditStringCode.MainCreditCreditNr, null, allowMissing: true) == null;

        public void Place(PaymentOrderItem item, decimal amount, int? notificationId)
        {
            Placements.Add(new Placement
            {
                Amount = amount,
                AmountType = item,
                CreditNr = this.creditNr,
                NotificationId = notificationId
            });
        }
        public List<Placement> Placements { get; set; } = new List<Placement>();
        public decimal? RemainingActivePaymentPlanAmount { get; }

        //TODO: Is this serialized/connected to the ui?
        public class Placement
        {
            public PaymentOrderItem AmountType { get; set; }
            public decimal Amount { get; set; }
            public int? NotificationId { get; set; }
            public string CreditNr { get; set; }
        }
    }
}