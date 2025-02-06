using Microsoft.VisualStudio.TestTools.UnitTesting;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.SharedStandard;
using System;
using System.Collections.Generic;

namespace TestsnPreCredit.StandardPolicyFilters
{
    /*
    Since UL and ML share the LoanStandardLtlService these tests only cover what's special about mortgage loans.
     */
    [TestClass]
    public class MortgageLoanStandardLeftToLiveOnTests
    {
        /*
         * Borrowing 1 200 000 at 1% stress interest => - 1000 interst cost per month
         * LTV of 80% means 2% amortization => - 2000 amortization per month
         * So in total 22 000 left over
         */
        [TestMethod]
        public void OnNewPurchase_InterestAndAmortizationPayments_AreIncludedInLtl()
        {
            var result = RunStandardTest(x =>
            {
                x.Application.AddRow(initialUniqueItems: new Dictionary<string, string>
                {
                    { "isPurchase", "true" },
                    { "objectPriceAmount", "1500000" },
                    { "objectValueAmount", "1500000" },
                    { "ownSavingsAmount", "300000" },
                });
            });

            Assert.AreEqual(MonthlyIncome - 1000 - 2000, result.LtlAmount);
        }

        /*
         * Moving a loan of 100 000 and adding 20 000 on an object worth 200 000
         * So 60% LTV => 1% amortization of 100 per month
         * Stress interest 1% => 100 per month
         */
        [TestMethod]
        public void OnMoveExistingLoanWithPaidToCustomer_InterestAndAmortizationPayments_AreIncludedInLtl()
        {
            var result = RunStandardTest(x =>
            {
                x.Application.AddRow(initialUniqueItems: new Dictionary<string, string>
                {
                    { "isPurchase", "false" },
                    { "objectValueAmount", "200000" },
                    { "paidToCustomerAmount", "20000" },
                });

                x.MortgageLoansToSettle.AddRow(initialUniqueItems: new Dictionary<string, string>
                {
                    { "currentDebtAmount", "100000" },
                    { "shouldBeSettled", "true" }
                });
            });

            Assert.AreEqual(MonthlyIncome - 100 - 100, result.LtlAmount);
        }
        /*
         * Keeping a loan of 100 000 with another bank and adding 24 000 on an object with us worth 200 000
         * So 62% LTV => 1% amortization on 24k of 20 per month
         * Stress interest 1% => 20 per month
         * Amortization on the old loan => 500 per month
         * Interest on the old loan 3% at 100k => 250 per month
         */
        [TestMethod]
        public void OnExtendExistingLoanWithoutMovingIt_OldLoanCostAndNewInterestAndAmortizationPayments_AreIncludedInLtl()
        {
            var result = RunStandardTest(x =>
            {
                x.Application.AddRow(initialUniqueItems: new Dictionary<string, string>
                {
                    { "isPurchase", "false" },
                    { "objectValueAmount", "200000" },
                    { "paidToCustomerAmount", "24000" },
                });

                x.MortgageLoansToSettle.AddRow(initialUniqueItems: new Dictionary<string, string>
                {
                    { "currentDebtAmount", "100000" },
                    { "currentMonthlyAmortizationAmount", "500" },
                    { "interestRatePercent", "3" },
                    { "shouldBeSettled", "false" }
                });
            });

            Assert.AreEqual(MonthlyIncome - 20 - 20 - 500 - 250, result.LtlAmount);
        }

        [TestMethod]
        public void PropertyMonthlyCosts_AreIncludedInLtl()
        {
            var result = RunStandardTest(x =>
            {
                x.Application.AddRow(initialUniqueItems: new Dictionary<string, string>
                {
                    { "objectMonthlyFeeAmount", "500" },
                    { "objectOtherMonthlyCostsAmount", "1000" }
                });
            });

            Assert.AreEqual(MonthlyIncome - 500 - 1000, result.LtlAmount);
        }

        [TestMethod]
        public void HousingMonthlyCosts_AreNotIncludedInLtl()
        {
            var result = RunStandardTest(x =>
            {
                x.Application.AddRow(initialUniqueItems: new Dictionary<string, string>
                {
                    { "housingCostPerMonthAmount", "500" },
                    { "otherHouseholdFixedCostsAmount", "1000" }
                });
            });

            Assert.AreEqual(MonthlyIncome, result.LtlAmount);
        }

        const int MonthlyIncome = 25000;

        /// <summary>
        /// Shared starting point with a single applicant with no individual or household costs or income tax
        /// Income is 25 000 so that is the ltl starting point
        /// </summary>
        private LoanStandardLtlResult RunStandardTest(Action<(ComplexApplicationList Application, ComplexApplicationList MortgageLoansToSettle)> setup)
        {
            const int ApplicantAge = 50;

            var dataTables = new StrictMock<ILtlDataTables>();
            dataTables.Setup(x => x.IncomeTaxMultiplier).Returns(1m);
            dataTables.Setup(x => x.GetIndividualAgeCost(50)).Returns(0);
            dataTables.Setup(x => x.GetHouseholdMemberCountCost(1)).Returns(0);
            dataTables.Setup(x => x.StressInterestRatePercent).Returns(1m);

            var ltlService = new LoanStandardLtlService(dataTables.Object);

            var applicationList = ComplexApplicationList.CreateEmpty("Application");

            var applicantList = ComplexApplicationList.CreateEmpty("Applicant");
            applicantList.AddRow(initialUniqueItems: new Dictionary<string, string>
                {
                    { "isPartOfTheHousehold", "true" },
                    { "incomePerMonthAmount", MonthlyIncome.ToString() }
                });

            var householdChildrenList = ComplexApplicationList.CreateEmpty("HouseholdChildren");
            var loansToSettleList = ComplexApplicationList.CreateEmpty("LoansToSettle");
            var mortgageLoansToSettleList = ComplexApplicationList.CreateEmpty("MortgageLoansToSettle");
            var ageInYearsByApplicantNr = new Dictionary<int, int?> { { 1, ApplicantAge } };

            setup((Application: applicationList, MortgageLoansToSettle: mortgageLoansToSettleList));

            return ltlService.CalculateLeftToLiveOnForMortgageLoan(
                applicationList,
                applicantList,
                ageInYearsByApplicantNr,
                householdChildrenList,
                loansToSettleList,
                mortgageLoansToSettleList);
        }
    }
}