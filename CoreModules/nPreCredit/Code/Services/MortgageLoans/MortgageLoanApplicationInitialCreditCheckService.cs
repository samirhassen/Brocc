using Newtonsoft.Json;
using NTech;
using NTech.Banking.ScoringEngine;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.PreCredit.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class MortgageLoanApplicationInitialCreditCheckService : MortgageLoanCreditCheckBase, IMortgageLoanApplicationInitialCreditCheckService
    {
        private IHttpContextUrlService urlService;
        private readonly IMortgageApplicationRejectionService rejectionService;
        private readonly ICreditClient creditClient;
        private readonly IMortgageLoanWorkflowService mortgageLoanWorkflowService;
        private readonly IPublishEventService publishEventService;
        private readonly INTechCurrentUserMetadata ntechCurrentUserMetadata;
        private readonly DocumentDatabase<ScoringProcess.OfferModel> offerDb;

        public MortgageLoanApplicationInitialCreditCheckService(
            INTechCurrentUserMetadata ntechCurrentUserMetadata,
            IHttpContextUrlService urlService,
            IPublishEventService publishEventService,
            IMortgageApplicationRejectionService rejectionService,
            IClock clock,
            ICreditClient creditClient,
            IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository,
            IMortgageLoanWorkflowService mortgageLoanWorkflowService,
            IKeyValueStoreService keyValueStoreService) : base(clock, partialCreditApplicationModelRepository)
        {
            this.urlService = urlService;
            this.rejectionService = rejectionService;
            this.creditClient = creditClient;
            this.mortgageLoanWorkflowService = mortgageLoanWorkflowService;
            this.publishEventService = publishEventService;
            this.ntechCurrentUserMetadata = ntechCurrentUserMetadata;
            this.offerDb = new DocumentDatabase<ScoringProcess.OfferModel>(KeyValueStoreKeySpaceCode.MortgageLoanOfferV1, keyValueStoreService);
        }

        public const string WorkflowStepName = "MortgageLoanInitialCreditCheck";

        public bool TryDoInitialScoring(string applicationNr, bool skipProviderCallback, out MortgageLoanInitialScoringResponse result, out string failedMessage)
        {
            using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, clock))
            {
                var app = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == applicationNr && x.MortgageLoanExtension != null)
                    .Select(x => new
                    {
                        x.NrOfApplicants,
                        HasCreditDecision = x.CurrentCreditDecisionId.HasValue
                    })
                    .SingleOrDefault();
                if (app == null)
                {
                    result = null;
                    failedMessage = "No such application";
                    return false;
                }

                if (app.HasCreditDecision)
                {
                    result = null;
                    failedMessage = "Inital scoring can only be done once";
                    return false;
                }
            }
            var m = NEnv.MortgageLoanScoringSetup;

            var scoringInput = new ScoringDataModel();

            var additionalApplicantFields = new HashSet<string>();
            var customerIdByApplicantNr = new Dictionary<int, int>();

            FillScoringInputFromApplicationModel(applicationNr, scoringInput,
                additionalApplicationFields: new HashSet<string> { "mortgageLoanCurrentLoanAmount", "mortgageLoanHouseValueAmount" },
                additionalApplicantFields: additionalApplicantFields,
                withAppModel: a =>
            {
                scoringInput.Set("loanAmount", a.Application.Get("mortgageLoanCurrentLoanAmount").DecimalValue.Optional, null);
                scoringInput.Set("objectValue", a.Application.Get("mortgageLoanHouseValueAmount").DecimalValue.Optional, null);

                a.DoForEachApplicant(applicantNr =>
                    customerIdByApplicantNr[applicantNr] = a.Applicant(applicantNr).Get("customerId").IntValue.Required);
            });

            FillScoringInputFromCreditHistory(scoringInput, customerIdByApplicantNr, creditClient);

            var f = new PluginScoringProcessFactory();

            var p = f.GetScoringProcess("MortgageLoanInitial");

            //TODO: Generalize
            IPluginScoringProcessDataSource datasource;
            if (NEnv.ClientCfg.ClientName == "bofink")
                datasource = new Bofink.BofinkScoringDataSource(new PluginScoringProcessModelWithInternalHistory
                {
                    ScoringData = scoringInput,
                    HistoricalApplications = new List<HistoricalApplication>(), //TODO: lookup
                    HistoricalCredits = new List<HistoricalCredit>() //TODO: lookup
                });
            else if (NEnv.ClientCfg.ClientName == "bluestepFi")
                datasource = new BluestepFi.BluestepFiScoringDataSource(new PluginScoringProcessModelWithInternalHistory
                {
                    ScoringData = scoringInput,
                    HistoricalApplications = new List<HistoricalApplication>(), //TODO: lookup
                    HistoricalCredits = new List<HistoricalCredit>() //TODO: lookup
                });
            else
                throw new NotImplementedException();

            var scoreResult = p.Score(applicationNr, datasource); //TODO: Logic controls bofink

            ServiceEvent evt;
            using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, clock))
            {
                failedMessage = null;
                if (scoreResult.WasAccepted)
                {
                    evt = AcceptNewLoan(context, applicationNr, skipProviderCallback, scoreResult, scoringInput);

                    result = new MortgageLoanInitialScoringResponse
                    {
                        AcceptedOffer = new MortgageLoanInitialScoringResponse.Offer
                        {
                            InitialFeeAmount = scoreResult.Offer.InitialFeeAmount,
                            LoanAmount = scoreResult.Offer.LoanAmount,
                            MonthlyAmortizationAmount = scoreResult.Offer.MonthlyAmortizationAmount.Value,
                            MonthlyFeeAmount = scoreResult.Offer.MonthlyFeeAmount,
                            NominalInterestRatePercent = scoreResult.Offer.NominalInterestRatePercent,
                            ValidUntilDate = clock.Today.AddDays(30) //TODO: Store this?
                        }
                    };
                }
                else
                {
                    result = new MortgageLoanInitialScoringResponse
                    {
                        RejectedDetails = new MortgageLoanInitialScoringResponse.RejectionDetails
                        {
                            RejectionReasons = scoreResult.RejectionRuleNames.Select(m.GetRejectionReasonNameByScoringRuleName).Distinct().ToList()
                        }
                    };
                    evt = Reject(context, applicationNr, result.RejectedDetails.RejectionReasons, scoreResult, scoringInput);
                }
                context.SaveChanges();
            }

            bool wasScoringEventPublished = false;
            Action sendScoringEvent = () =>
            {
                wasScoringEventPublished = true;
                //This is wrapped and pushed forward in time to prevent race conditions between this wevent and TryReject
                if (evt != null)
                    publishEventService.Publish(evt.Code, evt.Data);
            };

            if (!scoreResult.WasAccepted)
            {
                //Try to auto reject applications that ended up here since the user cant do anything to get it back anyway
                string fm;
                var wasRejected = this.rejectionService.TryReject(applicationNr, true, skipProviderCallback, out fm, beforePublishEvent: sendScoringEvent);
                if (!wasRejected)
                    Serilog.Log.Warning("Could not reject mortage loan application after rejected initial scoring: " + fm);
            }

            if (!wasScoringEventPublished)
                sendScoringEvent();

            return true;
        }

        private ServiceEvent AcceptNewLoan(
            PreCreditContextExtended context,
            string applicationNr,
            bool skipProviderCallback,
            PluginScoringProcessResult scoreResult,
            ScoringDataModel scoreInput)
        {
            bool wasAutomated = true; //TODO: Dynamic when moving this code
            CreditApplicationEventCode eventCode;

            var r = context.CreditApplicationHeaders.Where(x => x.ApplicationNr == applicationNr).Select(x => new
            {
                App = x,
                Ext = x.MortgageLoanExtension
            }).Single();

            var a = r.App;
            var m = r.Ext;

            var d = new AcceptedCreditDecision
            {
                ApplicationNr = applicationNr,
                CreditApplication = a,
                DecisionById = context.CurrentUserId,
                WasAutomated = wasAutomated,
                DecisionDate = context.Clock.Now,
                DecisionType = CreditDecisionTypeCode.Initial.ToString(),
                AcceptedDecisionModel = JsonConvert.SerializeObject(new
                {
                    scoringPass = "Initial",
                    mortgageLoanOffer = scoreResult.Offer,
                    recommendation = new
                    {
                        basis = scoreInput,
                        result = scoreResult
                    }
                })
            };
            context.FillInfrastructureFields(d);

            a.CurrentCreditDecision = d;
            context.CreditDecisions.Add(d);

            eventCode = CreditApplicationEventCode.MortgageLoanInitialScoringAccepted;
            var evt = context.CreateAndAddEvent(eventCode, applicationNr: applicationNr);

            mortgageLoanWorkflowService.ChangeStepStatusComposable(context, WorkflowStepName, mortgageLoanWorkflowService.AcceptedStatusName, applicationNr: applicationNr, evt: evt);
            offerDb.SetComposable(context, applicationNr, scoreResult.Offer);

            context.CreateAndAddComment("Accepted on initial scoring pass", eventCode.ToString(), applicationNr: applicationNr);

            return new ServiceEvent
            {
                Code = PreCreditEventCode.MortgageLoanInitialCreditCheckAccepted,
                Data = JsonConvert.SerializeObject(new
                {
                    applicationNr = applicationNr,
                    wasAutomated = wasAutomated,
                    currentUserId = context.CurrentUserId,
                    informationMetadata = context.InformationMetadata,
                    skipProviderCallback = skipProviderCallback
                })
            };
        }

        private ServiceEvent Reject(PreCreditContextExtended context,
            string applicationNr,
            List<string> rejectionReasons,
            PluginScoringProcessResult scoreResult,
            ScoringDataModel scoreInput)
        {
            bool wasAutomated = false; //TODO: Dynamic when moving this code

            CreditApplicationEventCode eventCode;

            var r = context.CreditApplicationHeaders.Where(x => x.ApplicationNr == applicationNr).Select(x => new
            {
                App = x,
                Ext = x.MortgageLoanExtension
            }).Single();

            var a = r.App;
            var m = r.Ext;

            m.InitialCreditCheckStatus = CreditApplicationMarkerStatusName.Rejected;
            a.CreditCheckStatus = CreditApplicationMarkerStatusName.Rejected;

            var d = new RejectedCreditDecision
            {
                ApplicationNr = applicationNr,
                CreditApplication = a,
                DecisionById = context.CurrentUserId,
                WasAutomated = wasAutomated,
                DecisionDate = context.Clock.Now,
                DecisionType = CreditDecisionTypeCode.Initial.ToString(),
                RejectedDecisionModel = JsonConvert.SerializeObject(new
                {
                    scoringPass = "Initial",
                    rejectionReasons = rejectionReasons,
                    recommendation = new
                    {
                        basis = scoreInput,
                        result = scoreResult
                    }
                })
            };
            context.FillInfrastructureFields(d);
            a.CurrentCreditDecision = d;
            context.CreditDecisions.Add(d);

            eventCode = CreditApplicationEventCode.MortgageLoanInitialScoringRejected;

            context.CreateAndAddEvent(eventCode, applicationNr: applicationNr);
            context.CreateAndAddComment("Rejected on initial scoring pass", eventCode.ToString(), applicationNr: applicationNr);

            return new ServiceEvent
            {
                Code = PreCreditEventCode.MortgageLoanInitialCreditCheckRejected,
                Data = JsonConvert.SerializeObject(new
                {
                    applicationNr = applicationNr,
                    wasAutomated = wasAutomated,
                    currentUserId = context.CurrentUserId,
                    informationMetadata = context.InformationMetadata,
                })
            };
        }

        private class ServiceEvent
        {
            public PreCreditEventCode Code { get; set; }
            public string Data { get; set; }
        }

        private class CustomerState
        {
            public bool CustomerAddressIsKnown { get; set; }
            public bool CustomerNameIsKnown { get; set; }
        }
        private IDictionary<int, CustomerState> GetCustomerStateByApplicantNr(int nrOfApplicants, Func<int, int> getCustomerIdByApplicantNr)
        {
            var customerClient = new PreCreditCustomerClient();
            var applicantNrByCustomerId = Enumerable
                .Range(1, nrOfApplicants)
                .Select(x => new { applicantNr = x, customerId = getCustomerIdByApplicantNr(x) })
                .ToDictionary(x => x.customerId, x => x.applicantNr);

            var customerPropertiesByCustomerId = customerClient.BulkFetchPropertiesByCustomerIdsSimple(new HashSet<int>(applicantNrByCustomerId.Keys), "firstName", "addressZipcode");

            return applicantNrByCustomerId.ToDictionary(x => x.Value, x =>
            {
                var applicantNr = x.Value;
                var customerId = x.Key;
                var customerProperties = customerPropertiesByCustomerId[customerId];
                return new CustomerState
                {
                    CustomerAddressIsKnown = !string.IsNullOrWhiteSpace(customerProperties?.Opt("addressZipcode")),
                    CustomerNameIsKnown = !string.IsNullOrWhiteSpace(customerProperties?.Opt("firstName"))
                };
            });
        }

        public bool TryGetCurrentAcceptedInitialOffer(ApplicationInfoModel application, out MortgageLoanInitialOffer offer, out string failedMessage)
        {
            if (application.MortgageLoanInitialCreditCheckStatus != "Accepted")
            {
                failedMessage = "The initial credit check was not accepted";
                offer = null;
                return false;
            }
            using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, clock))
            {
                var app = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == application.ApplicationNr)
                    .Select(x => new
                    {
                        InitialCreditDecision = x.CreditDecisions.OrderBy(y => y.Id).FirstOrDefault(),
                        x.MortgageLoanExtension.CustomerOfferStatus
                    })
                    .Single();

                if (app.CustomerOfferStatus != MortgageLoanCustomerOfferStatusCode.OfferAcceptedByCustomer.ToString())
                {
                    failedMessage = "The initial offer was not accepted by the customer";
                    offer = null;
                    return false;
                }

                var initialAcceptedDecison = CreditDecisionModelParser.ParseMortgageLoanAcceptedDecision((app.InitialCreditDecision as AcceptedCreditDecision)?.AcceptedDecisionModel);
                var o = initialAcceptedDecison.MortgageLoanOffer;

                offer = new MortgageLoanInitialOffer
                {
                    InitialFeeAmount = o.InitialFeeAmount,
                    LoanAmount = o.LoanAmount,
                    MonthlyAmortizationAmount = o.MonthlyAmortizationAmount.Value,
                    MonthlyFeeAmount = o.MonthlyFeeAmount,
                    NominalInterestRatePercent = o.NominalInterestRatePercent
                };

                if (offer.LoanAmount <= 0m || offer.NominalInterestRatePercent <= 0m)
                {
                    failedMessage = "Invalid offer, missing loan amount or nominal interest rate";
                    offer = null;
                    return false;
                }

                failedMessage = null;
                return true;
            }
        }
    }

    public interface IMortgageLoanApplicationInitialCreditCheckService
    {
        bool TryDoInitialScoring(string applicationNr, bool skipProviderCallback, out MortgageLoanInitialScoringResponse result, out string failedMessage);
        bool TryGetCurrentAcceptedInitialOffer(ApplicationInfoModel application, out MortgageLoanInitialOffer offer, out string failedMessage);
    }

    public class MortgageLoanInitialOffer
    {
        public decimal LoanAmount { get; set; }
        public decimal MonthlyAmortizationAmount { get; set; }
        public decimal NominalInterestRatePercent { get; set; }
        public decimal MonthlyFeeAmount { get; set; }
        public decimal InitialFeeAmount { get; set; }
    }

    public class MortgageLoanInitialScoringResponse
    {
        public Offer AcceptedOffer { get; set; }
        public RejectionDetails RejectedDetails { get; set; }

        public class Offer
        {
            public decimal LoanAmount { get; set; }
            public decimal MonthlyAmortizationAmount { get; set; }
            public decimal NominalInterestRatePercent { get; set; }
            public decimal MonthlyFeeAmount { get; set; }
            public decimal InitialFeeAmount { get; set; }
            public DateTime ValidUntilDate { get; set; }
        }

        public class RejectionDetails
        {
            public List<string> RejectionReasons { get; set; }
        }
    }
}