using Newtonsoft.Json;
using NTech.Banking.MortgageLoans;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Models;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Globalization;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services
{
    public class SwedishMortgageLoanAmortizationBasisService
    {
        public static decimal? ComputeLtv(decimal objectValueAmount, decimal currentPropertyLoanBalanceAmount) =>
            objectValueAmount <= 0 ? new decimal?() : Math.Round(currentPropertyLoanBalanceAmount / objectValueAmount, 2);
        
        public static decimal? ComputeLti(decimal incomeAmount, decimal currentPropertyLoanBalanceAmount, decimal otherPropertiesLoanBalanceAmount)
        {
            return incomeAmount <= 0
                ? new decimal?()
                : Math.Round((otherPropertiesLoanBalanceAmount + currentPropertyLoanBalanceAmount) / incomeAmount, 2);
        }

        public static decimal? ComputeIncomeFromLti(decimal ltiFraction, decimal currentPropertyLoanBalanceAmount, decimal otherPropertiesLoanBalanceAmount)
        {
            return ltiFraction <= 0 ? new decimal?() : Math.Ceiling((currentPropertyLoanBalanceAmount + otherPropertiesLoanBalanceAmount) / ltiFraction);
        }

        public static decimal GetAmortizationPercentForLtv(decimal? ltvFraction, MortageLoanAmortizationRuleCode ruleCode)
        {
            if (ruleCode == MortageLoanAmortizationRuleCode.r201723 || ruleCode == MortageLoanAmortizationRuleCode.r201616)
            {
                return !ltvFraction.HasValue
                    ? 0.02m
                    : (ltvFraction < 0.5m ? 0m
                        : ltvFraction < 0.7m ? 0.01m : 0.02m);
            }
            else
            {
                return 0m;
            }
        }

        public static decimal GetLtiAmortizationPercentForLti(decimal? ltiFraction, MortageLoanAmortizationRuleCode ruleCode)
        {
            if (ruleCode == MortageLoanAmortizationRuleCode.r201723)
            {
                return
                    !ltiFraction.HasValue
                    ? 0.01m
                    : ltiFraction < 4.5m ? 0m : 0.01m;
            }
            else
            {
                return 0m;
            }
        }

        //When the loan already has a previous basis here
        public static SwedishMortgageLoanAmortizationBasisModel CalculateSuggestedAmortizationBasisForExistingLoan(
            SwedishMortgageLoanAmortizationBasisModel currentBasis,
            DateTime forDate,
            List<CalculateMortgageLoanAmortizationBasisRequest.MlAmortizationBasisRequestNewLoan> newLoans = null,
            decimal? newCombinedYearlyIncomeAmount = null,
            Dictionary<string, decimal> currentLoanBalanceAmount = null,
            decimal? newOtherMortageLoansBalanceAmount = null,
            decimal? newObjectValueAmount = null,
            MortageLoanAmortizationRuleCode? overrideLegalFramework = null,
            bool ignoreAlternateRule = false,
            bool forceAlternateRuleEvenIfWorse = false)
        {
            return CalculateSuggestedAmortizationBasis(new CalculateMortgageLoanAmortizationBasisRequest
            {
                CombinedYearlyIncomeAmount = newCombinedYearlyIncomeAmount ?? currentBasis.CurrentCombinedYearlyIncomeAmount,
                ObjectValueAmount = newObjectValueAmount ?? currentBasis.ObjectValue,
                OtherMortageLoansBalanceAmount = newOtherMortageLoansBalanceAmount ?? currentBasis.OtherMortageLoansAmount,
                ForceAlternateRuleEvenIfWorse = forceAlternateRuleEvenIfWorse,
                IgnoreAlternateRule = ignoreAlternateRule,
                ExistingLoans = currentBasis.Loans.Select(x => new CalculateMortgageLoanAmortizationBasisRequest.MlAmortizationBasisRequestExistingLoan
                {
                    CreditNr = x.CreditNr,
                    CurrentBalanceAmount = currentLoanBalanceAmount?.OptS(x.CreditNr) ?? x.CurrentCapitalBalanceAmount,
                    IsUsingAlternateRule = x.IsUsingAlternateRule,
                    RuleCode = x.RuleCode,
                    MaxBalanceAmount = newObjectValueAmount.HasValue ? null : x.MaxCapitalBalanceAmount,
                    MonthlyAmortizationAmount = x.MonthlyAmortizationAmount
                }).ToList(),
                NewLoans = newLoans
            },
            forDate,
            overrideLegalFramework: overrideLegalFramework);
        }

        public static SwedishMortgageLoanAmortizationBasisModel CalculateSuggestedAmortizationBasis(CalculateMortgageLoanAmortizationBasisRequest request, DateTime today,
            MortageLoanAmortizationRuleCode? overrideLegalFramework = null)
        {
            request.NewLoans = request.NewLoans ?? new List<CalculateMortgageLoanAmortizationBasisRequest.MlAmortizationBasisRequestNewLoan>();
            request.ExistingLoans = (request.ExistingLoans ?? new List<CalculateMortgageLoanAmortizationBasisRequest.MlAmortizationBasisRequestExistingLoan>()).Where(x => x.CurrentBalanceAmount > 0m).ToList();

            var currentLoansBalance = request.NewLoans.Sum(x => x.CurrentBalanceAmount) + request.ExistingLoans.Sum(x => x.CurrentBalanceAmount);
            var amortizationBasisLoanAmount = request.NewLoans.Sum(x => x.CurrentBalanceAmount) + (request.ExistingLoans.Sum(x => x.MaxBalanceAmount ?? x.CurrentBalanceAmount));
            var otherMortageLoansBalanceAmount = request.OtherMortageLoansBalanceAmount ?? 0m;

            var ltiFraction = ComputeLti(request.CombinedYearlyIncomeAmount, currentLoansBalance, otherMortageLoansBalanceAmount);
            var ltvFraction = ComputeLtv(request.ObjectValueAmount, currentLoansBalance);

            MortageLoanAmortizationRuleCode currentLegalFramework = overrideLegalFramework ?? MortageLoanAmortizationRuleCode.r201723;

            decimal requiredMainRuleAmortizationFraction = GetAmortizationPercentForLtv(ltvFraction, currentLegalFramework);
            decimal requiredLtiBasedExtraAmortizationFraction = GetLtiAmortizationPercentForLti(ltiFraction, currentLegalFramework);

            var allLoans = request.ExistingLoans.Select(x => new
            {
                x.CreditNr,
                Balance = x.CurrentBalanceAmount,
                MaxBalance = x.MaxBalanceAmount.HasValue ? Math.Max(x.CurrentBalanceAmount, x.MaxBalanceAmount.Value) : x.CurrentBalanceAmount
            }).Concat(request.NewLoans.Select(x => new { x.CreditNr, Balance = x.CurrentBalanceAmount, MaxBalance = x.CurrentBalanceAmount }));

            var mainRuleBasis = new SwedishMortgageLoanAmortizationBasisModel
            {
                ObjectValueDate = request.ObjectValueDate ?? today,
                ObjectValue = request.ObjectValueAmount,
                CurrentCombinedYearlyIncomeAmount = request.CombinedYearlyIncomeAmount,
                OtherMortageLoansAmount = otherMortageLoansBalanceAmount,
                LtiFraction = ltiFraction,
                LtvFraction = ltvFraction,
                Loans = allLoans.Select(loan =>
                {
                    var thisLoanAmortizationBasisLoanAmount = loan.Balance * amortizationBasisLoanAmount / currentLoansBalance;
                    var mainRuleRequiredAmortizationAmount = Math.Round(thisLoanAmortizationBasisLoanAmount * requiredMainRuleAmortizationFraction / 12m);
                    var actualRequiredAmortizationAmount = Math.Round(thisLoanAmortizationBasisLoanAmount * (requiredMainRuleAmortizationFraction + requiredLtiBasedExtraAmortizationFraction) / 12m);

                    return new SwedishMortgageLoanAmortizationBasisModel.LoanModel
                    {
                        CreditNr = loan.CreditNr,
                        CurrentCapitalBalanceAmount = loan.Balance,
                        MaxCapitalBalanceAmount = loan.MaxBalance,
                        IsUsingAlternateRule = false,
                        RuleCode = currentLegalFramework.ToString(),
                        MonthlyAmortizationAmount = actualRequiredAmortizationAmount
                    };
                }).ToList()
            };

            var ignoreAlternateRule = request.IgnoreAlternateRule.GetValueOrDefault() || request.IsNewObjectValueAmount == true;
            if (!ignoreAlternateRule && request.ExistingLoans.Any() && request.NewLoans.Any())
            {
                //Try keeping all existing loans as is and putting the loans on the alternate rule and see if this is a lower monthly cost payment
                var alternateRuleBasis = new SwedishMortgageLoanAmortizationBasisModel
                {
                    ObjectValueDate = request.ObjectValueDate ?? today,
                    ObjectValue = request.ObjectValueAmount,
                    CurrentCombinedYearlyIncomeAmount = request.CombinedYearlyIncomeAmount,
                    OtherMortageLoansAmount = otherMortageLoansBalanceAmount,
                    LtiFraction = ltiFraction,
                    LtvFraction = ltvFraction,
                    Loans = new List<SwedishMortgageLoanAmortizationBasisModel.LoanModel>()
                };
                foreach (var loan in request.ExistingLoans.Where(x => x.CurrentBalanceAmount > 0m))
                {
                    alternateRuleBasis.Loans.Add(new SwedishMortgageLoanAmortizationBasisModel.LoanModel
                    {
                        CreditNr = loan.CreditNr,
                        CurrentCapitalBalanceAmount = loan.CurrentBalanceAmount,
                        MaxCapitalBalanceAmount = loan.MaxBalanceAmount,
                        IsUsingAlternateRule = loan.IsUsingAlternateRule,
                        RuleCode = loan.RuleCode,
                        MonthlyAmortizationAmount = loan.MonthlyAmortizationAmount,
                    });
                }
                foreach (var loan in request.NewLoans)
                {
                    var monthlyAmount = Math.Round(loan.CurrentBalanceAmount / 10m / 12m); //Pay over 10 years
                    alternateRuleBasis.Loans.Add(new SwedishMortgageLoanAmortizationBasisModel.LoanModel
                    {
                        CreditNr = loan.CreditNr,
                        CurrentCapitalBalanceAmount = loan.CurrentBalanceAmount,
                        MaxCapitalBalanceAmount = loan.CurrentBalanceAmount,
                        IsUsingAlternateRule = true,
                        RuleCode = currentLegalFramework.ToString(),
                        MonthlyAmortizationAmount = monthlyAmount,
                    });
                }

                if (request.ForceAlternateRuleEvenIfWorse.GetValueOrDefault() || alternateRuleBasis.Loans.Sum(x => x.MonthlyAmortizationAmount) < mainRuleBasis.Loans.Sum(x => x.MonthlyAmortizationAmount))
                {
                    return alternateRuleBasis;
                }
            }

            return mainRuleBasis;
        }

        public static SwedishAmorteringsunderlag GetSwedishAmorteringsunderlag(
            SwedishMortgageLoanAmortizationBasisModel basis,
            Dictionary<string, decimal> currentLoanBalanceAmount = null)
        {
            decimal GetBalance(SwedishMortgageLoanAmortizationBasisModel.LoanModel loan)
            {
                return currentLoanBalanceAmount.OptS(loan.CreditNr) ?? loan.CurrentCapitalBalanceAmount;
            }

            /*
             Apparently this completely ignores everything about the loans actual rules and just assumes everything follows the latest legal framework.
             Also ignores any alternate rules.
             */
            var huvudFraction = basis.LtvFraction < 0.5m ? 0m : basis.LtvFraction < 0.7m ? 0.01m : 0.02m;
            var balans = basis.Loans?.Where(x => x.CurrentCapitalBalanceAmount > 0m).Sum(x => x.MaxCapitalBalanceAmount ?? x.CurrentCapitalBalanceAmount) ?? 0m;
            var totalAmorteringKravObjektetBankenHuvud = Math.Round(balans * huvudFraction / 12m);

            var result = new SwedishAmorteringsunderlag
            {
                AmorteringsgrundandeVarde = basis.ObjectValue,
                DatumAmorteringsgrundandeVarde = basis.ObjectValueDate,
                AmorteringsgrundandeSkuld = basis.GetTotalAmortizationBasisLoanAmount(),
                SkuldEjOmfattasAmorteringskrav = basis.Loans
                    .Where(x => x.RuleCode == MortageLoanAmortizationRuleCode.none.ToString()).Sum(GetBalance),
                SkuldOmfattasAmorteringskrav = basis.Loans
                    .Where(x => x.RuleCode == MortageLoanAmortizationRuleCode.r201616.ToString()).Sum(GetBalance),
                SkuldOmfattasSkarptAmorteringskrav = basis.Loans
                    .Where(x => !x.RuleCode.IsOneOfIgnoreCase(MortageLoanAmortizationRuleCode.none.ToString(), MortageLoanAmortizationRuleCode.r201616.ToString())).Sum(GetBalance),
                VaravOvanAlternativRegeln = basis.Loans
                    .Where(x => x.IsUsingAlternateRule == true).Sum(GetBalance),
                TotalAmorteringObjektet = basis.Loans
                    .Sum(x => x.MonthlyAmortizationAmount),
                TotalAmorteringKravObjektetBankenHuvud = totalAmorteringKravObjektetBankenHuvud,
                TotalAmorteringKravObjektetBankenAlternativ = basis.Loans
                    .Where(x => x.IsUsingAlternateRule).Sum(x => x.MonthlyAmortizationAmount)
            };
            return result;
        }

        //TODO: Unstatic this
        public static IDictionary<string, object> GetSwedishAmorteringsunderlagPdfData(string creditNr, ICoreClock clock,
            CreditContextFactory creditContextFactory, ICustomerClient customerClient, MortgageLoanCollateralService mortgageLoanCollateralService)
        {
            var c = CultureInfo.GetCultureInfo("sv-SE");

            dynamic e = new ExpandoObject();
            var ee = e as IDictionary<string, object>;

            ee["printDate"] = clock.Today.ToString("d", c);

            using (var context = creditContextFactory.CreateContext())
            {
                var h = context
                    .CreditHeadersQueryable
                    .Select(x => new
                    {
                        x.CreditNr,
                        Customers = x.CreditCustomers.Select(y => new { y.ApplicantNr, y.CustomerId }),
                    })
                    .Single(x => x.CreditNr == creditNr);

                ee["customers"] = h.Customers.Select(cu =>
                {
                    var contactInfo = customerClient.FetchCustomerContactInfo(cu.CustomerId, true, false);
                    var fullName = $"{contactInfo.firstName} {contactInfo.lastName}".Trim();
                    ee[$"contact{cu.ApplicantNr}"] = new
                    {
                        fullName = fullName,
                        streetAddress = contactInfo.addressStreet,
                        areaAndZipcode = $"{contactInfo.addressZipcode} {contactInfo.addressCity}"
                    };
                    return new { fullName = $"{contactInfo.firstName} {contactInfo.lastName}".Trim() };
                }).ToArray();

                var collateral = mortgageLoanCollateralService.GetSeMortageLoanAmortizationBasis(context, new GetSeAmortizationBasisRequest
                {
                    CreditNr = creditNr,
                    UseUpdatedBalance = true
                });

                if (collateral == null || collateral.Amorteringsunderlag == null)
                {
                    throw new NTechCoreWebserviceException("Missing amortization basis") { IsUserFacing = true, ErrorHttpStatusCode = 400 };
                }

                var underlag = collateral.Amorteringsunderlag;

                var propertyReference = collateral.PropertyIdWithLabel;
                ee["objectName"] = $"{propertyReference}";

                ee["amortizationBasisObjectValue"] = underlag.AmorteringsgrundandeVarde.ToString("C", c);
                ee["amortizationBasisDate"] = underlag.DatumAmorteringsgrundandeVarde.ToString("d", c);
                ee["amortizationBasisLoanAmount"] = underlag.AmorteringsgrundandeSkuld.ToString("C", c);

                ee["ruleNoneLoanAmount"] = underlag.SkuldEjOmfattasAmorteringskrav.ToString("C", c);
                ee["ruleR201616LoanAmount"] = underlag.SkuldOmfattasAmorteringskrav.ToString("C", c);
                ee["ruleR201732LoanAmount"] = underlag.SkuldOmfattasSkarptAmorteringskrav.ToString("C", c);
                ee["ruleAlternateLoanAmount"] = underlag.VaravOvanAlternativRegeln.ToString("C", c);
                ee["totalObjectMonthlyAmortizationAmount"] = underlag.TotalAmorteringObjektet.ToString("C", c);

                ee["totalStandardObjectBankMonthlyAmortizationAmount"] = underlag.TotalAmorteringKravObjektetBankenHuvud.ToString("C", c);
                ee["totalAlternateObjectBankMonthlyAmortizationAmount"] = underlag.TotalAmorteringKravObjektetBankenAlternativ.ToString("C", c);

                return ee;
            }
        }
    }

    public class SwedishAmorteringsunderlag
    {
        public decimal AmorteringsgrundandeVarde { get; set; }
        public DateTime DatumAmorteringsgrundandeVarde { get; set; }
        public decimal AmorteringsgrundandeSkuld { get; set; }
        public decimal SkuldEjOmfattasAmorteringskrav { get; set; }
        public decimal SkuldOmfattasAmorteringskrav { get; set; }
        public decimal SkuldOmfattasSkarptAmorteringskrav { get; set; }
        public decimal VaravOvanAlternativRegeln { get; set; }
        public decimal TotalAmorteringObjektet { get; set; }
        public decimal TotalAmorteringKravObjektetBankenHuvud { get; set; }
        public decimal TotalAmorteringKravObjektetBankenAlternativ { get; set; }

        public string PrettyPrint()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }

    public class CalculateMortgageLoanAmortizationBasisRequest
    {
        [Required]
        public List<MlAmortizationBasisRequestNewLoan> NewLoans { get; set; }

        [Required]
        public List<MlAmortizationBasisRequestExistingLoan> ExistingLoans { get; set; }

        [Required]
        public decimal ObjectValueAmount { get; set; }

        /// <summary>
        /// If left out will default to today
        /// </summary>
        public DateTime? ObjectValueDate { get; set; }

        /// <summary>
        /// This is used to indicate that keeping any legacy rulecodes is not allowed
        /// </summary>
        public bool IsNewObjectValueAmount { get; set; }

        [Required]
        public decimal CombinedYearlyIncomeAmount { get; set; }

        public decimal? OtherMortageLoansBalanceAmount { get; set; }

        public bool? IgnoreAlternateRule { get; set; }
        public bool? ForceAlternateRuleEvenIfWorse { get; set; }

        public abstract class MlAmortizationBasisRequestLoan
        {
            [Required]
            public string CreditNr { get; set; }
            [Required]
            public decimal CurrentBalanceAmount { get; set; }
        }

        public class MlAmortizationBasisRequestNewLoan : MlAmortizationBasisRequestLoan
        {

        }

        public class MlAmortizationBasisRequestExistingLoan : MlAmortizationBasisRequestLoan
        {
            /// <summary>
            /// Used to compute required amortization amount
            /// </summary>
            public decimal? MaxBalanceAmount { get; set; }

            /// <summary>
            /// none, r201616 (Amorteringskrav), r201723 (Skärpt Amorteringskrav)
            /// If empty will be interpreted as the latest framework which currently is 2017:23
            /// </summary>
            public string RuleCode { get; set; }

            public bool IsUsingAlternateRule { get; set; }

            [Required]
            public decimal MonthlyAmortizationAmount { get; set; }
        }
    }
}
