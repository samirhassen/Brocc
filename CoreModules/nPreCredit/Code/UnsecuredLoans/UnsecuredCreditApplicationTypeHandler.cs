using Newtonsoft.Json;
using nPreCredit.Code.Agreements;
using nPreCredit.Code.Services;
using NTech;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code
{
    public class UnsecuredCreditApplicationTypeHandler : ICreditApplicationTypeHandler
    {
        private IClock clock;
        private readonly IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository;
        private readonly IHttpContextUrlService urlService;
        private readonly LoanAgreementPdfBuilderFactory agreementPdfBuilderFactory;

        public UnsecuredCreditApplicationTypeHandler(IClock clock, IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository, IHttpContextUrlService urlService,
            LoanAgreementPdfBuilderFactory agreementPdfBuilderFactory)
        {
            this.clock = clock;
            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
            this.urlService = urlService;
            this.agreementPdfBuilderFactory = agreementPdfBuilderFactory;
        }

        public string GetNewCreditCheckUrl(IServiceRegistryUrlService urlService, string applicationNr)
        {
            return urlService.LoggedInUserNavigationUrl(
                "CreditCheck/New",
                Tuple.Create("applicationNr", applicationNr))?.ToString();
        }

        public bool ShouldCheckAgreementUpdatesOnEnterApplication()
        {
            return true;
        }

        public AgreementSigningStatusWithPending GetAgreementSigningStatusWithPending(string applicationNr, IEnumerable<CreditApplicationOneTimeToken> tokens, out IList<CreditApplicationOneTimeToken> currentlyPendingTokens)
        {
            bool isAdditionalLoanOffer;
            using (var context = new PreCreditContext())
            {
                var app = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => new { x.CreditCheckStatus, x.CurrentCreditDecision })
                    .Single();
                var acceptedDecision = app.CurrentCreditDecision as AcceptedCreditDecision;

                if (app.CreditCheckStatus != "Accepted")
                {
                    currentlyPendingTokens = null;
                    return null;
                }

                if (app.CreditCheckStatus == "Accepted" && acceptedDecision == null)
                {
                    throw new Exception("Credit check is not approved");
                }

                var decisionModel = JsonConvert.DeserializeAnonymousType(acceptedDecision.AcceptedDecisionModel, new
                {
                    offer = new { amount = new decimal?() },
                    additionalLoanOffer = new { amount = new decimal?() }
                });

                if (decisionModel.offer == null && decisionModel.additionalLoanOffer == null)
                {
                    throw new Exception("Application has no offer");
                }

                if (decisionModel.offer != null && decisionModel.additionalLoanOffer != null)
                {
                    throw new Exception("Application has both a new loan offer and an additional loan offer");
                }

                isAdditionalLoanOffer = decisionModel.additionalLoanOffer != null;
            }

            var model = new AgreementSigningStatusWithPending();
            Action<int, AgreementSigningStatusWithPending.Applicant> setApplicant = (x, y) =>
            {
                if (x == 1)
                    model.applicant1 = y;
                else if (x == 2)
                    model.applicant2 = y;
                else
                    throw new NotImplementedException();
            };

            currentlyPendingTokens = new List<CreditApplicationOneTimeToken>();

            var tokensByApplicantNr = tokens
                .Select(x => new
                {
                    t = x,
                    d = JsonConvert.DeserializeAnonymousType(x.TokenExtraData, new { status = "", applicantNr = 0, signedDocumentKey = "", failureMessage = "" })
                })
                .GroupBy(x => x.d.applicantNr)
                .ToDictionary(x => x.Key);

            var appModel = partialCreditApplicationModelRepository.Get(applicationNr, new PartialCreditApplicationModelRequest { DocumentFields = new List<string> { "signed_initial_agreement_key", "signed_initial_agreement_date" } });

            foreach (var applicantNr in Enumerable.Range(1, appModel.NrOfApplicants))
            {
                var agreementKey = appModel.Document(applicantNr).Get("signed_initial_agreement_key").StringValue.Optional;
                var agreementDate = appModel.Document(applicantNr).Get("signed_initial_agreement_date").DateAndTimeValue.Optional;

                if (agreementKey != null)
                {
                    setApplicant(
                        applicantNr,
                        new AgreementSigningStatusWithPending.Applicant
                        {
                            status = "Success",
                            signedDocumentUrl = urlService.ActionStrict("ArchiveDocument", "CreditManagement", routeValues: new { key = agreementKey }),
                            signedDate = agreementDate
                        });
                }
                else
                {
                    var token = !tokensByApplicantNr.ContainsKey(applicantNr) ? null : tokensByApplicantNr[applicantNr].OrderByDescending(x => x.t.CreationDate).FirstOrDefault();
                    if (token != null)
                    {
                        if (token.d.status == "Failure")
                        {
                            setApplicant(
                                applicantNr,
                                new AgreementSigningStatusWithPending.Applicant
                                {
                                    status = "Failure",
                                    failureMessage = token.d.failureMessage
                                });
                        }
                        else
                        {
                            setApplicant(
                                applicantNr,
                                new AgreementSigningStatusWithPending.Applicant
                                {
                                    status = "Pending",
                                    sentDate = token.t.CreationDate
                                });
                            currentlyPendingTokens.Add(token.t);
                        }
                    }
                    else
                    {
                        setApplicant(
                            applicantNr,
                            new AgreementSigningStatusWithPending.Applicant
                            {
                                status = "NotSent"
                            });
                    }
                }
            }
            var pdfBuilder = agreementPdfBuilderFactory.Create(isAdditionalLoanOffer);
            string _;
            model.isSendAllowed = pdfBuilder.IsCreateAgreementPdfAllowed(applicationNr, out _);

            return model;
        }

        public IDictionary<string, string> GetRejectionReasonToDisplayNameMapping()
        {
            return NEnv.ScoringSetup.GetRejectionReasonToDisplayNameMapping();
        }

        public string GetViewCreditDecisionUrl(IServiceRegistryUrlService urlService, int decisionId)
        {
            return urlService.LoggedInUserNavigationUrl(
                "CreditCheck/View",
                Tuple.Create("id", decisionId.ToString()))?.ToString();
        }
    }
}
