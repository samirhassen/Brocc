using Newtonsoft.Json;
using NTech.Banking.Autogiro;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nCredit.WebserviceMethods
{
    public class GenerateDirectDebitPayerNumberMethod : TypedWebserviceMethod<GenerateDirectDebitPayerNumberMethod.Request, GenerateDirectDebitPayerNumberMethod.Response>
    {
        public override string Path => "DirectDebit/Generate-PayerNumber";

        public override bool IsEnabled => NEnv.IsDirectDebitPaymentsEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var clientBgNr = requestContext.Service().PaymentAccount.GetIncomingPaymentBankAccountNrRequireBankgiro();

            var payerNr = new AutogiroPaymentNumberGenerator().GenerateNr(request.CreditNr, request.ApplicantNr.Value);

            return new Response
            {
                ClientBankGiroNr = clientBgNr.NormalizedValue,
                PayerNr = payerNr
            };
        }


        public class Response
        {
            public string PayerNr { get; set; }
            public string ClientBankGiroNr { get; set; }
        }

        public class Request
        {
            /// <summary>
            /// NOTE: This credit does not have to exist
            /// </summary>
            [Required]
            public string CreditNr { get; set; }

            [Required]
            public int? ApplicantNr { get; set; }

        }
    }
}