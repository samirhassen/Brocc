using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace nPreCredit.Code.AffiliateReporting.Telefinans
{

    public class TelefinansWebservice : JsonAffiliateWebserviceBase, ITelefinansWebservice
    {
        private readonly IAffiliateDataSource affiliateDataSource;

        public TelefinansWebservice(IAffiliateDataSource affiliateDataSource)
        {
            this.affiliateDataSource = affiliateDataSource;
        }

        public HandleEventResult ApprovedV2(CreditDecisionApprovedEventModel evt)
        {
            var a = evt.GetSimplifiedOffer();

            Func<decimal?, int?> round = x => x.HasValue ? (int)Math.Round(x.Value) : new int?();

            return Put(new
            {
                transactionId = int.Parse(evt.ProviderApplicationId),
                bankInternalRefId = evt.ApplicationNr,
                bankStatus = "Granted",
                caseStatus = "APPROVED",
                message = $"The application is approved. Continue the signing process on {evt.ApplicationUrl}",
                offer = new
                {
                    amount = a.LoanAmount,
                    nominalInterest = a.InterestRatePercent,
                    effectiveInterest = a.EffectiveInterestRatePercent,
                    installmentFee = a.NotificationFeeAmount,
                    monthlyCost = a.MontlyPaymentExcludingFees,
                    setupFee = a.InitialFeeAmount,
                    repaymentPeriod = a.RepaymentTimeInMonths,
                    dueDay = 28,
                    maxAmount = round(a.LoanAmount.HasValue ? (int)Math.Round(a.LoanAmount.Value) : new int?())
                }
            }, evt.ProviderName);
        }

        public HandleEventResult Completed(LoanPaidOutEventModel evt)
        {
            return Put(new
            {
                transactionId = int.Parse(evt.ProviderApplicationId),
                bankInternalRefId = evt.ApplicationNr,
                bankStatus = "Paid",
                caseStatus = "COMPLETED",
                message = "The loan is paid out."
            }, evt.ProviderName);
        }

        public HandleEventResult Rejected(CreditApplicationRejectedEventModel evt)
        {
            return Put(new
            {
                transactionId = int.Parse(evt.ProviderApplicationId),
                bankInternalRefId = evt.ApplicationNr,
                bankStatus = "Denied",
                caseStatus = "REJECTED",
                message = "The application is rejected."
            }, evt.ProviderName);
        }

        public HandleEventResult Validated(CreditApplicationSignedAgreementEventModel evt)
        {
            return Put(new
            {
                transactionId = int.Parse(evt.ProviderApplicationId),
                bankInternalRefId = evt.ApplicationNr,
                bankStatus = "Sent",
                caseStatus = "VALIDATED",
                message = "The application has been validated, all documents has been received."
            }, evt.ProviderName);
        }

        private HandleEventResult Put<T>(T payload, string providerName)
        {
            var settings = affiliateDataSource.GetSettings(providerName);
            var custom = settings.ReadCustomSettingsAs<TelefinansCustomSettingsModel>();
            string jsonRequest = null;

            return SendJsonRequest(payload, HttpMethod.Put, custom.Url, response =>
            {
                var code = (int)response.StatusCode;
                if (response.IsSuccessStatusCode)
                    return Success(outgoingRequestBody: jsonRequest);
                else if (code == 429)
                    return Pending(message: "Failed because of too many recent requests. Waiting 30 minutes and trying again.", waitUntilNextAttempt: TimeSpan.FromMinutes(30), outgoingRequestBody: jsonRequest);
                else if (code == 401)
                    return Failed(message: "Invalid login credentials. Contact telefinans and ask for new a new webservice login.", outgoingRequestBody: jsonRequest);
                else
                    return HandleFailed(response, jsonRequest);
            }, m => m.Headers.Add("api_key", custom.ApiKey), observeJsonRequest: x => jsonRequest = x);
        }

        private HandleEventResult HandleFailed(HttpResponseMessage r, string jsonRequest)
        {
            var prefix = r.StatusCode == System.Net.HttpStatusCode.BadRequest ? "The provider considers the request invalid." : $"Unknown error from provider - {(int)r.StatusCode}.";
            if (IsJsonResponse(r) && HasResponseBody(r))
                return HandleJsonResponseRaw(r, x =>
                {
                    var p = ParseMessageAndModelStates(x);
                    var extra = p.Item1 ?? "";
                    if (p.Item2.Count > 0)
                        extra += " " + string.Join(", ", p.Item2.SelectMany(y => y.Value));
                    extra = extra.Length == 0 ? "" : $"({extra})";
                    return Failed($"{prefix} {extra}".Trim(), outgoingRequestBody: jsonRequest, outgoingResponseBody: x);
                }, allowNonSuccessStatusCode: true);
            else
                return Failed(prefix, outgoingRequestBody: jsonRequest);
        }

        private Tuple<string, Dictionary<string, List<string>>> ParseMessageAndModelStates(string json)
        {
            /* Example json
                {
                    "Message": "The request is invalid.",
                    "ModelState": {
                        "bankReportUpdate.transactionId": ["TransactionId does not point to an existing transaction"]
                    }
                }     
             */
            try
            {
                var root = JObject.Parse(json);
                var message = root.SelectToken("Message")?.Value<string>();

                Dictionary<string, List<string>> d = new Dictionary<string, List<string>>();
                var modelState = root.SelectToken("ModelState");
                foreach (var p in modelState.Children<JProperty>())
                {
                    var a = p?.Value as JArray;
                    if (a != null)
                    {
                        d[p.Name] = a.Values<string>().ToList();
                    }
                }
                return Tuple.Create(message, d.Count > 0 ? d : null);
            }
            catch
            {
                return Tuple.Create((string)null, (Dictionary<string, List<string>>)null);
            }
        }
    }

    public class TelefinansCustomSettingsModel
    {
        public string ApiKey { get; set; }
        public string Url { get; set; }
    }
}
