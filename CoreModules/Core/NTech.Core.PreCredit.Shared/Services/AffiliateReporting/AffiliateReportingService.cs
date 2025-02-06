using Newtonsoft.Json;
using nPreCredit.DbModel;
using NTech.Banking.LoanModel;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Core.PreCredit.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.AffiliateReporting
{
    public class AffiliateReportingService : IAffiliateReportingService
    {
        private readonly ICoreClock clock;
        private readonly Func<IPreCreditContextExtended> createNonHttpContextConnection;
        private readonly EncryptionService encryptionService;
        private readonly IPreCreditEnvSettings envSettings;
        private readonly ICreditClient creditClient;

        public AffiliateReportingService(ICoreClock clock,
            /*
             BEWARE: This runs in the background with no http context user so dont just switch this out for
                     IPreCreditContextFactoryService and inject it or provider callbacks will break
             */
            Func<IPreCreditContextExtended> createNonHttpContextConnection,
            EncryptionService encryptionService, IPreCreditEnvSettings envSettings, ICreditClient creditClient)
        {
            this.clock = clock;
            this.createNonHttpContextConnection = createNonHttpContextConnection;
            this.encryptionService = encryptionService;
            this.envSettings = envSettings;
            this.creditClient = creditClient;
        }

        public long AddCreditDecisionApprovedEvent(CreditDecisionApprovedEventModel model)
        {
            return AddEvent(CreditDecisionApprovedEventModel.EventTypeName, model);
        }

        public long AddCreditApplicationRejectedEvent(CreditApplicationRejectedEventModel model)
        {
            return AddEvent(CreditApplicationRejectedEventModel.EventTypeName, model);
        }

        public List<long> AddLoanPaidOutEventModels(List<LoanPaidOutEventModel> models)
        {
            return AddEvents(LoanPaidOutEventModel.EventTypeName, models);
        }

        public List<long> AddCreditApplicationCancelledEvents(List<CreditApplicationCancelledEventModel> models)
        {
            return AddEvents(CreditApplicationCancelledEventModel.EventTypeName, models);
        }

        public static List<AffiliateReportingEvent> AddCreditApplicationCancelledEventsComposable(List<CreditApplicationCancelledEventModel> models, IPreCreditContextExtended context, DateTime now)
        {
            return AddEventsComposable(CreditApplicationCancelledEventModel.EventTypeName, models, context, now);
        }

        public long AddLoanPaidOutEventModel(LoanPaidOutEventModel model)
        {
            return AddEvent(LoanPaidOutEventModel.EventTypeName, model);
        }

        public long AddCreditApplicationSignedAgreementEvent(CreditApplicationSignedAgreementEventModel model)
        {
            return AddEvent(CreditApplicationSignedAgreementEventModel.EventTypeName, model);
        }

        public List<AffiliateReportingEventModel> GetAffiliateReportingEventsForApplication(string applicationNr)
        {
            using (var context = createNonHttpContextConnection())
            {
                var events = context
                    .AffiliateReportingEventsQueryable
                    .Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => new AffiliateReportingEventModel
                    {
                        Id = x.Id,
                        ApplicationNr = x.ApplicationNr,
                        CreationDate = x.CreationDate,
                        EventData = x.EventData,
                        EventType = x.EventType,
                        ProcessedDate = x.ProcessedDate,
                        ProcessedStatus = x.ProcessedStatus,
                    })
                    .OrderByDescending(y => y.Id)
                    .ToList();

                var eventIds = events.Select(x => x.Id).ToList();

                var items = context
                            .AffiliateReportingLogItemsQueryable
                            .Where(y => eventIds.Contains(y.IncomingApplicationEventId))
                            .Select(y => new
                            {
                                y.IncomingApplicationEventId,
                                y.Id,
                                I = new AffiliateReportingEventModel.ItemModel
                                {
                                    LogDate = y.LogDate,
                                    ExceptionText = y.ExceptionText,
                                    MessageText = y.MessageText,
                                    ProcessedStatus = y.ProcessedStatus,
                                    OutgoingRequestBody = y.OutgoingRequestBody,
                                    OutgoingResponseBody = y.OutgoingResponseBody
                                }
                            })
                            .ToList()
                            .GroupBy(y => y.IncomingApplicationEventId)
                            .ToDictionary(y => y.Key, x => x.OrderByDescending(y => y.Id).Select(y => y.I).ToList());

                foreach (var e in events)
                {
                    e.Items = items.Opt(e.Id);
                }

                return events;
            }
        }

        private long AddEvent<T>(string eventType, T model) where T : AffiliateReportingEventModelBase
        {
            return AddEvents(eventType, new[] { model }).Single();
        }

        private List<long> AddEvents<T>(string eventType, IEnumerable<T> models) where T : AffiliateReportingEventModelBase
        {
            var now = DateTime.Now;
            using (var context = createNonHttpContextConnection())
            {
                var events = AddEventsComposable(eventType, models, context, now);

                context.SaveChanges();

                return events.Select(x => x.Id).ToList();
            }
        }

        private static List<AffiliateReportingEvent> AddEventsComposable<T>(string eventType, IEnumerable<T> models, IPreCreditContextExtended context, DateTime now) where T : AffiliateReportingEventModelBase
        {
            var events = new List<AffiliateReportingEvent>();

            foreach (var model in models)
            {
                var evt = new AffiliateReportingEvent
                {
                    EventType = eventType,
                    EventData = JsonConvert.SerializeObject(model),
                    ApplicationNr = model.ApplicationNr,
                    ProviderName = model.ProviderName,
                    CreationDate = now,
                    WaitUntilDate = now,
                    DeleteAfterDate = now.AddDays(30),
                    ProcessedStatus = AffiliateReportingEventResultCode.Pending.ToString()
                };
                events.Add(evt);
            }

            context.AddAffiliateReportingEvents(events.ToArray());

            return events;
        }

        public bool TryResetEventToPending(long affiliateReportingEventId)
        {
            using (var context = createNonHttpContextConnection())
            {
                var e = context.AffiliateReportingEventsQueryable.SingleOrDefault(x => x.Id == affiliateReportingEventId);
                if (e == null)
                    return false;
                e.ProcessedStatus = AffiliateReportingEventResultCode.Pending.ToString();
                e.ProcessedDate = null;
                context.SaveChanges();
                return true;
            }
        }

        public ApplicationStateModel GetCurrentApplicationState(string applicationNr, INTechCurrentUserMetadata ntechCurrentUser)
        {
            var now = clock.Now;

            using (var context = createNonHttpContextConnection())
            {
                var itemNamesToFetch = new List<string> { "providerApplicationId", "documentCheckStatus" };

                var result = context
                    .CreditApplicationHeadersQueryable
                    .Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => new
                    {
                        x.ApplicationNr,
                        x.ProviderName,
                        x.CreditCheckStatus,
                        x.CurrentCreditDecision,
                        Items = x.Items.Where(y => itemNamesToFetch.Contains(y.Name)),
                        x.IsRejected,
                        x.CustomerCheckStatus,
                        x.FraudCheckStatus
                    })
                    .FirstOrDefault();

                var response = new ApplicationStateModel();
                response.creditCheckStatus = result.CreditCheckStatus;

                if (result == null)
                {
                    return null;
                }

                var encryptedItemIds = result.Items.Where(x => x.IsEncrypted).Select(x => long.Parse(x.Value)).ToArray();
                var decryptedValueById = new Lazy<IDictionary<long, string>>(() =>
                    encryptionService.DecryptEncryptedValues(context, encryptedItemIds));

                string providerApplicationId = null;
                var providerApplicationIdItem = result.Items.FirstOrDefault(y => y.Name == "providerApplicationId");
                if (providerApplicationIdItem != null)
                {
                    var p = providerApplicationIdItem;
                    providerApplicationId = p.IsEncrypted ? decryptedValueById.Value[long.Parse(p.Value)] : p.Value;
                }
                response.providerApplicationId = providerApplicationId;
                response.providerName = result.ProviderName;

                var d = result.CurrentCreditDecision;

                var ad = d as AcceptedCreditDecision;
                var rd = d as RejectedCreditDecision;
                if (ad != null && result.CreditCheckStatus == "Accepted")
                {
                    var offer = CreditDecisionModelParser.ParseAcceptedNewCreditOffer(ad.AcceptedDecisionModel);
                    var additionalLoanOffer = CreditDecisionModelParser.ParseAcceptedAdditionalLoanOffer(ad.AcceptedDecisionModel);

                    response.isCreditDecisionAccepted = true;
                    response.offer = offer;

                    if (additionalLoanOffer != null)
                    {
                        var currentCredit = creditClient.GetCustomerCreditHistoryByCreditNrs(new List<string> { additionalLoanOffer.creditNr }).Single();
                        var balance = currentCredit.CapitalBalance + additionalLoanOffer.amount.Value;
                        var marginInterestRatePecent = (additionalLoanOffer.newMarginInterestRatePercent ?? currentCredit.MarginInterestRatePercent).Value;
                        var annuityAmount = (additionalLoanOffer.newAnnuityAmount ?? currentCredit.AnnuityAmount).Value;
                        var totalInterestRatePercent = marginInterestRatePecent + currentCredit.ReferenceInterestRatePercent.GetValueOrDefault();
                        var terms = PaymentPlanCalculation
                            .BeginCreateWithAnnuity(
                                balance,
                                annuityAmount,
                                totalInterestRatePercent, null, envSettings.CreditsUse360DayInterestYear)
                            .WithMonthlyFee(currentCredit.NotificationFeeAmount.GetValueOrDefault())
                            .EndCreate();

                        response.additionalLoanOffer = new ApplicationStateModel.AdditionalLoanOfferModel
                        {
                            amount = additionalLoanOffer.amount,
                            creditNr = additionalLoanOffer.creditNr,
                            newAnnuityAmount = additionalLoanOffer.newAnnuityAmount,
                            newMarginInterestRatePercent = additionalLoanOffer.newMarginInterestRatePercent,
                            loanStateAfter = new ApplicationStateModel.AdditionalLoanStateAfterModel
                            {
                                balance = balance,
                                repaymentTimeInMonths = terms.Payments.Count,
                                annuityAmount = terms.AnnuityAmount,
                                notificationFeeAmount = currentCredit.NotificationFeeAmount.GetValueOrDefault(),
                                marginInterestRatePercent = marginInterestRatePecent,
                                referenceInterestRatePercent = currentCredit.ReferenceInterestRatePercent,
                                effectiveInterestRatePercent = terms.EffectiveInterestRatePercent
                            }
                        };
                    }

                    var applicationWrapperUrlPattern = envSettings.ApplicationWrapperUrlPattern;
                    if (applicationWrapperUrlPattern != null)
                    {
                        //Send a wrapper link instead of the raw link so it can be switched to always point to the current state of the application
                        var wrapperToken = AgreementSigningProviderHelper.GetOrCreateApplicationWrapperToken(context, now, applicationNr, 1, ntechCurrentUser.UserId, ntechCurrentUser.InformationMetadata);
                        context.SaveChanges();
                        response.applicationWrapperUrl = new Uri(applicationWrapperUrlPattern.Replace("{token}", wrapperToken.Token)).AbsoluteUri;
                    }
                }
                else if (rd != null && result.CreditCheckStatus == "Rejected")
                {
                    response.isCreditDecisionAccepted = false;
                    response.rejectionReasons = CreditDecisionModelParser.ParseRejectionReasons(rd.RejectedDecisionModel);
                }

                if(response.rejectionReasons == null && result.IsRejected)
                {
                    var isDocumentCheckRejected = result.Items.Any(x => x.Name == "documentCheckStatus" && x.Value == "Rejected");
                    if (result.FraudCheckStatus == "Rejected")
                        response.afterCreditCheckRejectionReason = "finalCheck"; //note: final since we dont want to leak these reasons
                    else if (result.CustomerCheckStatus == "Rejected")
                        response.afterCreditCheckRejectionReason = "finalCheck";
                    else if (isDocumentCheckRejected)
                        response.afterCreditCheckRejectionReason = "incomeVerification";
                    else
                        response.afterCreditCheckRejectionReason = "other"; //Should not happen
                }
                
                return response;
            }
        }
    }

    public interface IAffiliateReportingService
    {
        long AddCreditDecisionApprovedEvent(CreditDecisionApprovedEventModel model);

        long AddCreditApplicationRejectedEvent(CreditApplicationRejectedEventModel model);

        long AddCreditApplicationSignedAgreementEvent(CreditApplicationSignedAgreementEventModel model);

        List<long> AddCreditApplicationCancelledEvents(List<CreditApplicationCancelledEventModel> models);

        List<long> AddLoanPaidOutEventModels(List<LoanPaidOutEventModel> models);

        List<AffiliateReportingEventModel> GetAffiliateReportingEventsForApplication(string applicationNr);

        ApplicationStateModel GetCurrentApplicationState(string applicationNr, INTechCurrentUserMetadata ntechCurrentUser);

        bool TryResetEventToPending(long affiliateReportingEventId);
    }

    public class AffiliateReportingEventModel
    {
        public long Id { get; set; }
        public string ApplicationNr { get; set; }
        public DateTime CreationDate { get; set; }
        public string EventData { get; set; }
        public string EventType { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string ProcessedStatus { get; set; }
        public List<ItemModel> Items { get; set; }

        public class ItemModel
        {
            public DateTime LogDate { get; set; }
            public string ExceptionText { get; set; }
            public string MessageText { get; set; }
            public string ProcessedStatus { get; set; }
            public string OutgoingRequestBody { get; set; }
            public string OutgoingResponseBody { get; set; }
        }
    }
}