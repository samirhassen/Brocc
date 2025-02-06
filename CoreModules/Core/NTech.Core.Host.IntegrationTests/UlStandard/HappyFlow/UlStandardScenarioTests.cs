using NTech.Core.Host.IntegrationTests.UlStandard;
using NTech.Core.Module.Shared.Services;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlStandardLikeScenarioTests
    {
        [Test]
        public void TestHappyFlow1()
        {
            UlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                support.AssertDayOfMonth(5);

                AddAndCheckTestPersonOne(support);
                AddAndScoreApplicationOnTestPersonOne(support);
                AnswerKycQuestionsOnApplicationOne(support);

                AddCredit_MissingOutgoingPaymentAccount(support);
                AddCredit(support, 1, OutgoingAccountOverride.UseStandardOutgoingAccount, DirectDebitCode.WithDirectDebit);
                SetAndRemoveSuperLowReferenceInterestRate(support, 1);
                var unplacedPaymentsEvent = AddUnplacedManualPayments(support);
                RepayUnplacedPayment(support, unplacedPaymentsEvent);
                RepayUnplacedPayment_OverrideSourceAccountWithSetting(support, unplacedPaymentsEvent);
                CanDisableOutgoingPaymentSourceAccountSetting(support);

                AddCredit(support, 2, OutgoingAccountOverride.UseStandardOutgoingAccount, DirectDebitCode.WithoutDirectDebit);

                support.MoveToNextDayOfMonth(14);

                NotifyCredit(support, 1, ScenarioName.Normal);
                NotifyCredit(support, 2, ScenarioName.OverrideIncomingAccountButNotDirectDebitAccount);
                NotifyCredit(support, 2, ScenarioName.OverrideIncomingAccountAndDirectDebitAccount);

                SettleCreditTwo(support);

                support.AssertDayOfMonth(16);
                support.MoveToNextDayOfMonth(20);

                DirectDebitDelivery(support);

                support.AssertDayOfMonth(20);

                F818_QuarterlyReport(support);
            });
        }
    }
}