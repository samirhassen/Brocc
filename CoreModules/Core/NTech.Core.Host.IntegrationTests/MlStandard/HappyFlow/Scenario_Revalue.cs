using NTech.Core.Credit.Shared.Models;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.MlStandard.Utilities;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class MlStandardLikeScenarioTests
    {
        private void RevalueLoan(MlStandardTestRunner.TestSupport support)
        {
            DoAfterDay(support, 5, () =>
            {
                var credit = CreditsMlStandard.GetCreatedCredit(support, 1);

                var collateralService = new MortgageLoanCollateralService(support.CurrentUser, support.Clock, support.ClientConfiguration);
                var service = new MlStandardSeRevaluationService(
                    support.CurrentUser,
                    support.Clock,
                    support.ClientConfiguration,
                    collateralService,
                    support.CreateCreditContextFactory());

                SwedishMortgageLoanAmortizationBasisModel GetBasis()
                {
                    using (var context = support.CreateCreditContextFactory().CreateContext())
                    {
                        return collateralService!.GetSeMortageLoanAmortizationBasis(context, new GetSeAmortizationBasisRequest
                        {
                            CreditNr = credit!.CreditNr,
                            UseUpdatedBalance = false
                        }).AmortizationBasis;
                    }
                }

                var basisBefore = GetBasis();

                var newBasis = service.CalculateRevaluate(
                    credit.CreditNr,
                    basisBefore.CurrentCombinedYearlyIncomeAmount + 1,
                    basisBefore.OtherMortageLoansAmount,
                    (Amount: basisBefore.ObjectValue, Date: basisBefore.ObjectValueDate), false, out bool _).NewBasis;

                service.CommitRevaluate(new MlStandardSeRevaluationCommitRequest
                {
                    NewBasis = newBasis
                });

                var basisAfter = GetBasis();

                //Just check that anything at all got saved.
                Assert.That(basisAfter.CurrentCombinedYearlyIncomeAmount, Is.EqualTo(basisBefore.CurrentCombinedYearlyIncomeAmount + 1));
            });
        }
    }
}