using nCredit;
using nCredit.DbModel.BusinessEvents;
using Newtonsoft.Json;
using NTech.Banking.MortgageLoans;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Models;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services
{
    public class MlStandardSeRevaluationService : BusinessEventManagerOrServiceBase
    {
        private readonly MortgageLoanCollateralService mortgageLoanCollateralService;
        private readonly CreditContextFactory creditContextFactory;

        public MlStandardSeRevaluationService(INTechCurrentUserMetadata currentUser, ICoreClock clock, IClientConfigurationCore clientConfiguration,
            MortgageLoanCollateralService mortgageLoanCollateralService, CreditContextFactory creditContextFactory) : base(currentUser, clock, clientConfiguration)
        {
            this.mortgageLoanCollateralService = mortgageLoanCollateralService;
            this.creditContextFactory = creditContextFactory;
        }

        public BusinessEventOnlyResponse CommitRevaluate(MlStandardSeRevaluationCommitRequest request)
        {
            var newBasis = request.NewBasis;
            using (var context = creditContextFactory.CreateContext())
            {
                var creditNrs = newBasis.Loans.Select(x => x.CreditNr).ToList();
                var creditHeaders = context.CreditHeadersQueryable.Where(x => creditNrs.Contains(x.CreditNr)).ToList();
                var collateralId = creditHeaders.Select(x => x.CollateralHeaderId.Value).Distinct().Single();

                var currentLoans = mortgageLoanCollateralService.GetCurrentLoansOnCollateral(context, new GetCurrentLoansOnCollateralRequest
                {
                    CollateralId = collateralId
                }).Loans;

                var evt = AddBusinessEvent(BusinessEventType.RevalueMortgageLoanSe, context);

                foreach (var currentLoan in currentLoans)
                {
                    var basisLoan = newBasis.Loans.Single(x => x.CreditNr == currentLoan.CreditNr);
                    if (!currentLoan.ActualFixedMonthlyPayment.HasValue)
                        throw new Exception("Missing ActualFixedMonthlyPayment");

                    if (currentLoan.ActualFixedMonthlyPayment.Value != basisLoan.MonthlyAmortizationAmount)
                    {
                        var credit = creditHeaders.Single(x => x.CreditNr == currentLoan.CreditNr);
                        AddDatedCreditValue(DatedCreditValueCode.MonthlyAmortizationAmount.ToString(), basisLoan.MonthlyAmortizationAmount, credit, evt, context);
                    }

                    AddComment($"New revaluation. Amortization updated: {Math.Round(basisLoan.MonthlyAmortizationAmount, 2):N} SEK.", BusinessEventType.RevalueMortgageLoanSe, context, creditNr: currentLoan.CreditNr, evt: evt);
                }

                var basisItemsToRemove = context
                    .CollateralHeadersQueryable
                    .Where(x => x.Id == collateralId)
                    .SelectMany(x => x.Items)
                    .Where(x => x.ItemName == "seMlAmortBasisKeyValueItemKey" && x.RemovedByBusinessEventId == null)
                    .ToList();
                basisItemsToRemove.ForEach(x => x.RemovedByEvent = evt);

                var basisKey = MortgageLoanCollateralService.AddSeAmortizationBasisToKeyValueStore(context, newBasis);

                context.AddCollateralItems(context.FillInfrastructureFields(new CollateralItem
                {
                    CollateralHeaderId = collateralId,
                    ItemName = "seMlAmortBasisKeyValueItemKey",
                    StringValue = basisKey,
                    CreatedByEvent = evt
                }));

                context.SaveChanges();

                return new BusinessEventOnlyResponse
                {
                    BusinessEventId = evt.Id
                };
            }
        }

        public MlStandardSeRevaluationCalculateModelResponse CalculateRevaluate(MlStandardSeRevaluationCalculateRequest request)
        {
            var newValuation = request.NewValuationAmount.HasValue && request.NewValuationDate.HasValue
                ? (
                    Amount: request.NewValuationAmount.Value,
                    Date: request.NewValuationDate.Value
                )
                : new (decimal Amount, DateTime Date)?();

            return new MlStandardSeRevaluationCalculateModelResponse
            {
                MlStandardSeRevaluationCalculateResult = CalculateRevaluate(request.CreditNr, request.CurrentCombinedYearlyIncomeAmount.Value, request.OtherMortageLoansAmount.Value, newValuation, keepExistingRuleCode: false, out bool isKeepExistingRuleCodeAllowed),
                IsKeepExistingRuleCodeAllowed = isKeepExistingRuleCodeAllowed,
                MlStandardSeRevaluationKeepExistingRuleCodeResult = CalculateRevaluate(request.CreditNr, request.CurrentCombinedYearlyIncomeAmount.Value, request.OtherMortageLoansAmount.Value, newValuation, keepExistingRuleCode: true, out bool _),
            };
        }

        public MlStandardSeRevaluationCalculateResponse CalculateRevaluate(string creditNr, decimal currentCombinedYearlyIncomeAmount, decimal otherMortageLoansAmount, (decimal Amount, DateTime Date)? newValuation, bool keepExistingRuleCode, out bool isKeepExistingRuleCodeAllowed)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var storedBasis = mortgageLoanCollateralService.GetSeMortageLoanAmortizationBasis(context, new GetSeAmortizationBasisRequest
                {
                    CreditNr = creditNr,
                    UseUpdatedBalance = false
                }).AmortizationBasis;

                var currentBalancesByCreditNr = mortgageLoanCollateralService.GetCurrentLoansOnCollateral(context, new GetCurrentLoansOnCollateralRequest
                {
                    CreditNr = creditNr
                })
                .Loans
                .ToDictionary(x => x.CreditNr);

                var objectValueAmount = newValuation?.Amount ?? storedBasis.ObjectValue;

                var newBasis = new SwedishMortgageLoanAmortizationBasisModel
                {
                    CurrentCombinedYearlyIncomeAmount = currentCombinedYearlyIncomeAmount,
                    OtherMortageLoansAmount = otherMortageLoansAmount,
                    ObjectValue = objectValueAmount,
                    ObjectValueDate = newValuation?.Date ?? storedBasis.ObjectValueDate
                };

                var amorteringskravRuleCode = MortageLoanAmortizationRuleCode.r201723;

                //Everything except MonthlyAmortizationAmount
                newBasis.Loans = storedBasis.Loans.Select(basisLoan =>
                {
                    var currentCapitalBalanceAmount = currentBalancesByCreditNr[basisLoan.CreditNr].CurrentCapitalBalanceAmount;
                    return new SwedishMortgageLoanAmortizationBasisModel.LoanModel
                    {
                        CreditNr = basisLoan.CreditNr,
                        CurrentCapitalBalanceAmount = currentCapitalBalanceAmount,
                        IsUsingAlternateRule = keepExistingRuleCode ? basisLoan.IsUsingAlternateRule : false,
                        MaxCapitalBalanceAmount = newValuation.HasValue
                            ? currentCapitalBalanceAmount
                            : Math.Max(
                                Math.Max(currentCapitalBalanceAmount, basisLoan.CurrentCapitalBalanceAmount),
                                basisLoan.MaxCapitalBalanceAmount ?? 0m),
                        RuleCode = keepExistingRuleCode ? basisLoan.RuleCode.ToString() : amorteringskravRuleCode.ToString(),
                        InterestBindMonthCount = context.DatedCreditValuesQueryable
                            .Where(x => x.CreditNr == basisLoan.CreditNr && x.Name == DatedCreditValueCode.MortgageLoanInterestRebindMonthCount.ToString())
                            .OrderByDescending(y => (decimal?)y.Id).Select(y => y.Value)
                            .FirstOrDefault()
                    };
                }).ToList();

                var currentPropertyLoanBalanceAmount = currentBalancesByCreditNr.Values.Sum(x => x.CurrentCapitalBalanceAmount);
                newBasis.LtvFraction = SwedishMortgageLoanAmortizationBasisService.ComputeLtv(objectValueAmount, currentPropertyLoanBalanceAmount);
                newBasis.LtiFraction = SwedishMortgageLoanAmortizationBasisService.ComputeLti(currentCombinedYearlyIncomeAmount,
                    currentPropertyLoanBalanceAmount, otherMortageLoansAmount);

                foreach (var loan in newBasis.Loans)
                {
                    var thisLoanAmortizationBasisLoanAmount = loan.CurrentCapitalBalanceAmount * newBasis.GetTotalAmortizationBasisLoanAmount() / newBasis.Loans.Sum(x => x.CurrentCapitalBalanceAmount);

                    var parsedRuleCode = Enum.TryParse(loan.RuleCode, out MortageLoanAmortizationRuleCode ruleCode);

                    var requiredMainRuleAmortizationFraction = SwedishMortgageLoanAmortizationBasisService.GetAmortizationPercentForLtv(newBasis.LtvFraction, ruleCode);
                    var requiredLtiBasedExtraAmortizationFraction = SwedishMortgageLoanAmortizationBasisService.GetLtiAmortizationPercentForLti(newBasis.LtiFraction, ruleCode);

                    loan.MonthlyAmortizationAmount = loan.IsUsingAlternateRule ? 
                        storedBasis.Loans.FirstOrDefault(x => x.CreditNr == loan.CreditNr).MonthlyAmortizationAmount 
                        : Math.Round(thisLoanAmortizationBasisLoanAmount * (requiredMainRuleAmortizationFraction + requiredLtiBasedExtraAmortizationFraction) / 12m);
                }

                isKeepExistingRuleCodeAllowed = GetIsKeepExistingRuleCodeAllowed(storedBasis);

                return new MlStandardSeRevaluationCalculateResponse
                {
                    NewBasis = newBasis,
                    NewAmorteringsunderlag = SwedishMortgageLoanAmortizationBasisService.GetSwedishAmorteringsunderlag(newBasis)
                };
            }
        }
        
        public bool GetIsKeepExistingRuleCodeAllowed(SwedishMortgageLoanAmortizationBasisModel storedBasis)
        {
            // Is only allowed in the scenarios: 
            // All active credit are on amortization freedom
            // All active credit are on amorteringskrav
            // Combination of active credits on amortization freedom + active credits on alternate rule

            var allActiveCreditsOnAmortizationFreedom = GetLoansWithRuleCodeCount(MortageLoanAmortizationRuleCode.none) == storedBasis.Loans.Count(); 

            var allActiveCreditsOnAmorteringskrav = GetLoansWithRuleCodeCount(MortageLoanAmortizationRuleCode.r201616) == storedBasis.Loans.Count();

            var creditsOnAmortizationFreedomCount = GetLoansWithRuleCodeCount(MortageLoanAmortizationRuleCode.none);
            var creditsOnAlternateRuleCount = storedBasis.Loans.Where(x => x.IsUsingAlternateRule == true).Count();

            var atleastOneActiveCreditOnAmortizationFreedom = creditsOnAmortizationFreedomCount >= 1;
            var atleastOneActiveCreditOnAlternateRule = creditsOnAlternateRuleCount >= 1;
            var onlyThisCombo = creditsOnAmortizationFreedomCount + creditsOnAlternateRuleCount == storedBasis.Loans.Count(); 

            return (allActiveCreditsOnAmortizationFreedom ||
                    allActiveCreditsOnAmorteringskrav ||
                    (atleastOneActiveCreditOnAmortizationFreedom && atleastOneActiveCreditOnAlternateRule && onlyThisCombo));

            int GetLoansWithRuleCodeCount(MortageLoanAmortizationRuleCode ruleCode)
            {
                return storedBasis.Loans.Where(x => x.RuleCode == ruleCode.ToString()).Count();
            }
        }

        public BusinessEventOnlyResponse SetAmortizationExceptions(MlStandardSeSetAmortizationExceptionsRequest request)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var currentByCreditNr = mortgageLoanCollateralService.GetCurrentLoansOnCollateral(context, new GetCurrentLoansOnCollateralRequest
                {
                    CreditNr = request.Credits.First().CreditNr
                }).Loans.ToDictionary(x => x.CreditNr);

                var evt = AddBusinessEvent(BusinessEventType.SetAmortizationExceptionsMortgageLoanSe, context);

                foreach (var requestCredit in request.Credits)
                {
                    var creditNr = requestCredit.CreditNr;
                    var currentCredit = currentByCreditNr.Req(creditNr);
                    var isRemove = currentCredit.AmortizationException != null && !requestCredit.HasException;
                    if (isRemove)
                    {
                        RemoveDatedCreditDate(context, creditNr, DatedCreditDateCode.AmortizationExceptionUntilDate, evt);
                        //NOTE: We would ideally like to delete DatedCreditStringCode.AmortizationExceptionReasons and DatedCreditValueCode.ExceptionAmortizationAmount also
                        //      but we dont support that in the system so for now this + logic around usage will have to do

                        AddComment($"Amortization exception removed.", BusinessEventType.SetAmortizationExceptionsMortgageLoanSe, context, creditNr: creditNr, evt: evt);
                    }
                    else if (requestCredit.HasException)
                    {
                        var currentException = currentCredit.AmortizationException;
                        var newException = requestCredit.Exception;

                        if (currentException == null || currentException.UntilDate != newException.UntilDate)
                        {
                            AddDatedCreditDate(DatedCreditDateCode.AmortizationExceptionUntilDate, newException.UntilDate.Value, evt, context, creditNr: creditNr);
                        }

                        if (currentException == null || currentException.AmortizationAmount != newException.AmortizationAmount)
                        {
                            AddDatedCreditValue(DatedCreditValueCode.ExceptionAmortizationAmount, newException.AmortizationAmount, evt, context, creditNr: creditNr);
                        }

                        string SerializeReasons(MortgageLoanSeAmortizationExceptionModel m) => JsonConvert.SerializeObject(m?.Reasons ?? new List<string>());
                        var currentReasonsRaw = SerializeReasons(currentException);
                        var newReasonsRaw = SerializeReasons(newException);
                        if (currentException == null || currentReasonsRaw != newReasonsRaw)
                        {
                            AddDatedCreditString(DatedCreditStringCode.AmortizationExceptionReasons.ToString(), newReasonsRaw, creditNr, evt, context);
                        }

                        AddComment($"New amortization exception: exception until {newException.UntilDate.Value:dd-MM-yyyy}, exception amount: {Math.Round(newException.AmortizationAmount, 2):N} SEK, exception reason: { String.Join(", ", newException.Reasons) }.",
                             BusinessEventType.SetAmortizationExceptionsMortgageLoanSe, context, creditNr: creditNr, evt: evt);
                    }
                }

                context.SaveChanges();

                return new BusinessEventOnlyResponse
                {
                    BusinessEventId = evt.Id
                };
            }
        }
    }

    public class MlStandardSeSetAmortizationExceptionsRequest
    {
        public List<MlStandardSeSetAmortizationExceptionModel> Credits { get; set; }
    }

    public class MlStandardSeSetAmortizationExceptionModel
    {
        public string CreditNr { get; set; }
        public bool HasException { get; set; }
        public MortgageLoanSeAmortizationExceptionModel Exception { get; set; }
    }

    public class MlStandardSeRevaluationCalculateRequest
    {
        public string CreditNr { get; set; }

        [Required]
        public decimal? CurrentCombinedYearlyIncomeAmount { get; set; }

        [Required]
        public decimal? OtherMortageLoansAmount { get; set; }

        public decimal? NewValuationAmount { get; set; }

        public DateTime? NewValuationDate { get; set; }
    }

    public class MlStandardSeRevaluationCalculateModelResponse
    {
        public MlStandardSeRevaluationCalculateResponse MlStandardSeRevaluationCalculateResult { get; set; }
        public MlStandardSeRevaluationCalculateResponse MlStandardSeRevaluationKeepExistingRuleCodeResult { get; set; }
        public bool IsKeepExistingRuleCodeAllowed { get; set; }
    }

    public class MlStandardSeRevaluationCalculateResponse
    {
        public SwedishMortgageLoanAmortizationBasisModel NewBasis { get; set; }
        public SwedishAmorteringsunderlag NewAmorteringsunderlag { get; set; }
    }

    public class MlStandardSeRevaluationCommitRequest
    {
        [Required]
        public SwedishMortgageLoanAmortizationBasisModel NewBasis { get; set; }
    }

    public class BusinessEventOnlyResponse
    {
        public int BusinessEventId { get; set; }
    }
}
