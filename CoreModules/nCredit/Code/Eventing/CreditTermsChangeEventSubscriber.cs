using Newtonsoft.Json;
using NTech.Services.Infrastructure.Eventing;
using System;
using System.Threading;

namespace nCredit.Code
{
    public class CreditTermsChangeEventSubscriber : EventSubscriberBase, IEventSubscriber
    {
        public void OnStartup(Func<string, Action<string, CancellationToken>, string> subscribe)
        {
            Subscribe(CreditEventCode.CreditChangeTermsAgreementSigned, OnCreditChangeTermsAgreementSigned, subscribe);
        }

        public void OnCreditChangeTermsAgreementSigned(string data, CancellationToken ct)
        {
            var d = JsonConvert.DeserializeAnonymousType(data, new { token = (string)null, eventName = (string)null, errorMessage = (string)null, isMortgageLoan = new bool?() });
            if (!string.IsNullOrWhiteSpace(d?.eventName) && !string.IsNullOrWhiteSpace(d?.token))
            {
                BeginCallSelf().PostJson(
                    d?.isMortgageLoan == true 
                    ? "Api/MortgageLoans/ChangeTerms/HandleSignatureEvent"
                    : "Api/Credit/ChangeTerms/HandleSignatureEvent", d);
            }
        }
    }
}