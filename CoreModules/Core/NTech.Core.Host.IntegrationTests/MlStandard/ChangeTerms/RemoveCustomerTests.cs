using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.MlStandard.Utilities;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests.MlStandard.ChangeTerms
{
    public class RemoveCustomerTests
    {
        [Test]
        public void RemoveCustomerHappyFlow() => MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support => Test_RemoveCustomerHappyFlow(support));

        private void Test_RemoveCustomerHappyFlow(MlStandardTestRunner.TestSupport support)
        {
            support.MoveToNextDayOfMonth(1);
            
            foreach(var monthNr in Enumerable.Range(1, 5))
            {
                CreditsMlStandard.RunOneMonth(support,
                    beforeDay: dayNr =>
                    {
                        if (dayNr == 1)
                            DebugPrinter.PrintMonthStart(support);

                        if (dayNr == 3 && monthNr == 1)
                            CreditsMlStandard.CreateCredit(support, 1);

                        if(dayNr == 10 && monthNr == 3)
                        {
                            using(var context = support.CreateCreditContextFactory().CreateContext())
                            {
                                var credit = CreditsMlStandard.GetCreatedCredit(support, 1);
                                var customerIdToRemove = credit.CreditCustomers.Single(x => x.ApplicantNr == 1).CustomerId;
                                var removeService = new ChangeCreditCustomersService(support.CreateCreditContextFactory());
                                removeService.RemoveCreditCustomer(credit.CreditNr, customerIdToRemove, context);
                                context.SaveChanges();
                            }
                        }
                    },
                    afterDay: dayNr =>
                    {
                        DebugPrinter.PrintDay(support, support.Clock.Today);
                        if (dayNr == Month.ContainingDate(support.Clock.Today).LastDate.Day) DebugPrinter.PrintMonthEnd();
                    });
            }

            using(var context = support.CreateCreditContextFactory().CreateContext())
            {
                Assert.That(context.CreditCustomersQueryable.Count(), Is.EqualTo(1));
            }
        }
    }
}
