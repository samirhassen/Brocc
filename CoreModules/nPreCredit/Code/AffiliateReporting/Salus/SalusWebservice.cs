using System;
using System.Globalization;
using System.Net.Http;
using System.Web;

namespace nPreCredit.Code.AffiliateReporting.Salus
{
    public class SalusWebservice : JsonAffiliateWebserviceBase, ISalusWebservice
    {
        private readonly IAffiliateDataSource affiliateDataSource;

        public SalusWebservice(IAffiliateDataSource affiliateDataSource)
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
            var custom = settings.ReadCustomSettingsAs<SalusSettingsModel>();

            string jsonRequest = null;
            return SendJsonRequest(new
            {
                externalId = evt.ProviderApplicationId,
                messageType = messageType,
                messageData = messageData
            },
                HttpMethod.Post, AppendQueryStringParam(new Uri(custom.Url), "apikey", custom.ApiKey).ToString(), r =>
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
                },
                observeJsonRequest: x => jsonRequest = x
            );
        }

        private Uri AppendQueryStringParam(Uri uri, string name, string value)
        {
            var uriBuilder = new UriBuilder(uri);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query[name] = value;
            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri;
        }

        private string Dec(decimal? d)
        {
            return d.HasValue ? d.Value.ToString(CultureInfo.InvariantCulture) : "";
        }
    }
}