using Newtonsoft.Json;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class FetchApplicationConsentAnswersMethod : TypedWebserviceMethod<FetchApplicationConsentAnswersMethod.Request, FetchApplicationConsentAnswersMethod.Response>
    {
        public override string Path => "Application/FetchConsentAnswers";


        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            using (var context = new PreCreditContext())
            {
                var consentText =
                    context
                    .CreditApplicationItems
                    .Where(x => x.ApplicationNr == request.ApplicationNr && x.Name == "consentRawJson")
                    .Select(x => new ConsentModel { ApplicationNr = x.ApplicationNr, GroupName = x.GroupName, Item = x.Value })
                    .ToList();

                return new Response
                {
                    ConsentItems = consentText
                };
            }
        }


        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
        }

        public class Response
        {
            public List<ConsentModel> ConsentItems { get; set; }
        }

        public class ConsentModel
        {
            public string ApplicationNr { get; set; }
            public string GroupName { get; set; }
            public string Item { get; set; }
        }

    }

}