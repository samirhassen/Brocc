using nCredit;
using nCredit.DomainModel;
using NTech.Core.Credit.Database;
using NTech.Core.Host.IntegrationTests.MlStandard.Utilities;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class MlStandardLikeScenarioTests
    {
        private void AddLoan(MlStandardTestRunner.TestSupport support)
        {
            DoAfterDay(support, 5, () =>
            {
                CreditsMlStandard.SetupFixedInterestRates(support, new Dictionary<int, decimal>
                {
                    { 3, 0.2m },
                    { 4, 1m }
                });
                const string LoanOwnerName = "Self financed";
                var creditNr = CreditsMlStandard.CreateCredit(support, 1, loanOwnerName: LoanOwnerName).CreditNr;

                using (var context = new CreditContextExtended(support.CurrentUser, support.Clock))
                {
                    var credit = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, support.CreditEnvSettings);

                    Assert.That(
                        credit.GetDatedCreditValue(support.Clock.Today, DatedCreditValueCode.InitialRepaymentTimeInMonths),
                        Is.EqualTo(40m * 12m));
                    Assert.That(
                        credit.GetDatedCreditString(support.Clock.Today, DatedCreditStringCode.LoanOwner, null),
                        Is.EqualTo(LoanOwnerName));
                }
            });
        }
    }
}