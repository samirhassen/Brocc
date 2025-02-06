using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace nPreCredit.Code.AffiliateReporting.Sortter
{
    public class SortterWebservice : JsonAffiliateWebserviceBase, ISortterWebservice
    {
        private readonly IAffiliateDataSource affiliateDataSource;
        private readonly IClientConfiguration clientConfiguration;

        public SortterWebservice(IAffiliateDataSource affiliateDataSource, IClientConfiguration clientConfiguration)
        {
            this.affiliateDataSource = affiliateDataSource;
            this.clientConfiguration = clientConfiguration;
        }

        /// <summary>
        /// approved - lender approved the loan application and makes an loan offer 
        /// </summary>
        public HandleEventResult Approved(CreditDecisionApprovedEventModel evt)
        {
            var o = evt.GetSimplifiedOffer();

            return Put(new
            {
                currencyCode = clientConfiguration.Country.BaseCurrency,
                approvedAmount = o.LoanAmount,
                loanTermInMonths = o.RepaymentTimeInMonths,
                nominalInterestRate = o.InterestRatePercent,
                arrangementFee = o.InitialFeeAmount,
                administrationFee = o.NotificationFeeAmount,
                monthlyCosts = o.MontlyPaymentIncludingFees,
                totalAnnualPercentageRate = o.EffectiveInterestRatePercent,
                additionalDocuments = new string[] { },
                redirectUrl = evt.ApplicationUrl
            }, evt, "approved");
        }

        /// <summary>
        /// rejected - lender rejected the loan application and decided not to make loan offer
        /// </summary>
        public HandleEventResult Rejected(CreditApplicationRejectedEventModel evt)
        {
            return Put(new
            {
                reason = "other"
            }, evt, "rejected");
        }

        /// <summary>
        /// paid-out - the loan was paid to the applicant 
        /// </summary>
        public HandleEventResult Completed(LoanPaidOutEventModel evt)
        {
            return Put(new
            {
                currencyCode = clientConfiguration.Country.BaseCurrency,
                approvedAmount = evt.PaymentAmount,
                paidOutDate = evt.PaymentDate.ToString("yyyy-MM-dd")
            }, evt, "paid-out");
        }

        /// <summary>
        /// canceled - the loan application was canceled
        /// </summary>
        public HandleEventResult Cancelled(CreditApplicationCancelledEventModel evt)
        {
            //'expired' | 'applicant canceled' | 'other'
            return Put(new
            {
                reason = evt.WasAutomated ? "expired" : "other"
            }, evt, "canceled");
        }

        private HandleEventResult Put<T>(T payload, AffiliateReportingEventModelBase e, string state)
        {
            var settings = affiliateDataSource.GetSettings(e.ProviderName);
            var custom = settings.ReadCustomSettingsAs<SortterCustomSettingsModel>();
            string jsonRequest = null;

            var url = custom
                .UrlPattern
                .Replace("{{id}}", e.ProviderApplicationId)
                .Replace("{{state}}", state);

            return SendJsonRequest(payload, HttpMethod.Put, url, response =>
            {
                var code = (int)response.StatusCode;
                var jsonResponse = ReadJsonBodyIfAny(response);
                if (response.IsSuccessStatusCode)
                    return Success(outgoingRequestBody: jsonRequest, outgoingResponseBody: jsonResponse);
                else if (code == 404)
                    return Failed("Provider report the application does not exist.", outgoingRequestBody: jsonRequest, outgoingResponseBody: jsonResponse);
                else if (code == 422)
                    return HandleValidationError(jsonRequest, jsonResponse);
                else if (code == 401)
                    return Failed(message: "Invalid login credentials. Contact sortter and ask for new a new webservice login.", outgoingRequestBody: jsonRequest, outgoingResponseBody: jsonResponse);
                else
                    return Failed(message: $"Provider serverside error. {response.StatusCode} - {response.ReasonPhrase}", outgoingRequestBody: jsonRequest, outgoingResponseBody: jsonResponse);
            }, m =>
            {
                m.Headers.Add("auth-id", custom.AuthId);
                m.Headers.Add("auth-key", custom.AuthKey);
            }, observeJsonRequest: x => jsonRequest = x);
        }

        private HandleEventResult HandleValidationError(string jsonRequest, string jsonResponse)
        {
            ValidationErrorsModel errors = null;
            try
            {
                errors = JsonConvert.DeserializeObject<ValidationErrorsModel>(jsonResponse);
            }
            catch { /* ignored*/ }

            string extra = "";
            if (errors != null && errors.ValidationErrors != null && errors.ValidationErrors.Count > 0)
            {
                extra = " :" + string.Join(", ", errors.ValidationErrors.Select(x => $"{x.Field}={x.Message}"));
            }
            return Failed($"Provider reported the request was invalid{extra}", outgoingRequestBody: jsonRequest, outgoingResponseBody: jsonResponse);
        }

        private class ValidationErrorsModel
        {
            public List<ValidationErrorModel> ValidationErrors { get; set; }
            public class ValidationErrorModel
            {
                public string Field { get; set; }
                public string Message { get; set; }
            }
        }
    }

    public class SortterCustomSettingsModel
    {
        public string AuthId { get; set; }
        public string AuthKey { get; set; }
        public string UrlPattern { get; set; }
    }
}
