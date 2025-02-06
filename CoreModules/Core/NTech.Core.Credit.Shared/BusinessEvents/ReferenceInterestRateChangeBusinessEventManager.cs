using nCredit.Code.Services;
using nCredit.DbModel.Repository;
using nCredit.DomainModel;
using NTech.Banking.LoanModel;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class ReferenceInterestRateChangeBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        private readonly LegalInterestCeilingService legalInterestCeilingService;
        private readonly ICreditEnvSettings envSettings;

        public ReferenceInterestRateChangeBusinessEventManager(INTechCurrentUserMetadata currentUser, LegalInterestCeilingService legalInterestCeilingService,
            ICreditEnvSettings envSettings, ICoreClock clock,
            IClientConfigurationCore clientConfiguration) : base(currentUser, clock, clientConfiguration)
        {
            this.legalInterestCeilingService = legalInterestCeilingService;
            this.envSettings = envSettings;
        }

        public bool TryChangeReferenceInterest(ICreditContextExtended context, decimal newInterestRatePercent, out BusinessEvent evt, out string failMessage, Tuple<int, DateTime> initiatedByAndDate = null)
        {
            var now = Now;
            var bookKeepingDate = now.ToLocalTime().Date;

            evt = FillInInfrastructureFields(new BusinessEvent
            {
                EventDate = now,
                EventType = BusinessEventType.ReferenceInterestRateChange.ToString(),
                BookKeepingDate = bookKeepingDate,
                TransactionDate = now.ToLocalTime().Date
            });
            context.AddBusinessEvent(evt);

            var changeHeader = FillInInfrastructureFields(new ReferenceInterestChangeHeader
            {
                CreatedByEvent = evt,
                InitiatedByUserId = initiatedByAndDate?.Item1 ?? UserId,
                InitiatedDate = initiatedByAndDate?.Item2 ?? now.ToLocalTime().DateTime,
                NewInterestRatePercent = newInterestRatePercent,
                TransactionDate = evt.TransactionDate
            });
            context.AddReferenceInterestChangeHeaders(changeHeader);

            context.AddSharedDatedValues(FillInInfrastructureFields(new SharedDatedValue
            {
                BusinessEvent = evt,
                Name = SharedDatedValueCode.ReferenceInterestRate.ToString(),
                Value = newInterestRatePercent,
                TransactionDate = evt.TransactionDate
            }));

            if (!(envSettings.IsMortgageLoansEnabled && envSettings.MortgageLoanInterestBindingMonths.HasValue))
            {
                if (!TryUpdateReferenceInterestRates(context, newInterestRatePercent, evt, out failMessage))
                    return false;
            }

            failMessage = null;

            return true;
        }

        private IQueryable<CreditHeader> GetUpdateEligableCredits(ICreditContextExtended context, bool usesBoundInterest)
        {
            var credits = context.CreditHeadersQueryable.Where(x => x.Status == CreditStatus.Normal.ToString());
            if (usesBoundInterest)
            {
                var code = DatedCreditDateCode.MortgageLoanNextInterestRebindDate.ToString();
                var today = Clock.Today;
                var tmp = credits.Select(x => new
                {
                    C = x,
                    NextInterestRebindDate = x
                        .DatedCreditDates
                        .Where(y => y.Name == code && !y.RemovedByBusinessEventId.HasValue)
                        .OrderByDescending(y => y.TransactionDate)
                        .ThenByDescending(y => y.BusinessEventId)
                        .Select(y => y.Value)
                        .FirstOrDefault()
                });
                credits = tmp.Where(x => x.NextInterestRebindDate <= today).Select(x => x.C);
            }
            return credits;
        }

        private bool TryUpdateReferenceInterestRates(ICreditContextExtended context, decimal newInterestRatePercent, BusinessEvent evt, out string failMessage)
        {
            var isMortgageLoan = envSettings.IsMortgageLoansEnabled;

            var activeCredits = GetUpdateEligableCredits(context, isMortgageLoan);

            var maxCurrentMarginalInterestRate = activeCredits
                .Select(x => new
                {
                    LatestMarginalInterestRate = x.DatedCreditValues.Where(y => y.Name == DatedCreditValueCode.MarginInterestRate.ToString()).OrderByDescending(y => y.TransactionDate).ThenByDescending(y => y.Timestamp).Select(y => (decimal?)y.Value).FirstOrDefault()
                })
                .Where(x => x.LatestMarginalInterestRate.HasValue)
                .OrderByDescending(x => x.LatestMarginalInterestRate)
                .Take(1)
                .ToList()
                .Max(x => (decimal?)x.LatestMarginalInterestRate.Value);

            /*
            if (maxCurrentMarginalInterestRate.HasValue && (maxCurrentMarginalInterestRate + newInterestRatePercent) <= 0)
            {
                failMessage = $"Cannot change to this value since there is a credit with marginal interest rate {(maxCurrentMarginalInterestRate.Value / 100m).ToString("P")} and this change would make that credit have non-positive total interest rate";
                return false;
            }
            */

            var creditDataRepo = new PartialCreditModelRepository();

            var creditNrGroups = activeCredits.Select(x => x.CreditNr).ToArray().SplitIntoGroupsOfN(200);

            foreach (var activeCreditNrs in creditNrGroups)
            {
                var creditModels = CreditDomainModel.PreFetchForCredits(context, activeCreditNrs.ToArray(), envSettings);

                foreach (var creditNr in activeCreditNrs)
                {
                    var creditData = creditModels[creditNr];

                    context.AddDatedCreditValues(FillInInfrastructureFields(new DatedCreditValue
                    {
                        CreditNr = creditNr,
                        BusinessEvent = evt,
                        Name = DatedCreditValueCode.ReferenceInterestRate.ToString(),
                        Value = newInterestRatePercent,
                        TransactionDate = evt.TransactionDate
                    }));

                    var currentRequestedMarginInterestRate = creditData.GetDatedCreditValueOpt(context.CoreClock.Today, DatedCreditValueCode.RequestedMarginInterestRate);
                    var currentMarginInterestRate = creditData.GetDatedCreditValueOpt(context.CoreClock.Today, DatedCreditValueCode.MarginInterestRate).Value;

                    var changes = this.legalInterestCeilingService.HandleReferenceInterestRateChange(newInterestRatePercent, currentRequestedMarginInterestRate, currentMarginInterestRate);
                    var comment = $"Reference interest rate changed to {(newInterestRatePercent / 100m).ToString("P")}";
                    if (changes.NewMarginInterestRate.HasValue)
                    {
                        comment += $". Margin interest rate changed to {(changes.NewMarginInterestRate.Value / 100m).ToString("P")}";
                        context.AddDatedCreditValues(FillInInfrastructureFields(new DatedCreditValue
                        {
                            CreditNr = creditNr,
                            BusinessEvent = evt,
                            Name = DatedCreditValueCode.MarginInterestRate.ToString(),
                            Value = changes.NewMarginInterestRate.Value,
                            TransactionDate = evt.TransactionDate
                        }));
                    }
                    if (changes.NewRequestedMarginInterestRate.HasValue)
                    {
                        comment += $". Requested margin interest rate changed to {(changes.NewRequestedMarginInterestRate.Value / 100m).ToString("P")}";
                        context.AddDatedCreditValues(FillInInfrastructureFields(new DatedCreditValue
                        {
                            CreditNr = creditNr,
                            BusinessEvent = evt,
                            Name = DatedCreditValueCode.RequestedMarginInterestRate.ToString(),
                            Value = changes.NewRequestedMarginInterestRate.Value,
                            TransactionDate = evt.TransactionDate
                        }));
                    }
                    if (isMortgageLoan && envSettings.MortgageLoanInterestBindingMonths.HasValue)
                    {
                        var nextInterestRebindDate = Clock.Today.AddMonths(envSettings.MortgageLoanInterestBindingMonths.Value);
                        comment += $". Interest bound until {nextInterestRebindDate.ToString("yyyy-MM-dd")}";
                        context.AddDatedCreditDates(FillInInfrastructureFields(new DatedCreditDate
                        {
                            CreditNr = creditNr,
                            BusinessEvent = evt,
                            Name = DatedCreditDateCode.MortgageLoanNextInterestRebindDate.ToString(),
                            Value = nextInterestRebindDate,
                            TransactionDate = evt.TransactionDate
                        }));
                    }

                    var notNotifiedCapitalBalance = creditData.GetNotNotifiedCapitalBalance(context.CoreClock.Today);
                    if (envSettings.ShouldRecalculateAnnuityOnInterestChange && creditData.GetAmortizationModel(context.CoreClock.Today).UsesAnnuities && notNotifiedCapitalBalance > 0m)
                    {
                        var currentReferenceInterestRate = creditData.GetDatedCreditValue(context.CoreClock.Today, DatedCreditValueCode.ReferenceInterestRate, defaultValue: 0m);

                        var currentAnnuityAmount = creditData.GetDatedCreditValue(context.CoreClock.Today, DatedCreditValueCode.AnnuityAmount);
                        var currentRepaymentTime = CalculateCurrentRepaymentTimeWithFallback(
                            creditData.GetDatedCreditValue(context.CoreClock.Today, DatedCreditValueCode.ReferenceInterestRate)
                            +
                            creditData.GetDatedCreditValue(context.CoreClock.Today, DatedCreditValueCode.MarginInterestRate),
                            notNotifiedCapitalBalance,
                            currentAnnuityAmount);

                        var newTotalInterestRate = (changes.NewMarginInterestRate ?? currentMarginInterestRate) + newInterestRatePercent;
                        var newAnnuityAmount = PaymentPlanCalculation
                            .BeginCreateWithRepaymentTime(notNotifiedCapitalBalance, currentRepaymentTime, newTotalInterestRate, true, null, envSettings.CreditsUse360DayInterestYear)
                            .EndCreate()
                            .AnnuityAmount;

                        if (newAnnuityAmount != currentAnnuityAmount)
                        {
                            comment += $". Changed annuity from {currentAnnuityAmount.ToString("C", this.CommentFormattingCulture)} to {newAnnuityAmount.ToString("C", this.CommentFormattingCulture)} to maintain repayment time.";
                            context.AddDatedCreditValues(FillInInfrastructureFields(new DatedCreditValue
                            {
                                CreditNr = creditNr,
                                BusinessEvent = evt,
                                Name = DatedCreditValueCode.AnnuityAmount.ToString(),
                                Value = newAnnuityAmount,
                                TransactionDate = evt.TransactionDate
                            }));
                        }
                    }

                    AddComment(
                        comment,
                        BusinessEventType.ReferenceInterestRateChange,
                        context,
                        creditNr: creditNr);
                }
            }

            failMessage = null;

            return true;
        }

        private int CalculateCurrentRepaymentTimeWithFallback(decimal interestRatePercent, decimal loanAmount, decimal annuityAmount)
        {
            int? GetFallbackMonthCount(bool isRequired)
            {
                var rawSetting = ClientCfg.OptionalSetting("ntech.credit.annuity.maxrepaymenttimeinmonths");
                if (rawSetting == null)
                {
                    if (isRequired)
                    {
                        throw new Exception("Missing client config setting ntech.credit.annuity.maxrepaymenttimeinmonths");
                    }
                    else
                    {
                        return null;
                    }
                }
                return int.Parse(rawSetting);
            };

            try
            {
                var monthCount = PaymentPlanCalculation
                    .BeginCreateWithAnnuity(loanAmount, annuityAmount, interestRatePercent, null, envSettings.CreditsUse360DayInterestYear)
                    .EndCreate()
                    .Payments
                    .Count;

                var fallbackMonthCount = GetFallbackMonthCount(false);

                return fallbackMonthCount.HasValue
                    ? Math.Min(fallbackMonthCount.Value, monthCount)
                    : monthCount;
            }
            catch
            {
                return GetFallbackMonthCount(true).Value;
            }
        }

        public bool TryUpdateReferenceInterestWhereBindingHasExpired(ICreditContextExtended context, out BusinessEvent evt, out string failMessage)
        {
            if (!envSettings.IsMortgageLoansEnabled)
                throw new Exception("Only allowed for mortgage loans");

            var m = new SharedDatedValueDomainModel(context);
            var currentReferenceInterestRatePercent = m.GetReferenceInterestRatePercent(Clock.Today);

            var now = Now;
            var bookKeepingDate = now.ToLocalTime().Date;

            evt = FillInInfrastructureFields(new BusinessEvent
            {
                EventDate = now,
                EventType = BusinessEventType.MortgageLoanReferenceInterestUpdate.ToString(),
                BookKeepingDate = bookKeepingDate,
                TransactionDate = now.ToLocalTime().Date
            });
            context.AddBusinessEvent(evt);

            if (!TryUpdateReferenceInterestRates(context, currentReferenceInterestRatePercent, evt, out failMessage))
                return false;

            failMessage = null;

            return true;
        }
    }
}