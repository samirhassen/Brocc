using NTech;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class MortgageLoanApplicationCreditCheckService : IMortgageLoanApplicationCreditCheckService
    {
        private IServiceRegistryUrlService urlService;
        private ICreditApplicationTypeHandler mortgageLoanCreditApplicationTypeHandler;
        private readonly INTechCurrentUserMetadata ntechCurrentUserMetadata;
        private readonly IClock clock;

        public MortgageLoanApplicationCreditCheckService(INTechCurrentUserMetadata ntechCurrentUserMetadata,
            IServiceRegistryUrlService urlService,
            ICreditApplicationTypeHandler mortgageLoanCreditApplicationTypeHandler, IClock clock)
        {
            this.urlService = urlService;
            this.mortgageLoanCreditApplicationTypeHandler = mortgageLoanCreditApplicationTypeHandler;
            this.ntechCurrentUserMetadata = ntechCurrentUserMetadata;
            this.clock = clock;
        }

        private PreCreditContextExtended createContext()
        {
            return new PreCreditContextExtended(ntechCurrentUserMetadata, clock);
        }

        public MortgageLoanApplicationInitialCreditCheckStatusModel FetchApplicationInitialStatus(string applicationNr)
        {
            using (var context = createContext())
            {
                var h = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => new
                    {
                        x.IsActive,
                        x.IsPartiallyApproved,
                        x.WaitingForAdditionalInformationDate,
                        InitialCreditDecision = x.CreditDecisions.OrderBy(y => y.Id).FirstOrDefault(),
                        x.MortgageLoanExtension.InitialCreditCheckStatus,
                        x.IsFinalDecisionMade,
                        CustomerOfferStatus = x.MortgageLoanExtension.CustomerOfferStatus
                    })
                    .Single();

                var cd = h.InitialCreditDecision;

                var ad = cd as AcceptedCreditDecision;
                var rd = cd as RejectedCreditDecision;

                MortgageLoanApplicationInitialCreditCheckStatusModel.AcceptedDecisionModel acceptedDecision = null;
                MortgageLoanApplicationInitialCreditCheckStatusModel.RejectedDecisionModel rejectedDecision = null;
                if (ad != null)
                {
                    var decisionModel = CreditDecisionModelParser.ParseMortgageLoanAcceptedDecision(ad.AcceptedDecisionModel);
                    if (decisionModel.ScoringPass != "Initial")
                        throw new Exception($"The application {applicationNr} seems to lack an initial scoring pass");

                    acceptedDecision = new MortgageLoanApplicationInitialCreditCheckStatusModel.AcceptedDecisionModel
                    {
                        Offer = decisionModel?.MortgageLoanOffer == null ? null : new MortgageLoanApplicationInitialCreditCheckStatusModel.OfferModel
                        {
                            InitialFeeAmount = decisionModel.MortgageLoanOffer?.InitialFeeAmount,
                            LoanAmount = decisionModel.MortgageLoanOffer?.LoanAmount,
                            MonthlyAmortizationAmount = decisionModel.MortgageLoanOffer?.MonthlyAmortizationAmount,
                            MonthlyFeeAmount = decisionModel.MortgageLoanOffer?.MonthlyFeeAmount,
                            NominalInterestRatePercent = decisionModel.MortgageLoanOffer?.NominalInterestRatePercent
                        },
                        ScoringPass = decisionModel?.ScoringPass
                    };
                }
                else if (rd != null)
                {
                    var decisionModel = CreditDecisionModelParser.ParseMortgageLoanRejectedDecision(rd.RejectedDecisionModel);
                    if (decisionModel.ScoringPass != "Initial")
                        throw new Exception($"The application {applicationNr} seems to lack an initial scoring pass");

                    var toDisplayName = this.mortgageLoanCreditApplicationTypeHandler.GetRejectionReasonToDisplayNameMapping();
                    var actualReasons = decisionModel?.RejectionReasons?.Select(x => new MortgageLoanApplicationInitialCreditCheckStatusModel.RejectionReasonModel
                    {
                        Name = x,
                        DisplayName = toDisplayName?.Opt(x) ?? x
                    })
                        .ToList();
                    rejectedDecision = new MortgageLoanApplicationInitialCreditCheckStatusModel.RejectedDecisionModel
                    {
                        ScoringPass = decisionModel?.ScoringPass,
                        RejectionReasons = actualReasons
                    };
                }
                else if (cd != null)
                    throw new Exception("Unknown credit decision type");

                var currentCreditDecisionId = cd?.Id;

                return new MortgageLoanApplicationInitialCreditCheckStatusModel
                {
                    CreditCheckStatus = h.InitialCreditCheckStatus,
                    CustomerOfferStatus = h.CustomerOfferStatus,
                    IsViewDecisionPossible = currentCreditDecisionId.HasValue,
                    ViewCreditDecisionUrl = currentCreditDecisionId.HasValue
                        ? this.mortgageLoanCreditApplicationTypeHandler.GetViewCreditDecisionUrl(this.urlService, currentCreditDecisionId.Value)
                        : null,
                    AcceptedDecision = acceptedDecision,
                    RejectedDecision = rejectedDecision,
                };
            }
        }

        public MortgageLoanApplicationFinalCreditCheckStatusModel FetchApplicationFinalStatus(string applicationNr)
        {
            throw new NotImplementedException();
        }
    }

    public interface IMortgageLoanApplicationCreditCheckService
    {
        MortgageLoanApplicationInitialCreditCheckStatusModel FetchApplicationInitialStatus(string applicationNr);
        MortgageLoanApplicationFinalCreditCheckStatusModel FetchApplicationFinalStatus(string applicationNr);
    }

    public class MortgageLoanApplicationInitialCreditCheckStatusModel
    {
        public string CreditCheckStatus { get; set; }
        public string CustomerOfferStatus { get; set; }
        public bool IsViewDecisionPossible { get; set; }
        public string ViewCreditDecisionUrl { get; set; }
        public RejectedDecisionModel RejectedDecision { get; set; }
        public AcceptedDecisionModel AcceptedDecision { get; set; }
        public class RejectedDecisionModel
        {
            public string ScoringPass { get; set; }
            public List<RejectionReasonModel> RejectionReasons { get; set; }
        }
        public class AcceptedDecisionModel
        {
            public string ScoringPass { get; set; }
            public OfferModel Offer { get; set; }
        }
        public class OfferModel
        {
            public decimal? LoanAmount { get; set; }
            public decimal? MonthlyAmortizationAmount { get; set; }
            public decimal? NominalInterestRatePercent { get; set; }
            public decimal? InitialFeeAmount { get; set; }
            public decimal? MonthlyFeeAmount { get; set; }
        }
        public class RejectionReasonModel
        {
            public string DisplayName { get; set; }
            public string Name { get; set; }
        }
    }

    public class MortgageLoanApplicationFinalCreditCheckStatusModel
    {
        public bool HasNonExpiredBindingOffer { get; set; }
        public bool IsNewCreditCheckPossible { get; set; }
        public string NewCreditCheckUrl { get; set; }
        public string UnsignedAgreementDocumentUrl { get; set; }
        public string UnsignedAgreementDocumentArchiveKey { get; set; }
        public string CreditCheckStatus { get; set; }
        public bool IsViewDecisionPossible { get; set; }
        public string ViewCreditDecisionUrl { get; set; }
        public RejectedDecisionModel RejectedDecision { get; set; }
        public AcceptedDecisionModel AcceptedDecision { get; set; }
        public class RejectedDecisionModel
        {
            public string ScoringPass { get; set; }
            public List<RejectionReasonModel> RejectionReasons { get; set; }
        }
        public class AcceptedDecisionModel
        {
            public string ScoringPass { get; set; }
            public OfferModel Offer { get; set; }
        }
        public class OfferModel
        {
            public decimal? LoanAmount { get; set; }
            public decimal? MonthlyAmortizationAmount { get; set; }
            public decimal? NominalInterestRatePercent { get; set; }
            public decimal? InitialFeeAmount { get; set; }
            public decimal? MonthlyFeeAmount { get; set; }
            public string BindingUntilDate { get; set; }

            public DateTime? GetBindingUntilDate()
            {
                if (BindingUntilDate == null)
                    return null;
                return DateTime.ParseExact(BindingUntilDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        public class RejectionReasonModel
        {
            public string DisplayName { get; set; }
            public string Name { get; set; }
        }
    }
}