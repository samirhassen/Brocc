using nCredit.DbModel.BusinessEvents;
using nCredit.DomainModel;
using NTech.Core.Host.IntegrationTests.MlStandard.Utilities;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests.MlStandard
{
    public partial class ChangeTermsTests
    {
        [Test]
        public void TestHappyFlow()
        {
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var daysUntilNextRebind = 1;
                var newTerms = new MlNewChangeTerms
                {
                    NewFixedMonthsCount = 12,
                    NewMarginInterestRatePercent = 0.10m,
                    NewReferenceInterestRatePercent = 3.75m,
                    NewInterestBoundFrom = support.Clock.Today.AddDays(daysUntilNextRebind)
                };

                var loanAmount = 1900000m;
                var pendingChangeId = 1;
                var credit = CreditsMlStandard.CreateCredit(
                         support: support,
                         creditNumber: 1,
                         loanAmount: loanAmount,
                         referenceInterestRatePercent: newTerms.NewReferenceInterestRatePercent,
                         interestRebindMounthCount: newTerms.NewFixedMonthsCount);

                ComputeNewTerms(support, credit, loanAmount, newTerms);
                StartChangeTerms(support, credit, newTerms);
                ScheduleChangeTerms(support, pendingChangeId, credit.CreditNr);

                support.MoveForwardNDays(daysUntilNextRebind);

                CompleteChangeTerms(support, credit.CreditNr, false);
            });
        }

        [Test]
        public void TestHappyFlow2()
        {
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var daysUntilNextRebind = 20;
                var newTerms = new MlNewChangeTerms
                {
                    NewFixedMonthsCount = 24,
                    NewMarginInterestRatePercent = 0.40m,
                    NewReferenceInterestRatePercent = 2.75m,
                    NewInterestBoundFrom = support.Clock.Today.AddDays(daysUntilNextRebind)
                };

                var loanAmount = 1300000m;
                var pendingChangeId = 1;
                var credit = CreditsMlStandard.CreateCredit(
                         support: support,
                         creditNumber: 2,
                         loanAmount: loanAmount,
                         referenceInterestRatePercent: newTerms.NewReferenceInterestRatePercent,
                         interestRebindMounthCount: newTerms.NewFixedMonthsCount);

                ComputeNewTerms(support, credit, loanAmount, newTerms);
                StartChangeTerms(support, credit, newTerms);
                ScheduleChangeTerms(support, pendingChangeId, credit.CreditNr);

                support.MoveForwardNDays(daysUntilNextRebind);

                CompleteChangeTerms(support, credit.CreditNr, false);
            });
        }

        [Test]
        public void TestTooHighMarginInterestRate()
        {
            const decimal maxMarginInterestRate = 99m;
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var loanAmount = 1640540m;
                var referenceInterestRatePercent = 2.75m;
                var newInterestBoundUntilMonths = 24;
                var newMarginInterest = 100m;
                var credit = CreditsMlStandard.CreateCredit(
                         support: support,
                         creditNumber: 3,
                         loanAmount: loanAmount,
                         referenceInterestRatePercent: referenceInterestRatePercent);

                TooHighMarginInterestRate(support, credit, loanAmount, referenceInterestRatePercent, newInterestBoundUntilMonths, newMarginInterest);
            }, overrideCreditSettings: envSettings =>
            {
                envSettings.Setup(x => x.MinAndMaxAllowedMarginInterestRate).Returns(Tuple.Create((decimal?)null, (decimal?)maxMarginInterestRate));
            });
        }

        [Test]
        public void TestCreditWithDefaultChangeTerms()
        {
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var loanAmount = 2000000m;
                var referenceInterestRatePercent = 3.5m;
                var credit = CreditsMlStandard.CreateCredit(
                             support: support,
                             creditNumber: 3,
                             loanAmount: loanAmount,
                             referenceInterestRatePercent: referenceInterestRatePercent,
                             interestRebindMounthCount: 3);

                support.MoveForwardNDays(3 * 31);

                CompleteChangeTerms(support, credit.CreditNr, true);
            });
        }

        [Test]
        public void TestChangeToNegativeMarginInterest()
        {
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var daysUntilNextRebind = 12;
                var newTerms = new MlNewChangeTerms
                {
                    NewFixedMonthsCount = 12,
                    NewMarginInterestRatePercent = -3m,
                    NewReferenceInterestRatePercent = 3.75m,
                    NewInterestBoundFrom = support.Clock.Today.AddDays(daysUntilNextRebind)
                };

                var loanAmount = 1900000m;
                var pendingChangeId = 1;
                var credit = CreditsMlStandard.CreateCredit(
                         support: support,
                         creditNumber: 1,
                         loanAmount: loanAmount,
                         referenceInterestRatePercent: newTerms.NewReferenceInterestRatePercent,
                         interestRebindMounthCount: newTerms.NewFixedMonthsCount);

                ComputeNewTerms(support, credit, loanAmount, newTerms);
                StartChangeTerms(support, credit, newTerms);
                ScheduleChangeTerms(support, pendingChangeId, credit.CreditNr);

                support.MoveForwardNDays(daysUntilNextRebind);

                CompleteChangeTerms(support, credit.CreditNr, false);
            });
        }

        [Test]
        public void TestChangeToNegativeMarginInterest_WouldMakeTotalNegative()
        {
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var daysUntilNextRebind = 20;
                var newTerms = new MlNewChangeTerms
                {
                    NewFixedMonthsCount = 12,
                    NewMarginInterestRatePercent = -3m,
                    NewReferenceInterestRatePercent = 2.75m,
                    NewInterestBoundFrom = support.Clock.Today.AddDays(daysUntilNextRebind)
                };

                var loanAmount = 1900000m;
                var pendingChangeId = 1;
                var credit = CreditsMlStandard.CreateCredit(
                         support: support,
                         creditNumber: 1,
                         loanAmount: loanAmount,
                         referenceInterestRatePercent: newTerms.NewReferenceInterestRatePercent,
                         interestRebindMounthCount: newTerms.NewFixedMonthsCount);

                ComputeNewTerms(support, credit, loanAmount, newTerms);
                StartChangeTerms(support, credit, newTerms);
                ScheduleChangeTerms(support, pendingChangeId, credit.CreditNr);

                support.MoveForwardNDays(daysUntilNextRebind);

                CompleteChangeTerms(support, credit.CreditNr, false);
            });
        }

        [Test]
        public void TestReferenceInterestDoesNotExistDefaultsToThreeMonths()
        {
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var daysUntilNextRebind = 30;
                var newTerms = new MlNewChangeTerms
                {
                    NewFixedMonthsCount = 13,
                    NewMarginInterestRatePercent = 0.40m,
                    NewReferenceInterestRatePercent = 2.75m,
                    NewInterestBoundFrom = support.Clock.Today.AddDays(daysUntilNextRebind)
                };

                var loanAmount = 1900000m;
                var pendingChangeId = 1;
                var credit = CreditsMlStandard.CreateCredit(
                         support: support,
                         creditNumber: 1,
                         loanAmount: loanAmount,
                         referenceInterestRatePercent: newTerms.NewReferenceInterestRatePercent,
                         interestRebindMounthCount: newTerms.NewFixedMonthsCount);

                ComputeNewTerms(support, credit, loanAmount, newTerms);
                StartChangeTerms(support, credit, newTerms);
                ScheduleChangeTerms(support, pendingChangeId, credit.CreditNr);

                support.MoveForwardNDays(daysUntilNextRebind);

                CompleteChangeTerms(support, credit.CreditNr, false);
            });
        }

        [Test]
        public void TestTermChangeForThreeMonthPreventsNegativeTotalInterest()
        {
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                support.MoveToNextDayOfMonth(1);

                CreditsMlStandard.SetupFixedInterestRates(support, new Dictionary<int, decimal> { { 3, 2.5m } });

                var loanAmount = 1900000m;
                var credit = CreditsMlStandard.CreateCredit(
                         support: support,
                         creditNumber: 1,
                         loanAmount: loanAmount,
                         referenceInterestRatePercent: 2m,
                         marginInterestRatePercent: -2.5m,
                         interestRebindMounthCount: 3);

                decimal GetMarginInterstRate()
                {
                    using (var context = support.CreateCreditContextFactory().CreateContext())
                    {
                        return CreditDomainModel.PreFetchForSingleCredit(credit!.CreditNr, context, support.CreditEnvSettings)
                            .GetDatedCreditValue(support.Clock.Today, nCredit.DatedCreditValueCode.MarginInterestRate);
                    }
                }

                //Actual rate constrained by the reference interest rate so the total cannot go negative
                Assert.That(GetMarginInterstRate(), Is.EqualTo(-2m));

                CreditsMlStandard.RunOneMonth(support);

                CreditsMlStandard.SetupFixedInterestRates(support, new Dictionary<int, decimal> { { 3, 3.5m } });

                CreditsMlStandard.RunOneMonth(support);
                CreditsMlStandard.RunOneMonth(support);
                CreditsMlStandard.RunOneMonth(support);

                //The next rebind should now allow the margin to go down since it's no longer constrained by the reference interest rate
                Assert.That(GetMarginInterstRate(), Is.EqualTo(-2.5m));
            });
        }
    }
}
