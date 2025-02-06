using Newtonsoft.Json;
using nPreCredit;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlLegacyScenarioTests
    {
        [Test]
        public void TestHappyFlow1()
        {
            UlLegacyTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                AddAndCheckTestPersonOne(support);
                AddApprovedApplicationOnPersonOne(support);
                AnswerAdditionalQuestions(support);
                AddCreditOnPersonOne(support);
                TestSearchForCreditOnPersonOne(support);
                AddAndAuthenticateWithApiKey(support);
                var unplacedPaymentsEvent = AddUnplacedManualPayments(support);
                RepayUnplacedPayment(support, unplacedPaymentsEvent);
                AddAdditionalLoanOnCredit(support);
                ChangeTermsOnCredit(support, EmailProviderState.Down, 2);
                ChangeTermsOnCredit(support, EmailProviderState.Working, 3);
                SendSettlementSuggestions(support, EmailProviderState.Down, 4);
                SendSettlementSuggestions(support, EmailProviderState.Working, 5);
                EditKycQuestionTemplates(support);
                PlacePayments(support);
                ExportToCm1(support);
                using(var context = support.PreCreditContextService.CreateExtended())
                {
                    var d = context.CreditApplicationHeadersQueryable.Select(x => x.CurrentCreditDecision).Single();
                    Assert.That(((AcceptedCreditDecision)d).AcceptedDecisionModel, Is.EqualTo("{\"offer\":{\"amount\":8000.0,\"repaymentTimeInMonths\":84,\"marginInterestRatePercent\":12.0,\"referenceInterestRatePercent\":0.1,\"initialFeeAmount\":0.0,\"notificationFeeAmount\":5.0,\"annuityAmount\":141.65,\"effectiveInterestRatePercent\":14.09,\"totalPaidAmount\":12318.55,\"initialPaidToCustomerAmount\":8000.0},\"recommendation\":{\"HasOffer\":true,\"OfferedAmount\":8000.0,\"MaxOfferedAmount\":8000.0,\"OfferedRepaymentTimeInMonths\":84,\"OfferedInterestRatePercent\":12.0,\"OfferedNotificationFeeAmount\":5.0,\"OfferedInitialFeeAmount\":0.0,\"OfferedAdditionalLoanCreditNr\":null,\"OfferedAdditionalLoanNewAnnuityAmount\":null,\"OfferedAdditionalLoanNewMarginInterestPercent\":null,\"ScoringData\":{\"Items\":[{\"Name\":\"nrOfApplicants\",\"Value\":\"1\",\"ApplicantNr\":null},{\"Name\":\"isProviderApplication\",\"Value\":\"false\",\"ApplicantNr\":null},{\"Name\":\"scoreVersion\",\"Value\":\"2018\",\"ApplicantNr\":null},{\"Name\":\"requestedAmount\",\"Value\":\"8000\",\"ApplicantNr\":null},{\"Name\":\"currentInternalLoanBalance\",\"Value\":\"0\",\"ApplicantNr\":null},{\"Name\":\"campaignCode\",\"Value\":\"H00000\",\"ApplicantNr\":null},{\"Name\":\"requestedRepaymentTimeInYears\",\"Value\":\"7\",\"ApplicantNr\":null},{\"Name\":\"isPurposeSettlement\",\"Value\":\"false\",\"ApplicantNr\":null},{\"Name\":\"applicantsHaveSeparateLoans\",\"Value\":\"false\",\"ApplicantNr\":null},{\"Name\":\"randomNr\",\"Value\":\"100\",\"ApplicantNr\":null},{\"Name\":\"randomNrRejectBelowLimit\",\"Value\":\"51\",\"ApplicantNr\":null},{\"Name\":\"offeredLoanMonthlyCost\",\"Value\":\"146.65\",\"ApplicantNr\":null},{\"Name\":\"maxAllowedLoanAmount\",\"Value\":\"8000\",\"ApplicantNr\":null},{\"Name\":\"suggestedLoanAmount\",\"Value\":\"8000\",\"ApplicantNr\":null},{\"Name\":\"incomePerMonthAmount\",\"Value\":\"5000\",\"ApplicantNr\":1},{\"Name\":\"nrOfChildren\",\"Value\":\"0\",\"ApplicantNr\":1},{\"Name\":\"approvedSat\",\"Value\":\"true\",\"ApplicantNr\":1},{\"Name\":\"isMale\",\"Value\":\"false\",\"ApplicantNr\":1},{\"Name\":\"ageInYears\",\"Value\":\"47\",\"ApplicantNr\":1},{\"Name\":\"employment\",\"Value\":\"fulltime\",\"ApplicantNr\":1},{\"Name\":\"currentEmploymentMonthCount\",\"Value\":\"199\",\"ApplicantNr\":1},{\"Name\":\"marriage\",\"Value\":\"married\",\"ApplicantNr\":1},{\"Name\":\"housing\",\"Value\":\"rentedapartment\",\"ApplicantNr\":1},{\"Name\":\"mortgageLoanAmount\",\"Value\":\"10000\",\"ApplicantNr\":1},{\"Name\":\"currentlyOverdueNrOfDays\",\"Value\":\"0\",\"ApplicantNr\":1},{\"Name\":\"maxNrOfDaysBetweenDueDateAndPaymentEver\",\"Value\":\"0\",\"ApplicantNr\":1},{\"Name\":\"historicalDebtCollectionCount\",\"Value\":\"0\",\"ApplicantNr\":1},{\"Name\":\"maxNrOfDaysBetweenDueDateAndPaymentLastSixMonths\",\"Value\":\"0\",\"ApplicantNr\":1},{\"Name\":\"nrOfActiveLoans\",\"Value\":\"0\",\"ApplicantNr\":1},{\"Name\":\"existingCustomerBalance\",\"Value\":\"0\",\"ApplicantNr\":1},{\"Name\":\"minNrOfClosedNotificationsOnActiveLoans\",\"Value\":\"0\",\"ApplicantNr\":1},{\"Name\":\"pausedDays\",\"Value\":\"0\",\"ApplicantNr\":1},{\"Name\":\"activeApplicationCount\",\"Value\":\"0\",\"ApplicantNr\":1},{\"Name\":\"maxActiveApplicationAgeInDays\",\"Value\":\"0\",\"ApplicantNr\":1},{\"Name\":\"latestApplicationRejectionReasons\",\"Value\":\"null\",\"ApplicantNr\":1}]},\"PetrusVersion\":2,\"PetrusApplicationId\":\"CA1_1\",\"RejectionReasons\":null},\"application\":{\"nrOfApplicants\":1,\"applicant1\":{\"birthDate\":\"1975-02-15\",\"customerId\":\"1\",\"housingCostPerMonthAmount\":\"250\",\"incomePerMonthAmount\":\"5000\",\"mortgageLoanAmount\":\"10000\",\"carOrBoatLoanAmount\":\"0\",\"studentLoanAmount\":\"0\",\"otherLoanAmount\":\"0\",\"creditCardAmount\":\"0\",\"creditReportConsent\":\"True\",\"customerConsent\":\"True\",\"email\":\"BoRautavaara65489@rhyta.com\",\"phone\":\"024 320 9897\",\"education\":\"education_yrkesskola\",\"housing\":\"housing_hyresbostad\",\"employment\":\"employment_fastanstalld\",\"employedSinceMonth\":\"2005-08\",\"employer\":\"ica\",\"employerPhone\":\"46546546\",\"marriage\":\"marriage_gift\",\"nrOfChildren\":\"0\",\"approvedSat\":\"true\"},\"application\":{\"scoringVersion\":\"2018\",\"amount\":\"8000\",\"loansToSettleAmount\":\"0\",\"repaymentTimeInYears\":\"7\",\"campaignCode\":\"H00000\"}},\"otherApplications\":{\"applicant1\":[]},\"credits\":{\"applicant1\":[]}}"));
                }
            });
        }
    }
}