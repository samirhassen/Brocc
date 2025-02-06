using System.Globalization;
using System.Net.Http;

namespace nPreCredit.Code.AffiliateReporting.Standard
{
    public class StandardWebservice : JsonAffiliateWebserviceBase, IStandardWebservice
    {
        private readonly IAffiliateDataSource affiliateDataSource;

        public StandardWebservice(IAffiliateDataSource affiliateDataSource)
        {
            this.affiliateDataSource = affiliateDataSource;
        }

        public HandleEventResult ReportAcceptedApplication(CreditDecisionApprovedEventModel evt)
        {
            var offer = evt.GetSimplifiedOffer();
            return Post(
                evt, "credit-decision-accepted",
                new
                {
                    amount = Dec(offer?.LoanAmount),
                    monthlyAmount = Dec(offer?.MontlyPaymentIncludingFees),
                    monthlyFee = Dec(offer?.NotificationFeeAmount),
                    initialFee = Dec(offer?.InitialFeeAmount),
                    repaymentTimeInMonths = Dec(offer?.RepaymentTimeInMonths),
                    interestRateNominal = Dec(offer?.InterestRatePercent),
                    interestRateEffective = Dec(offer?.EffectiveInterestRatePercent),
                    signatureUrlApplicant1 = evt.ApplicationUrl
                });
        }

        public HandleEventResult ReportCustomerSignedAgreement(CreditApplicationSignedAgreementEventModel evt)
        {
            return Post(
              evt, "customer-signed-agreement",
              new
              {
                  applicantNr = evt.ApplicantNr,
                  allApplicantsHaveNowSigned = evt.AllApplicantsHaveNowSigned
              });
        }

        public HandleEventResult ReportRejectedApplication(CreditApplicationRejectedEventModel evt)
        {
            return Post(
              evt, "credit-decision-rejected",
              new
              {
                  rejectionReasonCode = evt.IsRejectedDueToPaymentRemark() ? "PaymentRemark" : "OtherScoring"
              });
        }

        public HandleEventResult ReportCancelledApplication(CreditApplicationCancelledEventModel evt)
        {
            return Post(
              evt, "credit-application-cancelled",
              new
              {
                  reason = evt.WasAutomated ? "expired" : "other"
              });
        }

        public HandleEventResult ReportLoanPaidToCustomer(LoanPaidOutEventModel evt)
        {
            return Post(evt, "loan-paid-to-customer",
              new
              {
                  paymentAmount = Dec(evt.PaymentAmount),
                  paymentDate = evt.PaymentDate.ToString("yyyy-MM-dd")
              });
        }

        private HandleEventResult Post<T>(AffiliateReportingEventModelBase evt, string messageType, T messageData)
        {
            var settings = affiliateDataSource.GetSettings(evt.ProviderName);
            var custom = settings.ReadCustomSettingsAs<StandardSettingsModel>();

            string jsonRequest = null;
            return SendJsonRequest(new
            {
                externalId = evt.ProviderApplicationId,
                applicationNr = evt.ApplicationNr,
                messageType = messageType,
                messageData = messageData
            },
                HttpMethod.Post, custom.Url, r =>
                {
                    if (r.IsSuccessStatusCode)
                    {
                        string contentType = "";
                        var jsonOrHtmlResponse = ReadJsonOrHtmlBodyIfAny(r, x => x, x => x, x => contentType = $" - '{x}'");
                        return Success(message: $"Success", outgoingRequestBody: jsonRequest, outgoingResponseBody: jsonOrHtmlResponse);
                    }
                    else
                    {
                        string contentType = "";
                        var jsonOrHtmlResponse = ReadJsonOrHtmlBodyIfAny(r, x => x, x => x, x => contentType = $" - '{x}'");
                        return Failed(message: $"Affiliate serverside error. {(int)r.StatusCode} - {r.ReasonPhrase}{contentType}", outgoingRequestBody: jsonRequest, outgoingResponseBody: jsonOrHtmlResponse);
                    }
                }, setupMessage: m =>
                {
                    if (!string.IsNullOrWhiteSpace(custom.BasicAuthPassword))
                    {
                        AddBasicAuthenticationHeader(m, custom.BasicAuthUserName, custom.BasicAuthPassword);
                    }
                },
                observeJsonRequest: x => jsonRequest = x
            );
        }

        private string Dec(decimal? d)
        {
            return d.HasValue ? d.Value.ToString(CultureInfo.InvariantCulture) : "";
        }
    }
}