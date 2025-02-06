using Microsoft.VisualStudio.TestTools.UnitTesting;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.SharedStandard;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestsnPreCredit.StandardPolicyFilters
{
    /*
     These tests are intended to match the documentation here:
    https://naktergal.atlassian.net/wiki/spaces/PROD/pages/399442131/Kalp
    
    Specifically the linked excel model. If you change codes that causes these tests to break,
    make sure that the documentation is changed also so the code, tests and documentation are all in synch.    
     */
    [TestClass]
    public class UnsecuredLoanStandardLeftToLiveOnTests
    {
        [TestMethod]
        public void ExcelResult_Ltl_MatchesModel()
        {
            var result = GetExcelTestResult();
            Assert.AreEqual(13696, result.LtlAmount);
        }

        [TestMethod]
        public void ExcelResult_IncomeGroup_MatchesModel()
        {
            var result = GetExcelTestResult();
            Assert.AreEqual(42000, result.Groups.Single(x => x.Code == "applicant1Income").ContributionAmount + result.Groups.Single(x => x.Code == "applicant2Income").ContributionAmount);
        }

        [TestMethod]
        public void ExcelResult_HouseholdCostGroup_MatchesModel()
        {
            var result = GetExcelTestResult();
            Assert.AreEqual(-19205, result.Groups.Single(x => x.Code == "householdCosts").ContributionAmount);
        }

        [TestMethod]
        public void ExcelResult_HouseholdCostGroup_Applicant2RemovedFromHouseHold_MatchesModel()
        {
            var result = GetExcelTestResult(mutateApplicant: (applicantNr, applicantData) =>
            {
                if (applicantNr == 2)
                {
                    applicantData["isPartOfTheHousehold"] = "false";
                }
            });
            Assert.AreEqual(-13995, result.Groups.Single(x => x.Code == "householdCosts").ContributionAmount);
        }

        [TestMethod]
        public void ExcelResult_Ltl_Applicant2RemovedFromHouseHold_MatchesModel()
        {
            var result = GetExcelTestResult(mutateApplicant: (applicantNr, applicantData) =>
            {
                if (applicantNr == 2)
                {
                    applicantData["isPartOfTheHousehold"] = "false";
                }
            });
            Assert.AreEqual(18906, result.LtlAmount, Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        }

        [TestMethod]
        public void ExcelResult_LoanAndOtherFixedCostsAndAssetsGroup_MatchesModel()
        {
            var result = GetExcelTestResult();
            Assert.AreEqual(-(2899 + 5600 + 600), result.Groups.Single(x => x.Code == "loanAndOtherFixedCostsAndAssets").ContributionAmount);
        }

        #region "Excel model"
        private LoanStandardLtlResult GetExcelTestResult(Action<int, Dictionary<string, string>> mutateApplicant = null)
        {
            var applicationList = ComplexApplicationList.CreateEmpty("Application");

            applicationList.SetRow(1, new Dictionary<string, string>
            {
                { "requestedLoanAmount", "150000" },
                { "requestedRepaymentTime", "120m" },
                { "housingCostPerMonthAmount", "5000" },
                { "otherHouseholdFixedCostsAmount", "900" },
                { "otherHouseholdFinancialAssetsAmount", "300" },
                { "incomingChildSupportAmount", "500" },
                { "outgoingChildSupportAmount", "5" },
                { "childBenefitAmount", "50" }
            });

            var applicantList = ComplexApplicationList.CreateEmpty("Applicant");
            var applicant1Items = new Dictionary<string, string>
            {
                { "isPartOfTheHousehold", "true" },
                { "incomePerMonthAmount", "25000" }
            };
            mutateApplicant?.Invoke(1, applicant1Items);
            applicantList.SetRow(1, initialUniqueItems: applicant1Items);

            var applicant2Items = new Dictionary<string, string>
            {
                { "isPartOfTheHousehold", "true" },
                { "incomePerMonthAmount", "35000" }
            };
            mutateApplicant?.Invoke(2, applicant2Items);
            applicantList.SetRow(2, initialUniqueItems: applicant2Items);

            var ageInYearsByApplicantNr = new Dictionary<int, int?>
            {
                { 1, 52 },
                { 2, 48 }
            };

            var householdChildrenList = ComplexApplicationList.CreateEmpty("HouseholdChildren");
            householdChildrenList.AddRow(new Dictionary<string, string>
            {
                { "sharedCustody", "true" },
                { "ageInYears", "12" }
            });
            householdChildrenList.AddRow(new Dictionary<string, string>
            {
                { "sharedCustody", "false" },
                { "ageInYears", "14" }
            });

            var loansToSettleList = ComplexApplicationList.CreateEmpty("LoansToSettle");
            loansToSettleList.AddRow(new Dictionary<string, string>
            {
                { "shouldBeSettled", "false" },
                { "monthlyCostAmount", "200" }
            });
            loansToSettleList.AddRow(new Dictionary<string, string>
            {
                //shouldBeSettled left out to ensure missing is interpreted as false
                { "monthlyCostAmount", "400" }
            });

            var dataTables = new StrictMock<ILtlDataTables>();
            dataTables.Setup(x => x.IncomeTaxMultiplier).Returns(0.7m);
            dataTables.Setup(x => x.StressInterestRatePercent).Returns(20m);
            dataTables.Setup(x => x.CreditsUse360DayInterestYear).Returns(false);

            dataTables.Setup(x => x.GetIndividualAgeCost(12)).Returns(4140);
            dataTables.Setup(x => x.GetIndividualAgeCost(14)).Returns(4590);
            dataTables.Setup(x => x.GetIndividualAgeCost(48)).Returns(4710);
            dataTables.Setup(x => x.GetIndividualAgeCost(52)).Returns(4660);

            dataTables.Setup(x => x.GetHouseholdMemberCountCost(3)).Returns(3220m);
            dataTables.Setup(x => x.GetHouseholdMemberCountCost(4)).Returns(3720m);

            var ltlService = new LoanStandardLtlService(dataTables.Object);
            return ltlService.CalculateLeftToLiveOnForUnsecuredLoan(applicationList, applicantList, ageInYearsByApplicantNr, householdChildrenList, loansToSettleList);
        }
        #endregion
    }
}