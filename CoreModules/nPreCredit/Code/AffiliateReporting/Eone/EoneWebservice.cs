using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;

namespace nPreCredit.Code.AffiliateReporting.Eone
{
    public class EoneWebservice : JsonAffiliateWebserviceBase, IEoneWebservice
    {
        private readonly IAffiliateDataSource affiliateDataSource;

        public EoneWebservice(IAffiliateDataSource affiliateDataSource)
        {
            this.affiliateDataSource = affiliateDataSource;
        }
        private class EoneResponse
        {
            public string Status { get; set; } //OK | FAIL
            public string ErrorCode { get; set; }

            public string InferErrorMessage(bool isOutPayment)
            {
                var d = isOutPayment ? outpaymentErrorMessageFromCode : decisionErrorMessageFromCode;
                if (ErrorCode != null && d.ContainsKey(ErrorCode))
                    return d[ErrorCode];
                else
                    return "Unknown errorcode";
            }
        }

        public HandleEventResult ReportGrantedApplication(CreditDecisionApprovedEventModel evt)
        {
            var offer = evt?.GetSimplifiedOffer();

            int? periodInYears = null;


            if ((offer?.RepaymentTimeInMonths).HasValue)
            {
                periodInYears = (int)Math.Round(((decimal)offer.RepaymentTimeInMonths.Value) / 12m);
            }

            return Post(evt, false, new
            {
                ID = evt.ProviderApplicationId,
                Type = "Granted",
                Amount = F(offer?.LoanAmount, x => x.ToString("F0", CultureInfo.InvariantCulture)),
                Period = F(periodInYears, x => x.ToString(CultureInfo.InvariantCulture)),
                Installment = F(offer?.MontlyPaymentIncludingFees, x => x.ToString(CultureInfo.InvariantCulture)),
                InterestNominal = F(offer?.InterestRatePercent, x => x.ToString(CultureInfo.InvariantCulture)),
                InterestEffective = F(offer?.EffectiveInterestRatePercent, x => x.ToString(CultureInfo.InvariantCulture)),
                EsigningURL = evt.ApplicationUrl
            });
        }

        public HandleEventResult ReportRejectedApplication(CreditApplicationRejectedEventModel evt)
        {
            var rejectionCode = evt.IsRejectedDueToPaymentRemark() ? EoneApiRejectionCode.PaymentDefaults : EoneApiRejectionCode.ScoringDefaults;
            return Post(evt, false, new
            {
                ID = evt.ProviderApplicationId,
                Type = "Denied",
                DeniedType = rejectionCode.ToString()
            });
        }

        public HandleEventResult ReportPaymentOnNewCredit(LoanPaidOutEventModel evt)
        {
            return Post(evt, true, new
            {
                ID = evt.ProviderApplicationId,
                Date = evt.PaymentDate.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture),
                Amount = Convert.ToInt32(Math.Floor(evt.PaymentAmount)).ToString(CultureInfo.InvariantCulture)
            });
        }

        #region "Error Codes"
        private static Dictionary<string, string> decisionErrorMessageFromCode = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            {"0", "Service error" },
            {"1", "Content type is invalid" },
            {"2", "Malformed request" },
            {"3", "API key is missing" },
            {"4", "API key is invalid" },
            {"5", "Internal server error" },
            {"6", "Invalid request method (use POST)" },
            {"30020", "Internal error" },
            {"30030", "Internal error" },
            {"30040", "Missing transaction ID field" },
            {"30050", "Invalid transaction ID" },
            {"30060", "Internal error" },
            {"30070", "Internal error" },
            {"30080", "Missing decision type field" },
            {"30090", "Invalid decision type" },
            {"30100", "Missing loan amount field" },
            {"30110", "Invalid loan amount" },
            {"30111", "Missing loan period field" },
            {"30112", "Invalid loan period" },
            {"30120", "Missing necessary interest rate fields" },
            {"30130", "Invalid interest rate" },
            {"30131", "Missing installment field" },
            {"30132", "Invalid monthly installment amount" },
            {"30133", "Missing CanceledType field" },
            {"30134", "Invalid CanceledType value" },
            {"30135", "Missing reason for canceled decision" },
            {"30140", "Invalid denied loan reason" },
            {"30150", "Decision has already been added" },
            {"30151", "Only previously reported granted decision can be canceled" },
            {"30160", "Internal error" },
            {"30170", "Internal error" },
            {"30180", "Internal error" },
            {"30181", "Internal error" }
        };
        private static Dictionary<string, string> outpaymentErrorMessageFromCode = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            {"0", "Service error" },
            {"1", "Content type is invalid" },
            {"2", "Malformed request" },
            {"3", "API key is missing" },
            {"4", "API key is invalid" },
            {"5", "Internal server error" },
            {"6", "Invalid request method" },
            {"10010", "Out payment information is missing" },
            {"10020", "Missing out payment ID field" },
            {"10030", "Missing out payment Date field" },
            {"10040 ", "Missing out payment Amount field" },
            {"10050", "Internal server error" },
            {"10110", "Internal server error" },
            {"10120", "Invalid out payment ID" },
            {"10130", "Invalid out payment Date" },
            {"10140", "Invalid out payment Amount" },
            {"10210", "Out payment information already added" },
            {"10220", "Internal server error" },
            {"10230", "Internal server error" },
            {"10240", "Internal server error" },
            {"10310", "Internal server error" },
            {"10410", "Internal server error" },
            {"30010", "Possibly invalid API key" } //Not in the documentation but seems to be what happen when using the wrong key        
        };
        #endregion

        private HandleEventResult Post<T>(AffiliateReportingEventModelBase evt, bool isOutPayment, T messageData)
        {
            var settings = affiliateDataSource.GetSettings(evt.ProviderName);
            var custom = settings.ReadCustomSettingsAs<EoneSettingsModel>();

            object payload = null;
            if (isOutPayment)
            {
                payload = new
                {
                    APIKey = custom.OutpaymentApiKey,
                    OutPayment = messageData
                };
            }
            else
            {
                payload = new
                {
                    APIKey = custom.DecisionApiKey,
                    Decision = messageData
                };
            }

            string jsonRequest = null;
            return SendJsonRequest(payload,
                HttpMethod.Post, isOutPayment ? custom.OutpaymentUrl : custom.DecisionUrl, r =>
                {
                    if (r.IsSuccessStatusCode)
                    {
                        string jsonResponse = null;
                        return HandleJsonResponseAsType<EoneResponse>(
                            r,
                            p =>
                            ((p?.Status ?? "").ToLowerInvariant() == "ok")
                                ? Success(message: $"Success", outgoingRequestBody: jsonRequest, outgoingResponseBody: jsonResponse)
                                : Failed(message: $"Affiliate serverside error: {p.ErrorCode} - {p.InferErrorMessage(isOutPayment)}", outgoingRequestBody: jsonRequest, outgoingResponseBody: jsonResponse),
                            observeJsonResponse: x => jsonResponse = x);
                    }
                    else
                    {
                        string contentType = "";
                        var jsonOrHtmlResponse = ReadJsonOrHtmlBodyIfAny(r, x => x, x => x, x => contentType = $" - '{x}'");
                        return Failed(message: $"Affiliate serverside error. {(int)r.StatusCode} - {r.ReasonPhrase}{contentType}", outgoingRequestBody: jsonRequest, outgoingResponseBody: jsonOrHtmlResponse);
                    }
                }, observeJsonRequest: x => jsonRequest = x);
        }

        private string F<T>(T? input, Func<T, string> f) where T : struct
        {
            return input.HasValue ? f(input.Value) : null;
        }

        public enum EoneApiRejectionCode
        {
            PaymentDefaults, //Customer payment defaults
            ScoringDefaults, //Customer did not pass the scoring process
            OldCustomer, //Already a customer
            NoCoApplicant //Should try again with co-applicant
        }

        public enum EoneApiCanceledCode
        {
            Attachments, // Canceled after reviewing required attachments submitted by the customer
            Customer //Canceled by the customer
        }
    }
}