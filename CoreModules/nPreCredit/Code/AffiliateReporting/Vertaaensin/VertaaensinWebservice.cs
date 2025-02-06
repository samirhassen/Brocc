using System.Net.Http;

namespace nPreCredit.Code.AffiliateReporting.Vertaaensin
{
    public class VertaaensinWebservice : JsonAffiliateWebserviceBase, IVertaaensinWebservice
    {
        private readonly IAffiliateDataSource affiliateDataSource;

        public VertaaensinWebservice(IAffiliateDataSource affiliateDataSource)
        {
            this.affiliateDataSource = affiliateDataSource;
        }

        public HandleEventResult ReportApproved(CreditDecisionApprovedEventModel evt)
        {
            var settings = affiliateDataSource.GetSettings(evt.ProviderName);
            var custom = settings.ReadCustomSettingsAs<VertaaensinCustomSettingsModel>();

            var a = evt.GetSimplifiedOffer();

            return Post(new
            {
                externalApplicationId = evt.ProviderApplicationId,
                providerCode = custom.ProviderCode,
                status = "APPROVED",
                loanAmount = a?.LoanAmount,
                tenor = a?.RepaymentTimeInMonths, //repayment time in months  
                monthlyPayment = a?.MontlyPaymentIncludingFees, //including all fees that the customer has to pay,  
                nominalInterest = a?.InterestRatePercent,
                apr = a?.EffectiveInterestRatePercent,
                openingFee = evt.NewLoanOffer?.InitialFeeAmount,
                monthlyFee = a?.NotificationFeeAmount,
                applicationUrl = evt.ApplicationUrl
            }, custom);
        }

        public HandleEventResult ReportRejected(CreditApplicationRejectedEventModel evt)
        {
            var settings = affiliateDataSource.GetSettings(evt.ProviderName);
            var custom = settings.ReadCustomSettingsAs<VertaaensinCustomSettingsModel>();

            return Post(new
            {
                externalApplicationId = evt.ProviderApplicationId,
                providerCode = custom.ProviderCode,
                status = "REJECTED",
                message = "Application rejected" //NOTE: evt.RejectionReasons could be used to make this more granular but make sure we are allowed to send out such info first.
            }, custom);
        }

        public HandleEventResult ReportLoanPaidOut(LoanPaidOutEventModel evt)
        {
            var settings = affiliateDataSource.GetSettings(evt.ProviderName);
            var custom = settings.ReadCustomSettingsAs<VertaaensinCustomSettingsModel>();

            return Post(new
            {
                externalApplicationId = evt.ProviderApplicationId,
                providerCode = custom.ProviderCode,
                status = "LOAN_ISSUED",
                loanAmount = evt.PaymentAmount
            }, custom);
        }

        public HandleEventResult Post<T>(T payload, VertaaensinCustomSettingsModel s)
        {
            string jsonRequest = null;
            return SendJsonRequest(payload, HttpMethod.Post, s.Url, r =>
            {
                if (r.IsSuccessStatusCode)
                {
                    string jsonResponse = null;
                    return HandleJsonResponseAsAnonymousType(
                        r,
                        new { status = (string)null, message = (string)null },
                        p => (p.status == "success")
                            ? Success(message: $"Success: {p.message}", outgoingRequestBody: jsonRequest, outgoingResponseBody: jsonResponse)
                            : Failed(message: $"Affiliate serverside error: {p.message}", outgoingRequestBody: jsonRequest, outgoingResponseBody: jsonResponse),
                        observeJsonResponse: x => jsonResponse = x);
                }
                else
                {
                    string contentType = "";
                    var jsonOrHtmlResponse = ReadJsonOrHtmlBodyIfAny(r, x => x, x => x, x => contentType = $" - '{x}'");
                    return Failed(message: $"Affiliate serverside error. {(int)r.StatusCode} - {r.ReasonPhrase}{contentType}", outgoingRequestBody: jsonRequest, outgoingResponseBody: jsonOrHtmlResponse);
                }
            },
            m => AddBasicAuthenticationHeader(m, s.Username, s.Password),
            observeJsonRequest: x => jsonRequest = x
            );
        }
    }

    public class VertaaensinCustomSettingsModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ProviderCode { get; set; }
        public string Url { get; set; }
    }
}