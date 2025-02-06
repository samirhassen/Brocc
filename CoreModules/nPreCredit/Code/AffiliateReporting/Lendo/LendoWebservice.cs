using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;

namespace nPreCredit.Code.AffiliateReporting.Lendo
{
    public class LendoWebservice : JsonAffiliateWebserviceBase, ILendoWebservice
    {
        private readonly IAffiliateDataSource affiliateDataSource;

        public LendoWebservice(IAffiliateDataSource affiliateDataSource)
        {
            this.affiliateDataSource = affiliateDataSource;
        }

        //By 'send agreement' they seem to mean all the applicants signed the agreement ... wierd name.
        public HandleEventResult ReportSendAgreement(CreditApplicationSignedAgreementEventModel evt)
        {
            return evt.AllApplicantsHaveNowSigned
                ? Post(evt, new LendoAgreementMessage())
                : Ignored();
        }

        public HandleEventResult ReportAcceptedApplication(CreditDecisionApprovedEventModel evt)
        {
            var offer = evt.GetSimplifiedOffer();
            return Post(evt, new LendoApprovedMessage
            {
                amount = (offer?.LoanAmount).GetValueOrDefault(),
                amortizeLength = (offer?.RepaymentTimeInMonths).GetValueOrDefault(),
                adminFee = (int)Math.Round((offer?.NotificationFeeAmount).GetValueOrDefault()),
                monthlyCost = (int)Math.Round((offer?.MontlyPaymentExcludingFees).GetValueOrDefault()),
                effectiveInterestRate = (offer?.EffectiveInterestRatePercent).GetValueOrDefault(),
                esign = evt.ApplicationUrl,
                interestRate = (offer?.InterestRatePercent).GetValueOrDefault(),
                setupFee = (int)Math.Round((offer?.InitialFeeAmount).GetValueOrDefault())
            });
        }

        public HandleEventResult ReportRejectedApplication(CreditApplicationRejectedEventModel evt)
        {
            return Post(evt, new LendoRejectedMessage
            {
                reason = evt.IsRejectedDueToPaymentRemark() ? "PaymentRemark" : "OtherScoring"
            });
        }

        public HandleEventResult ReportLoanPaidToCustomer(LoanPaidOutEventModel evt)
        {
            return Post(evt, new LendoLoanPaidOutMessage
            {
                amount = evt.PaymentAmount,
                paidOutDate = evt.PaymentDate.ToString("yyyy-MM-dd")
            });
        }

        private HandleEventResult Post<T>(AffiliateReportingEventModelBase evt, T messageData) where T : LendoMessageBase
        {
            var settings = affiliateDataSource.GetSettings(evt.ProviderName);
            var custom = settings.ReadCustomSettingsAs<LendoSettingsModel>();

            var data = new ExpandoObject();

            //NOTE: The reason we dont map externalId to externalId is that
            //      when we say 'external' we mean lendo but when they say 'external' they mean us. 
            //      What we call 'externalId' they call containerId in their api.
            messageData.externalId = evt.ApplicationNr;

            var k = ParseApiKey(custom.ApiKey);

            messageData.token = k.Token;

            var dd = data as IDictionary<string, object>;
            dd[messageData.messageName()] = messageData;

            string jsonRequest = null;
            return SendJsonRequest(
                data, HttpMethod.Post, NTechServiceRegistry.CreateUrl(new Uri(custom.Url), "bank_api/json").ToString(), r =>
                {
                    if (IsJsonResponse(r))
                    {
                        string jsonResponse = null;
                        return HandleJsonResponseAsType<LendoResponse>(r, rr =>
                        {
                            var isError = (rr?.result?.status?.Equals("ERROR", StringComparison.OrdinalIgnoreCase) ?? false);
                            if (isError)
                                return Failed(message: $"Affiliate serverside error. {(int)r.StatusCode} - {rr.result.error}", outgoingRequestBody: jsonRequest, outgoingResponseBody: jsonResponse);
                            else
                                return Success(message: $"Success", outgoingRequestBody: jsonRequest, outgoingResponseBody: jsonResponse);
                        }, observeJsonResponse: x => jsonResponse = x, allowNonSuccessStatusCode: true);
                    }
                    else
                    {
                        string contentType = "";
                        var jsonOrHtmlResponse = ReadJsonOrHtmlBodyIfAny(r, x => x, x => x, x => contentType = $" - '{x ?? "no content type"}'");
                        return Failed(message: $"Affiliate serverside error. {(int)r.StatusCode} - {r.ReasonPhrase}{contentType}", outgoingRequestBody: jsonRequest, outgoingResponseBody: jsonOrHtmlResponse);
                    }
                },
                setupMessage: m => { if (k.HasBasicAuth) AddBasicAuthenticationHeader(m, k.Username, k.Password); },
                observeJsonRequest: x => jsonRequest = x
            );
        }

        private class LendoLoanPaidOutMessage : LendoMessageBase
        {
            public string paidOutDate { get; set; }
            public decimal amount { get; set; }
            public override string messageName()
            {
                return "loanPaidOutByExternalId";
            }
        }

        private class LendoApprovedMessage : LendoMessageBase
        {
            public decimal amount { get; set; }
            public int amortizeLength { get; set; }
            public decimal interestRate { get; set; }
            public decimal effectiveInterestRate { get; set; }
            public int monthlyCost { get; set; }
            public int setupFee { get; set; }
            public int adminFee { get; set; }
            public string esign { get; set; }

            public override string messageName()
            {
                return "approvedApplicationByExternalId";
            }
        }

        private class LendoRejectedMessage : LendoMessageBase
        {
            public string reason { get; set; }
            public override string messageName()
            {
                return "rejectedApplicationByExternalId";
            }
        }

        private class LendoAgreementMessage : LendoMessageBase
        {
            public override string messageName()
            {
                return "sentAgreementByExternalId";
            }
        }

        private class ApiKey
        {
            public string Token { get; set; }
            public bool HasBasicAuth { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
        }

        private ApiKey ParseApiKey(string keyRaw)
        {
            //So for some wierdass reason these guys have basic auth layered on top of their api login, but only in their dev environment
            var parts = keyRaw.Split(';');
            if (parts.Length == 3)
                return new ApiKey
                {
                    HasBasicAuth = true,
                    Token = parts[0],
                    Username = parts[1],
                    Password = parts[2]
                };
            else
                return new ApiKey
                {
                    HasBasicAuth = false,
                    Token = keyRaw
                };
        }

        private abstract class LendoMessageBase
        {
            public string externalId { get; set; }
            public string token { get; set; }
            public abstract string messageName();
        }

        private class LendoResponse
        {
            public class R
            {
                public string status { get; set; }
                public string error { get; set; }
            }

            public R result { get; set; }
        }
    }
}