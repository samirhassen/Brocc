using nCredit.Code.Services;
using Newtonsoft.Json;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.Eventing;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace nCredit.Code.AffiliateReporting
{
    public class AffiliateReportingEventSubscriber : IEventSubscriber
    {
        public AffiliateReportingEventSubscriber()
        {

        }

        private ConcurrentQueue<string> subscriberIds = new ConcurrentQueue<string>();
        private static Lazy<NTechSelfRefreshingBearerToken> User = new Lazy<NTechSelfRefreshingBearerToken>(() =>
        {
            var credentials = NEnv.ApplicationAutomationUsernameAndPassword;
            return NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, credentials.Item1, credentials.Item2);
        });

        public void OnOutgoingCreditPaymentFileCreated(string eventData, CancellationToken t)
        {
            var data = JsonConvert.DeserializeAnonymousType(eventData, new { outgoingPaymentFileHeaderId = 0 });

            if (!NEnv.ServiceRegistry.ContainsService("nPreCredit"))
                return;
                        
            var s = new OutgoingPaymentsService(new NTech.Core.Credit.Shared.Database.CreditContextFactory(() => new CreditContextExtended(User.Value.GetUserMetadata(), CoreClock.SharedInstance)));
            var payments = s
                .FetchPayments(data.outgoingPaymentFileHeaderId)
                .Where(x => x.EventTypePaymentSource.IsOneOf(BusinessEventType.NewAdditionalLoan.ToString(), BusinessEventType.NewCredit.ToString()))
                .Select(x => new PreCreditClient.LoanPaidOutEventModel
                {
                    ApplicationNr = x.ApplicationNr,
                    PaymentAmount = x.PaidToCustomerAmount,
                    ProviderApplicationId = x.ProviderApplicationId,
                    CreditNr = x.CreditNr,
                    PaymentDate = x.TransactionDate,
                    ProviderName = x.CreditProviderName
                })
                .ToList();

            if (payments.Count == 0)
                return;

            var client = new PreCreditClient();
            
            client.AddAffiliateReportingLoanPaidOutEvents(payments, token: User.Value);
        }

        public void OnShutdown(Action<string> unsubscribe)
        {
            string subscriberId;
            while (subscriberIds.TryDequeue(out subscriberId))
            {
                unsubscribe(subscriberId);
            }
        }

        public void OnStartup(Func<string, Action<string, CancellationToken>, string> subscribe)
        {
            subscriberIds.Enqueue(subscribe("OutgoingCreditPaymentFileCreated", OnOutgoingCreditPaymentFileCreated));
        }
    }
}
