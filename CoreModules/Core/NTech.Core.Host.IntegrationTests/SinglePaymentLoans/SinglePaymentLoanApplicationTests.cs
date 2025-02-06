using nPreCredit.Code.Services;
using nPreCredit.Code.Services.NewUnsecuredLoans;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.PreCredit.Shared.Models;

namespace NTech.Core.Host.IntegrationTests.SinglePaymentLoans
{
    internal class SinglePaymentLoanApplicationTests
    {
        [Test]
        public void TenDayApplication()
        {
            new Test(isDays: true).RunTest();
        }

        [Test]
        public void FourMonthApplication()
        {
            new Test(isDays: false).RunTest();
        }

        public class Test : SinglePaymentLoansTestRunner
        {
            private readonly bool isDays;

            public Test(bool isDays)
            {
                this.isDays = isDays;
            }
            protected override void DoTest()
            {
                Support.WithPreCreditDb(context =>
                {
                    context.HandlerLimitLevels.Add(new nPreCredit.DbModel.HandlerLimitLevel
                    {
                        IsOverrideAllowed = true,
                        LimitLevel = 2,
                        HandlerUserId = 0
                    });
                    context.SaveChanges();
                    return "";
                });
                var applicationService = Support.GetRequiredService<CreateApplicationUlStandardService>();

                TestPersons.EnsureTestPerson(Support, 1);
                var personData = TestPersons.GetTestPersonDataBySeed(Support, 1);

                var request = new UlStandardApplicationRequest
                {
                    RequestedAmount = 1000,
                    RequestedRepaymentTimeInMonths = isDays ? null : 4,
                    RequestedRepaymentTimeInDays = isDays ? 10 : null,
                    HousingCostPerMonthAmount = 2000,
                    HousingType = "tenant",
                    NrOfHouseholdChildren = 4,
                    Applicants = new List<UlStandardApplicationRequest.ApplicantModel>
                    {
                        new UlStandardApplicationRequest.ApplicantModel
                        {
                            CivilStatus = "co_habitant",
                            ClaimsToBePep = false,
                            ClaimsToHaveKfmDebt = false,
                            EmployedSince = new DateTime(1994,12,10),
                            EmployerName = "Företaget AB",
                            EmployerPhone = "010 111 222 333",
                            EmploymentStatus = "unemployed",
                            HasConsentedToCreditReport = true,
                            HasConsentedToShareBankAccountData = true,
                            MonthlyIncomeAmount = 36000,
                            CivicRegNr = personData["civicRegNr"]
                        }
                    },
                    Meta = new UlStandardApplicationRequest.MetadataModel
                    {
                        ProviderName = "self"
                    }
                };

                var applicationNr = applicationService.CreateApplication(request, isFromInsecureSource: false, requestJson: null);
                Support.WithPreCreditDb(context =>
                {
                    var applicationData = ComplexApplicationListService.GetListRow(applicationNr, "Application", 1, context);
                    if(isDays)
                    {
                        Assert.That(applicationData.UniqueItems.Opt("requestedRepaymentTime"), Is.EqualTo("10d"));
                    }
                    else
                    {
                        Assert.That(applicationData.UniqueItems.Opt("requestedRepaymentTime"), Is.EqualTo("4m"));
                    }
                    return "";
                });

                var recommendationService = Support.GetRequiredService<CreditRecommendationUlStandardService>();
                recommendationService.AcceptInitialCreditDecision(applicationNr, new UnsecuredLoanStandardCurrentCreditDecisionOfferModel 
                {
                    RepaymentTimeInMonths = 4,
                    NotificationFeeAmount = 5m,
                    PaidToCustomerAmount = 1000m,
                    NominalInterestRatePercent = 15m,
                    ReferenceInterestRatePercent = 0.2m
                }, true, false, recommendation: null);

                Support.WithPreCreditDb(context =>
                {
                    var decisionItems = context.CreditDecisionItems.Where(x => x.CreditDecisionId == 1 && x.IsRepeatable == false).Select(x => new { x.ItemName, x.Value }).ToDictionary(x => x.ItemName, x => x.Value);
                    Assert.That(decisionItems.Opt("paidToCustomerAmount"), Is.EqualTo("1000"));
                    Assert.That(decisionItems.Opt("repaymentTimeInMonths"), Is.EqualTo("4"));
                    Assert.That(decisionItems.Opt("effectiveInterestRatePercent"), Is.EqualTo("27.63"));
                    Assert.That(decisionItems.Opt("annuityAmount"), Is.EqualTo("257.97"));
                    return "";
                });
            }
        }
    }
}
