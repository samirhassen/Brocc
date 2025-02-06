using NTech.Banking.LoanModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace nPreCredit.Code.Services.SharedStandard
{
    public class LoanStandardLtlService
    {
        private static Lazy<CultureInfo> formattingCulture = new Lazy<CultureInfo>(() => CultureInfo.GetCultureInfo("sv-SE"));
        private readonly ILtlDataTables dataTables;

        private static CultureInfo FormattingCulture => formattingCulture.Value;

        public LoanStandardLtlService(ILtlDataTables dataTables)
        {
            this.dataTables = dataTables;
        }

        private decimal LtlIncomeTaxMultiplier => dataTables.IncomeTaxMultiplier ?? 0.7m;
        private int LtlDefaultChildAgeInYears => dataTables.DefaultChildAgeInYears ?? 10;
        private int LtlDefaultApplicantAgeInYears => dataTables.DefaultApplicantAgeInYears ?? 30;

        public LoanStandardLtlResult CalculateLeftToLiveOnForMortgageLoan(
            ComplexApplicationList applicationList,
            ComplexApplicationList applicantList,
            Dictionary<int, int?> ageInYearsByApplicantNr,
            ComplexApplicationList householdChildrenList,
            ComplexApplicationList loansToSettleList,
            ComplexApplicationList mortgageLoansToSettleList) => CalculateLeftToLiveOn(
                true, applicationList, applicantList, ageInYearsByApplicantNr,
                householdChildrenList, loansToSettleList, mortgageLoansToSettleList);

        public LoanStandardLtlResult CalculateLeftToLiveOnForUnsecuredLoan(
            ComplexApplicationList applicationList,
            ComplexApplicationList applicantList,
            Dictionary<int, int?> ageInYearsByApplicantNr,
            ComplexApplicationList householdChildrenList,
            ComplexApplicationList loansToSettleList) => CalculateLeftToLiveOn(
                false, applicationList, applicantList, ageInYearsByApplicantNr,
                householdChildrenList, loansToSettleList, ComplexApplicationList.CreateEmpty("MortgageLoansToSettle"));

        private LoanStandardLtlResult CalculateLeftToLiveOn(
            bool isMortgageLoan,
            ComplexApplicationList applicationList,
            ComplexApplicationList applicantList,
            Dictionary<int, int?> ageInYearsByApplicantNr,
            ComplexApplicationList householdChildrenList,
            ComplexApplicationList loansToSettleList,
            ComplexApplicationList mortgageLoansToSettleList)
        {
            try
            {
                var ltl = new LoanStandardLtlResult
                {
                    Groups = new List<LoanStandardLtlResult.Group>()
                };

                //***************************
                //** Applicant income groups *
                //***************************
                foreach (var applicant in applicantList.GetRows())
                {
                    ltl.Groups.Add(CreateApplicantIncomeLtlGroup(applicant));
                }

                //***************************
                //** Household costs group  *
                //***************************
                ltl.Groups.Add(CreateHouseholdCostsLtlGroup(applicantList, householdChildrenList, ageInYearsByApplicantNr, applicationList));

                //*********************************************
                //** Loans and other fixed costs and assets  **
                //*********************************************
                ltl.Groups.Add(CreateLoansFixedCostsAssetsLtlGroup(applicationList, applicantList, loansToSettleList, mortgageLoansToSettleList, isMortgageLoan));

                //******************************
                //** Sums and display values  **
                //******************************
                foreach (var group in ltl.Groups)
                {
                    group.ContributionAmount = group.ContributionItems.Sum(x => x.ContributionAmount);
                    group.DisplayContributionAmount = group.ContributionAmount.ToString("N0", FormattingCulture);
                    foreach (var item in group.ContributionItems)
                    {
                        item.DisplayContributionAmount = item.ContributionAmount.ToString("N0", FormattingCulture);
                    }
                }

                ltl.LtlAmount = ltl.Groups.Sum(x => x.ContributionAmount);
                ltl.DisplayLtlAmount = ltl.LtlAmount.Value.ToString("N0", FormattingCulture);

                //NOTE: These two are not really needed for the model but adding translation support in the future will be way easier if all the strings are in the model vs all but these two
                ltl.DisplaySummaryHeader = "Sum left to live on";
                ltl.DisplaySummaryFooter = "Left to live on result";

                return ltl;
            }
            catch (LtlComputationException ex)
            {
                return new LoanStandardLtlResult
                {
                    UndefinedReasonMessage = ex.Message
                };
            }
        }

        private LoanStandardLtlResult.Group CreateLoansFixedCostsAssetsLtlGroup(
            ComplexApplicationList applicationList,
            ComplexApplicationList applicantList,
            ComplexApplicationList loansToSettleList,
            ComplexApplicationList mortgageLoansToSettleList,
            bool isMortgageLoan)
        {
            var ltlStressInterestRatePercent = dataTables.StressInterestRatePercent;

            var applicationRow = applicationList.GetRow(1, true);

            var otherHouseholdFinancialAssets = applicationRow.GetUniqueItemDecimal("otherHouseholdFinancialAssetsAmount") ?? 0m;

            //**************************
            //*** Other loan costs  ****
            //**************************
            var currentLoansNotToBeSettledMonthlyCost = loansToSettleList.GetRows().Select(x => x.GetUniqueItem("shouldBeSettled") == "true"
                ? 0m
                : x.GetUniqueItemDecimal("monthlyCostAmount") ?? 0m).Sum();

            currentLoansNotToBeSettledMonthlyCost += mortgageLoansToSettleList.GetRows().Select(x => x.GetUniqueItem("shouldBeSettled") == "true"
                ? 0m
                : CalculateMortgageLoanToSettleMonthlyAmount(x) ?? 0m).Sum();

            var g = new LoanStandardLtlResult.Group
            {
                Code = $"loanAndOtherFixedCostsAndAssets",
                DisplayGroupHeader = $"Loans and other fixed costs and assets",
                DisplayContributionHeader = $"Sum loans and other fixed costs and assets"
            };

            if (isMortgageLoan)
            {
                var objectValueAmount = applicationRow.GetUniqueItemDecimal("objectValueAmount");
                g.InformationItems.Add(new LoanStandardLtlResult.InformationItem
                {
                    ItemCode = "estimatedValue",
                    DisplayLabel = "Estimated value",
                    Value = objectValueAmount,
                    DisplayValue = objectValueAmount.HasValue ? (Math.Round(objectValueAmount.Value)).ToString("N0", FormattingCulture) : "missing"
                });

                var requestedLoanAmount = MortgageLoanLtxService.CalculateRequestedLoanAmount(applicationList, mortgageLoansToSettleList);

                g.InformationItems.Add(new LoanStandardLtlResult.InformationItem
                {
                    ItemCode = "requestedLoanAmount",
                    DisplayLabel = "Requested loan amount",
                    Value = requestedLoanAmount,
                    DisplayValue = ((int)Math.Round(requestedLoanAmount)).ToString("N0", FormattingCulture)
                });

                var loansThatShouldNotBeSettledAmount = mortgageLoansToSettleList
                        .GetRows()
                        .Where(x => x.GetUniqueItemBoolean("shouldBeSettled") == false)
                        .Sum(x => x.GetUniqueItemDecimal("currentDebtAmount") ?? 0m);

                g.InformationItems.Add(new LoanStandardLtlResult.InformationItem
                {
                    ItemCode = "loansThatShouldNotBeSettledAmount",
                    DisplayLabel = "Loans that should not be settled",
                    Value = requestedLoanAmount,
                    DisplayValue = ((int)Math.Round(loansThatShouldNotBeSettledAmount)).ToString("N0", FormattingCulture)
                });

                var loanToIncome = MortgageLoanLtxService.CalculateLoanToIncome(applicationList, applicantList, mortgageLoansToSettleList, loansToSettleList, null);
                var loanToValue = MortgageLoanLtxService.CalculateLoanToValue(applicationList, mortgageLoansToSettleList, null);

                decimal ltiAmortizationAmount = 0m;
                decimal ltv50AmortizationAmount = 0m;
                decimal ltv70AmortizationAmount = 0m;
                var onePercentMonthlyAmortizationAmount = Math.Round(requestedLoanAmount * 0.01m / 12m, 2);
                if (!loanToIncome.HasValue || loanToIncome.Value > 4.5m) //Assume the worst if missing
                    ltiAmortizationAmount = onePercentMonthlyAmortizationAmount;
                if (!loanToValue.HasValue || loanToValue.Value > 0.5m)
                    ltv50AmortizationAmount = onePercentMonthlyAmortizationAmount;
                if (!loanToValue.HasValue || loanToValue.Value > 0.7m)
                    ltv70AmortizationAmount = onePercentMonthlyAmortizationAmount;

                var totalAmortizationAmount = ltiAmortizationAmount + ltv50AmortizationAmount + ltv70AmortizationAmount;

                g.InformationItems.Add(new LoanStandardLtlResult.InformationItem
                {
                    ItemCode = "loanToIncome",
                    DisplayLabel = "Loan to income",
                    Value = loanToIncome,
                    DisplayValue = loanToIncome.HasValue ? (loanToIncome.Value).ToString("N2", FormattingCulture) : "missing"
                });
                g.InformationItems.Add(new LoanStandardLtlResult.InformationItem
                {
                    ItemCode = "ltiAmortizationAmount",
                    DisplayLabel = "Loan to income > 4,5 (+ 1% amortization)",
                    Value = ltiAmortizationAmount,
                    DisplayValue = (Math.Round(ltiAmortizationAmount)).ToString("N0", FormattingCulture)
                });
                g.InformationItems.Add(new LoanStandardLtlResult.InformationItem
                {
                    ItemCode = "loanToValue",
                    DisplayLabel = "Loan to value",
                    Value = loanToValue,
                    DisplayValue = loanToValue.HasValue ? ((int)Math.Round(loanToValue.Value * 100m)).ToString(FormattingCulture) + "%" : "missing"
                });
                g.InformationItems.Add(new LoanStandardLtlResult.InformationItem
                {
                    ItemCode = "ltv50AmortizationAmount",
                    DisplayLabel = "Loan to value > 50% (+ 1% amortization)",
                    Value = requestedLoanAmount,
                    DisplayValue = (Math.Round(ltv50AmortizationAmount)).ToString("N0", FormattingCulture)
                });
                g.InformationItems.Add(new LoanStandardLtlResult.InformationItem
                {
                    ItemCode = "ltv70AmortizationAmount",
                    DisplayLabel = "Loan to value > 70% (+ 1% amortization)",
                    Value = requestedLoanAmount,
                    DisplayValue = (Math.Round(ltv70AmortizationAmount)).ToString("N0", FormattingCulture)
                });

                g.ContributionItems.Add(new LoanStandardLtlResult.ContributionItem
                {
                    ItemCode = "amortizationAmount",
                    DisplayLabel = "Amortization cost",
                    ContributionAmount = -(int)Math.Round(totalAmortizationAmount),
                    DisplayContributionAmount = ((int)Math.Round(totalAmortizationAmount)).ToString("N0", FormattingCulture)
                });

                var stressInterestAmount = (int)Math.Round(requestedLoanAmount * ltlStressInterestRatePercent / 100m / 12m);
                g.ContributionItems.Add(new LoanStandardLtlResult.ContributionItem
                {
                    ItemCode = "stressInterestAmount",
                    DisplayLabel = $"Interest cost (stressed interest {ltlStressInterestRatePercent / 100m:P})",
                    ContributionAmount = -stressInterestAmount
                });

                g.ContributionItems.Add(new LoanStandardLtlResult.ContributionItem
                {
                    ItemCode = "objectMonthlyFeeAmount",
                    DisplayLabel = $"Property monthly fee",
                    ContributionAmount = -(int)Math.Round(applicationRow.GetUniqueItemDecimal("objectMonthlyFeeAmount") ?? 0m)
                });

                g.ContributionItems.Add(new LoanStandardLtlResult.ContributionItem
                {
                    ItemCode = "objectOtherMonthlyCostsAmount",
                    DisplayLabel = $"Property other monthly costs",
                    ContributionAmount = -(int)Math.Round(applicationRow.GetUniqueItemDecimal("objectOtherMonthlyCostsAmount") ?? 0m)
                });
            }
            else
            {
                var requestedLoanAmount = applicationRow.GetUniqueItemDecimal("requestedLoanAmount");
                if (!requestedLoanAmount.HasValue)
                    throw new LtlComputationException("Missing requestedLoanAmount");

                var requestedRepaymentTime = applicationRow.GetUniqueItemTimeCountWithPeriodMarker("requestedRepaymentTime");
                if (!requestedRepaymentTime.HasValue)
                    throw new LtlComputationException("Missing requestedRepaymentTime");

                PaymentPlanCalculation paymentPlan;
                if(requestedRepaymentTime.Value.IsDays)
                {
                    //TODO: Initial fee
                    paymentPlan = PaymentPlanCalculation.CaclculateSinglePaymentWithRepaymentTimeInDays(requestedLoanAmount.Value, requestedRepaymentTime.Value.Count, ltlStressInterestRatePercent);
                }
                else
                {
                    paymentPlan = PaymentPlanCalculation
                        .BeginCreateWithRepaymentTime(requestedLoanAmount.Value, requestedRepaymentTime.Value.Count, ltlStressInterestRatePercent, true, null, dataTables.CreditsUse360DayInterestYear)
                        .EndCreate();
                }

                g.InformationItems.Add(new LoanStandardLtlResult.InformationItem
                {
                    ItemCode = "requestedLoanAmount",
                    DisplayLabel = "Requested loan amount",
                    Value = requestedLoanAmount,
                    DisplayValue = ((int)Math.Round(requestedLoanAmount.Value)).ToString("N0", FormattingCulture)
                });
                if(requestedRepaymentTime.Value.IsDays)
                {
                    g.InformationItems.Add(new LoanStandardLtlResult.InformationItem
                    {
                        ItemCode = "requestedRepaymentTimeDays",
                        DisplayLabel = "Requested repayment days",
                        Value = requestedRepaymentTime.Value.Count,
                        DisplayValue = requestedRepaymentTime.Value.Count.ToString("N0", FormattingCulture)
                    });
                    g.ContributionItems.Add(new LoanStandardLtlResult.ContributionItem
                    {
                        ItemCode = "newLoanMonthlyAmount",
                        DisplayLabel = "New loan monthly cost",
                        ContributionAmount = -(int)Math.Round(paymentPlan.Payments[0].TotalAmount)
                    });
                }
                else
                {
                    g.InformationItems.Add(new LoanStandardLtlResult.InformationItem
                    {
                        ItemCode = "requestedRepaymentTimeMonths",
                        DisplayLabel = "Requested repayment months",
                        Value = requestedRepaymentTime.Value.Count,
                        DisplayValue = requestedRepaymentTime.Value.Count.ToString("N0", FormattingCulture)
                    });
                    g.ContributionItems.Add(new LoanStandardLtlResult.ContributionItem
                    {
                        ItemCode = "newLoanMonthlyAmount",
                        DisplayLabel = "New loan monthly cost",
                        ContributionAmount = -(int)Math.Round(paymentPlan.AnnuityAmount)
                    });
                }

                var housingCostPerMonthAmount = applicationRow.GetUniqueItemDecimal("housingCostPerMonthAmount") ?? 0m;
                g.ContributionItems.Add(new LoanStandardLtlResult.ContributionItem
                {
                    ItemCode = "housingCostsPerMonth",
                    DisplayLabel = "Housing costs per month (interest, amortization, monthly fee)",
                    ContributionAmount = -(int)Math.Round(housingCostPerMonthAmount)
                });

                var otherHouseholdFixedCosts = applicationRow.GetUniqueItemDecimal("otherHouseholdFixedCostsAmount") ?? 0m;
                g.ContributionItems.Add(new LoanStandardLtlResult.ContributionItem
                {
                    ItemCode = "otherFixedHousingCosts",
                    DisplayLabel = "Other fixed housing costs (water, electriciy, garbage collection etc.)",
                    ContributionAmount = -(int)Math.Round(otherHouseholdFixedCosts)
                });
            }

            g.InformationItems.Add(new LoanStandardLtlResult.InformationItem
            {
                ItemCode = "stressedInterest",
                DisplayLabel = "Stressed interest",
                Value = ltlStressInterestRatePercent,
                DisplayValue = (ltlStressInterestRatePercent / 100m).ToString("P", FormattingCulture)
            });

            g.ContributionItems.Add(new LoanStandardLtlResult.ContributionItem
            {
                ItemCode = "sumOtherLoansMonthlyAmount",
                DisplayLabel = "Sum other loans monthly amount",
                ContributionAmount = -(int)Math.Round(currentLoansNotToBeSettledMonthlyCost)
            });
            g.ContributionItems.Add(new LoanStandardLtlResult.ContributionItem
            {
                ItemCode = "otherHouseholdFinancialMonthlyAssets",
                DisplayLabel = "Other household financial monthly assets",
                ContributionAmount = (int)Math.Round(otherHouseholdFinancialAssets)
            });

            return g;
        }

        private LoanStandardLtlResult.Group CreateHouseholdCostsLtlGroup(
            ComplexApplicationList applicantList,
            ComplexApplicationList householdChildrenList,
            Dictionary<int, int?> ageInYearsByApplicantNr,
            ComplexApplicationList applicationList)
        {
            var g = new LoanStandardLtlResult.Group
            {
                Code = "householdCosts",
                DisplayGroupHeader = "Household costs",
                DisplayContributionHeader = "Sum household costs"
            };

            foreach (var applicantRow in applicantList.GetRows())
            {
                var applicantNr = applicantRow.Nr;
                var age = ageInYearsByApplicantNr[applicantNr] ?? LtlDefaultApplicantAgeInYears;
                var isApplicantPartOfHousehold = IsApplicantPartOfHousehold(applicantRow);
                g.ContributionItems.Add(new LoanStandardLtlResult.ContributionItem
                {
                    ContributionAmount = isApplicantPartOfHousehold ? -(int)Math.Round(dataTables.GetIndividualAgeCost(age)) : 0,
                    DisplayLabel = $"Applicant {applicantNr} individual cost",
                    ItemCode = $"applicant{applicantNr}IndividualCost"
                });
                g.InformationItems.Add(new LoanStandardLtlResult.InformationItem
                {
                    ItemCode = $"applicant{applicantNr}Age",
                    DisplayLabel = $"Applicant {applicantNr} age",
                    Value = age,
                    DisplayValue = age.ToString(FormattingCulture)
                });
                g.InformationItems.Add(new LoanStandardLtlResult.InformationItem
                {
                    ItemCode = $"applicant{applicantNr}IsPartOfHousehold",
                    DisplayLabel = $"Applicant {applicantNr} part of household",
                    DisplayValue = BoolToDisplayValue(isApplicantPartOfHousehold),
                    Value = null, ////Not a single value
                });
            }
            foreach (var childRow in householdChildrenList.GetRows())
            {
                var childNr = childRow.Nr;
                var childAge = childRow.GetUniqueItemInteger("ageInYears") ?? LtlDefaultChildAgeInYears;
                var sharedCustody = childRow.GetUniqueItemBoolean("sharedCustody") ?? false;
                var childIndividualCost = (int)Math.Round(dataTables.GetIndividualAgeCost(childAge) * (sharedCustody ? 0.5m : 1m));
                g.ContributionItems.Add(new LoanStandardLtlResult.ContributionItem
                {
                    ItemCode = $"child{childNr}IndividualCost",
                    DisplayLabel = $"Child {childNr} individual cost",
                    ContributionAmount = -childIndividualCost
                });
                g.InformationItems.Add(new LoanStandardLtlResult.InformationItem
                {
                    ItemCode = $"child{childNr}AgeAndSharedCustody",
                    DisplayLabel = $"Child {childNr}, age, shared custody?",
                    DisplayValue = $"{childAge}, {BoolToDisplayValue(sharedCustody)}",
                    Value = null //Not a single value
                });
            }

            var application = applicationList.GetRow(1, true);
            g.ContributionItems.Add(new LoanStandardLtlResult.ContributionItem
            {
                ItemCode = $"outgoingChildSupport",
                DisplayLabel = "Pays in child support",
                ContributionAmount = -(int)Math.Round(application.GetUniqueItemDecimal("outgoingChildSupportAmount") ?? 0m)
            });
            g.ContributionItems.Add(new LoanStandardLtlResult.ContributionItem
            {
                ItemCode = $"incomingChildSupport",
                DisplayLabel = "Receives in child support",
                ContributionAmount = (int)Math.Round(application.GetUniqueItemDecimal("incomingChildSupportAmount") ?? 0m)
            });
            g.ContributionItems.Add(new LoanStandardLtlResult.ContributionItem
            {
                ItemCode = $"childBenefitAmount",
                DisplayLabel = "Child benefit amount",
                ContributionAmount = (int)Math.Round(application.GetUniqueItemDecimal("childBenefitAmount") ?? 0m)
            });

            var nrOfPersonsInHousehold =
                applicantList.GetRows().Count(IsApplicantPartOfHousehold)
                +
                householdChildrenList.GetRows().Count;

            var sharedCosts = dataTables.GetHouseholdMemberCountCost(nrOfPersonsInHousehold);
            g.ContributionItems.Add(new LoanStandardLtlResult.ContributionItem
            {
                ItemCode = "sharedCosts",
                DisplayLabel = "Shared costs",
                ContributionAmount = -(int)Math.Round(sharedCosts)
            });
            g.InformationItems.Add(new LoanStandardLtlResult.InformationItem
            {
                ItemCode = "nrOfPersonsInHousehold",
                DisplayLabel = "Nr of persons in household",
                Value = nrOfPersonsInHousehold,
                DisplayValue = nrOfPersonsInHousehold.ToString(FormattingCulture)
            });
            return g;
        }

        private LoanStandardLtlResult.Group CreateApplicantIncomeLtlGroup(ComplexApplicationList.Row applicant)
        {
            var applicantNr = applicant.Nr;
            var g = new LoanStandardLtlResult.Group
            {
                Code = $"applicant{applicantNr}Income",
                DisplayGroupHeader = $"Applicant {applicantNr} income",
                DisplayContributionHeader = $"Applicant {applicantNr} contribution to household income"
            };

            //Contribution items
            var taxMultiplier = LtlIncomeTaxMultiplier;
            var applicantIncomeBeforeTax = applicant.GetUniqueItemDecimal("incomePerMonthAmount") ?? 0m;
            var applicantIncomeAfterTax = (int)Math.Round(applicantIncomeBeforeTax * taxMultiplier);

            g.ContributionItems.Add(new LoanStandardLtlResult.ContributionItem
            {
                ItemCode = $"applicant{applicantNr}Income",
                DisplayLabel = "Monthly income after tax",
                ContributionAmount = applicantIncomeAfterTax
            });

            //Information items
            g.InformationItems.Add(new LoanStandardLtlResult.InformationItem
            {
                ItemCode = $"applicant{applicantNr}TaxPercent",
                DisplayLabel = "Tax",
                Value = 1m - taxMultiplier,
                DisplayValue = (1m - taxMultiplier).ToString("P", FormattingCulture)
            });
            g.InformationItems.Add(new LoanStandardLtlResult.InformationItem
            {
                ItemCode = $"applicant{applicantNr}IncomeBeforeTax",
                DisplayLabel = "Monthly income before tax",
                DisplayValue = ((int)Math.Round(applicantIncomeBeforeTax)).ToString("N0", FormattingCulture),
                Value = applicantIncomeBeforeTax
            });
            return g;
        }

        private bool IsApplicantPartOfHousehold(ComplexApplicationList.Row applicantRow) =>
            applicantRow.GetUniqueItemBoolean("isPartOfTheHousehold").GetValueOrDefault();

        private string BoolToDisplayValue(bool value) => value ? "yes" : "no";

        /// <summary>
        /// Calculate monthly amount from amortization, current debt and interest. Ignores the should be settled flag.
        /// </summary>
        public static decimal? CalculateMortgageLoanToSettleMonthlyAmount(ComplexApplicationList.Row row)
        {
            var currentDebtAmount = row.GetUniqueItemDecimal("currentDebtAmount");
            var currentMonthlyAmortizationAmount = row.GetUniqueItemDecimal("currentMonthlyAmortizationAmount");
            var interestRatePercent = row.GetUniqueItemDecimal("interestRatePercent");
            decimal? currentMonthlyInterestAmount = null;
            if (currentDebtAmount.HasValue && interestRatePercent.HasValue)
                currentMonthlyInterestAmount = Math.Round(currentDebtAmount.Value * interestRatePercent.Value / 100m / 12m);

            return currentMonthlyAmortizationAmount.HasValue || currentMonthlyInterestAmount.HasValue
                ? (currentMonthlyAmortizationAmount ?? 0m) + (currentMonthlyInterestAmount ?? 0m)
                : new decimal?();
        }
    }

    public class LtlComputationException : Exception
    {
        public LtlComputationException(string message) : base(message)
        {
        }
    }

    public class LoanStandardLtlResult
    {
        public int? LtlAmount { get; set; }
        public string DisplayLtlAmount { get; set; }
        public string DisplaySummaryHeader { get; set; }
        public string DisplaySummaryFooter { get; set; }
        public string UndefinedReasonMessage { get; set; }
        public List<Group> Groups { get; set; }
        public class Group
        {
            public Group()
            {
                ContributionItems = new List<ContributionItem>();
                InformationItems = new List<InformationItem>();
            }
            public string Code { get; set; }
            public int ContributionAmount { get; set; }
            public string DisplayGroupHeader { get; set; }
            public string DisplayContributionHeader { get; set; }
            public string DisplayContributionAmount { get; set; }
            public List<ContributionItem> ContributionItems { get; set; }
            public List<InformationItem> InformationItems { get; set; }
        }
        public class ContributionItem
        {
            public string ItemCode { get; set; }
            public int ContributionAmount { get; set; }
            public string DisplayLabel { get; set; }
            public string DisplayContributionAmount { get; set; }
        }
        public class InformationItem
        {
            public string ItemCode { get; set; }
            public decimal? Value { get; set; }
            public string DisplayLabel { get; set; }
            public string DisplayValue { get; set; }
        }
    }
}