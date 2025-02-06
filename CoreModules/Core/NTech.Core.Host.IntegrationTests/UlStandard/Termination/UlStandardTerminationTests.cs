using NTech.Banking.Conversion;

namespace NTech.Core.Host.IntegrationTests.UlStandard.Termination
{
    public class UlStandardTerminationTests
    {
        public enum TestVariationCode
        {
            NoPaymentsStandardCase,
            DebtCollectionExportAfterSuspensionWithoutNewTerminationLetter,
            InactivatedTerminationLetterPreventsDebtCollection,
            TerminationLetterPreventedByPostpone,
            TerminationLetterNonOverDuePreventNewOne,
            PartialNotificationPaymentDoesNotPreventTerminatonLetter,
            FullNotificationPaymentPreventsTerminationLetter,
            OldestNotificationOnlyPaymentDoesNotPreventDebtCollection,
            TerminationLetterOverDueNotificationPaymentDoesPreventDebtCollection,
            AllOverdueNotificationsPaymentPreventsDebtCollection
        }

        [Test]
        public void TestVariation([ValueSource(nameof(GetVariations))] TestVariationCode variation)
        {
            UlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var t = new UlStandardTerminationTester(variation, support);
                t.RunTest();
            });
        }

        private static IEnumerable<TestVariationCode> GetVariations()
        {
            return Enums.GetAllValues<TestVariationCode>();
        }
    }
}
