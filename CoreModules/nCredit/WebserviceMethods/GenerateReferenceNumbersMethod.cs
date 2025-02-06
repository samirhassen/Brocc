using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.WebserviceMethods
{
    public class GenerateReferenceNumbersMethod : TypedWebserviceMethod<GenerateReferenceNumbersMethod.Request, GenerateReferenceNumbersMethod.Response>
    {
        public override string Path => "Credit/Generate-Reference-Numbers";
        
        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            var r = new Response();
            var creditNrGenerator = new CreditNrGenerator(requestContext.Service().ContextFactory);
            if (request.CreditNrCount.GetValueOrDefault() > 0)
                r.CreditNrs = creditNrGenerator.GenerateNewCreditNrs(request.CreditNrCount.Value).ToList();

            if (request.OcrNrCount.GetValueOrDefault() > 0)
            {
                var ocrNrGenerator = requestContext.Service().CreateOcrPaymentReferenceGenerator(requestContext.CurrentUserMetadata());
                r.OcrNrs = Enumerable.Range(1, request.OcrNrCount.Value).Select(_ => ocrNrGenerator.GenerateNew().NormalForm).ToList();
            }

            return r;
        }

        public class Request
        {
            public int? CreditNrCount { get; set; }
            public int? OcrNrCount { get; set; }
        }

        public class Response
        {
            public List<string> CreditNrs { get; set; }
            public List<string> OcrNrs { get; set; }
        }
    }
}