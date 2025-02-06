using Moq;
using nCredit.Code;
using nCredit.DbModel.BusinessEvents;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.UlStandard;
using NTech.Core.Host.IntegrationTests.UlStandard.Utilities;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlStandardLikeScenarioTests
    {
        private void SettleCreditTwo(UlStandardTestRunner.TestSupport support)
        {
            support.AssertDayOfMonth(14);
            support.MoveToNextDayOfMonth(15);

            //Settle credit 2 over 2 days
            var rseService = new SwedishMortgageLoanRseService(support.CreateCreditContextFactory(), support.GetNotificationProcessSettingsFactory(),
                support.Clock, support.ClientConfiguration, support.CreditEnvSettings);
            var settlementSuggestionMgr = support.GetRequiredService<CreditSettlementSuggestionBusinessEventManager>();
            const int SettlementDay = 16;            
            var settlementData = Credits.CreateSettlementOffer(support, CreditsUlStandard.GetCreateCredit(support, 2).CreditNr, Month.ContainingDate(support.Clock.Today).GetDayDate(SettlementDay), null, null);

            support.MoveToNextDayOfMonth(SettlementDay);

            var unplacedPaymentEvent = Credits.AddUnplacedPayments(support, support.CreditEnvSettings,
                (Amount: settlementData.settlementAmount, NoteText: "SettleCredit2"));
            int paymentId;
            using (var context = support.CreateCreditContextFactory().CreateContext())
            {
                paymentId = context.IncomingPaymentHeadersQueryable.Single(x => x.Transactions.Any(y => y.BusinessEventId == unplacedPaymentEvent.Id)).Id;
            }

            var secureMessage = new Mock<ISecureMessageService>();
            var credit = CreditsUlStandard.GetCreateCredit(support, 2);
            Credits.PlaceUnplacedPaymentUsingSuggestion(support, paymentId, credit.CreditNr);
            Credits.AssertIsSettled(support, credit.CreditNr);            

        }
    }
}