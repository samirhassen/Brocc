using Autofac;
using Newtonsoft.Json;
using nPreCredit.Controllers;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.Eventing;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace nPreCredit.Code.AffiliateReporting
{
    public class AffiliateReportingEventSubscriber : IEventSubscriber
    {
        public AffiliateReportingEventSubscriber()
        {

        }

        private ConcurrentQueue<string> subscriberIds = new ConcurrentQueue<string>();
        private static Lazy<Clients.nPreCreditClient> preCreditClient = new Lazy<Clients.nPreCreditClient>(() =>
        {
            var unp = NEnv.ApplicationAutomationUsernameAndPassword;
            var token = NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, unp.Item1, unp.Item2);
            return new Clients.nPreCreditClient(token.GetToken);
        });

        public void OnApplicationRejected(string eventData, CancellationToken t, ILifetimeScope scope)
        {
            var data = JsonConvert.DeserializeAnonymousType(eventData, new { applicationNr = (string)null, providerName = (string)null });

            if (string.IsNullOrWhiteSpace(data.applicationNr))
                return;

            var state = GetApplicationState(data.applicationNr);

            string[] rejectionReasons;
            if (state?.afterCreditCheckRejectionReason != null)
                rejectionReasons = new string[] { state.afterCreditCheckRejectionReason };
            else if (state?.rejectionReasons != null && state.rejectionReasons.Length > 0)
                rejectionReasons = state.rejectionReasons;
            else
                return;            

            var s = scope.Resolve<IAffiliateReportingService>();

            s.AddCreditApplicationRejectedEvent(new CreditApplicationRejectedEventModel
            {
                ApplicationNr = data.applicationNr,
                ProviderApplicationId = state.providerApplicationId,
                ProviderName = state.providerName,
                RejectionReasons = rejectionReasons?.ToList()
            });
        }

        public void OnCreditApplicationCreditCheckAccepted(string eventData, CancellationToken t, ILifetimeScope scope)
        {
            var data = JsonConvert.DeserializeAnonymousType(eventData, new { applicationNr = (string)null, providerName = (string)null });
            if (string.IsNullOrWhiteSpace(data.applicationNr))
                return;

            var state = GetApplicationState(data.applicationNr);
            if (state == null || !state.isCreditDecisionAccepted || (state.offer == null && state.additionalLoanOffer == null))
                return;

            var s = scope.Resolve<IAffiliateReportingService>();

            var a = state.additionalLoanOffer;
            var n = state.offer;
            s.AddCreditDecisionApprovedEvent(new CreditDecisionApprovedEventModel
            {
                ApplicationNr = data.applicationNr,
                ApplicationUrl = state.applicationWrapperUrl,
                ProviderApplicationId = state.providerApplicationId,
                ProviderName = state.providerName,
                AdditionalLoanOffer = a == null ? null : new CreditDecisionApprovedEventModel.AdditionalLoanOfferModel
                {
                    Amount = a.amount,
                    CreditNr = a.creditNr,
                    NewAnnuityAmount = a.newAnnuityAmount,
                    NewMarginInterestRatePercent = a.newMarginInterestRatePercent,
                    LoanStateAfter = new CreditDecisionApprovedEventModel.AdditionalLoanOfferModel.LoanStateModel
                    {
                        AnnuityAmount = a.loanStateAfter.annuityAmount,
                        NotificationFeeAmount = a.loanStateAfter.notificationFeeAmount,
                        Balance = a.loanStateAfter.balance,
                        EffectiveInterestRatePercent = a.loanStateAfter.effectiveInterestRatePercent,
                        MarginInterestRatePercent = a.loanStateAfter.marginInterestRatePercent,
                        ReferenceInterestRatePercent = a.loanStateAfter.referenceInterestRatePercent,
                        RepaymentTimeInMonths = a.loanStateAfter.repaymentTimeInMonths
                    }
                },
                NewLoanOffer = n == null ? null : new CreditDecisionApprovedEventModel.NewLoanOfferModel
                {
                    Amount = n.amount,
                    AnnuityAmount = n.annuityAmount,
                    InitialFeeAmount = n.initialFeeAmount,
                    InitialPaidToCustomerAmount = n.initialPaidToCustomerAmount,
                    NotificationFeeAmount = n.notificationFeeAmount,
                    TotalPaidAmount = n.totalPaidAmount,
                    EffectiveInterestRatePercent = n.effectiveInterestRatePercent,
                    MarginInterestRatePercent = n.marginInterestRatePercent,
                    ReferenceInterestRatePercent = n.referenceInterestRatePercent,
                    RepaymentTimeInMonths = n.repaymentTimeInMonths
                }
            });
        }

        public void OnSignedAgreementAdded(string eventData, CancellationToken ct, ILifetimeScope scope)
        {
            var data = JsonConvert.DeserializeAnonymousType(eventData, new
            {
                applicationNr = (string)null,
                applicantNr = (int?)null,
                allApplicantsHaveNowSigned = (bool?)null,
                providerName = (string)null
            });

            if (data.applicationNr == null || !data.applicantNr.HasValue || !data.allApplicantsHaveNowSigned.HasValue)
                return;

            var state = GetApplicationState(data.applicationNr);
            if (state == null || !state.isCreditDecisionAccepted)
                return;

            var s = scope.Resolve<IAffiliateReportingService>();

            s.AddCreditApplicationSignedAgreementEvent(new CreditApplicationSignedAgreementEventModel
            {
                ApplicationNr = data.applicationNr,
                ApplicantNr = data.applicantNr.Value,
                AllApplicantsHaveNowSigned = data.allApplicantsHaveNowSigned.Value,
                ProviderApplicationId = state.providerApplicationId,
                ProviderName = state.providerName
            });
        }

        public void OnCreditApplicationExternalProviderEvent(string eventData, CancellationToken ct, ILifetimeScope scope)
        {
            var data = JsonConvert.DeserializeAnonymousType(eventData, new
            {
                applicationNr = (string)null,
                providerName = (string)null,
                eventName = (string)null,
                disableAutomation = (bool?)null
            });

            if (data.applicationNr == null || data.eventName == null)
                return;
            var eventName = data.eventName;
            Action<string, string> createComment = (x, y) => preCreditClient.Value.AddCommentToApplication(data.applicationNr, x, null, null, y);

            if (eventName == "CancelledOtherOffer" || eventName == "CancelledCustomerDeclined" || eventName == "CancelledGeneric")
            {
                preCreditClient.Value.CancelApplication(data.applicationNr, new CancelledByExternalRequestModel
                {
                    WasCancelledByExternal = true,
                    CancelStatusCode = eventName
                });
            }
            else if (eventName == "customerAcceptedOffer" || eventName == "accepted-offer")
            {
                createComment($"Provider reported event: the customer accepted our offer", "ProviderReportedCustomerAcceptedOffer");
            }
            else if (eventName == "customerRejectedOffer" || eventName == "rejected-offer")
            {
                createComment($"Provider reported event: the customer rejected our offer", "ProviderReportedCustomerRejectedOffer");
            }
            else
            {
                createComment($"Provider reported event: {eventName}", "ProviderReportedEvent");
            }
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
            subscriberIds.Enqueue(subscribe("CreditApplicationRejected", (d, ct) =>
                DependancyInjection.WithAffiliateReportingBackgroundSeviceScope(scope =>
                    OnApplicationRejected(d, ct, scope)
                )));

            subscriberIds.Enqueue(subscribe("CreditApplicationCreditCheckAccepted", (d, ct) =>
                DependancyInjection.WithAffiliateReportingBackgroundSeviceScope(scope =>
                    OnCreditApplicationCreditCheckAccepted(d, ct, scope)
                )));

            subscriberIds.Enqueue(subscribe("SignedAgreementAdded", (d, ct) =>
                DependancyInjection.WithAffiliateReportingBackgroundSeviceScope(scope =>
                    OnSignedAgreementAdded(d, ct, scope)
                )));

            subscriberIds.Enqueue(subscribe("CreditApplicationExternalProviderEvent", (d, ct) =>
                DependancyInjection.WithAffiliateReportingBackgroundSeviceScope(scope =>
                    OnCreditApplicationExternalProviderEvent(d, ct, scope)
                )));
        }

        private AffiliateReporting.ApplicationStateModel GetApplicationState(string applicationNr)
        {
            return preCreditClient.Value.GetApplicationState(applicationNr);
        }
    }
}