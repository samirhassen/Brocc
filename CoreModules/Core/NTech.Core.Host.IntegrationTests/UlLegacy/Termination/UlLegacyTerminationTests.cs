using NTech.Banking.Conversion;

namespace NTech.Core.Host.IntegrationTests.UlLegacy.Termination
{
    public class UlLegacyTerminationTests
    {
        public enum TestVariationCode
        {
            NoPaymentsStandardCase,
            DebtCollectionPreventedByNewNotification,
            TerminationLetterPreventedByPostpone,
            TerminationLetterNonOverDuePreventNewOne,
            PartialNotificationPaymentDoesNotPreventTerminatonLetter,
            FullNotificationPaymentPreventsTerminationLetter,
            OldestNotificationOnlyPaymentDoesNotPreventDebtCollection,
            TerminationLetterOverDueNotificationPaymentDoesNotPreventDebtCollection,
            AllOverdueNotificationsPaymentDoesPreventDebtCollection
        }

        [Test]
        public void TestVariation([ValueSource(nameof(GetVariations))] TestVariationCode variation)
        {
            UlLegacyTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var t = new UlLegacyTerminationTester(variation, support);
                t.RunTest();
            });
        }

        private static IEnumerable<TestVariationCode> GetVariations() => Enums.GetAllValues<TestVariationCode>();
    }
}
