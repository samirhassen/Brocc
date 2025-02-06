using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class FetchCompanyLoanApplicationAdditionalQuestionsAnswers : TypedWebserviceMethod<FetchCompanyLoanApplicationAdditionalQuestionsAnswers.Request, FetchCompanyLoanApplicationAdditionalQuestionsAnswers.Response>
    {
        public override string Path => "CompanyLoan/Fetch-AdditionalQuestions-Answers";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var d = GetAnswers(request.ApplicationNr, requestContext.Resolver().Resolve<IKeyValueStoreService>());

            return new Response
            {
                Document = d
            };
        }

        public static AdditionalQuestionsDocumentModel GetAnswers(string applicationNr, IKeyValueStoreService keyValueStoreService)
        {
            var dRaw = keyValueStoreService.GetValue(applicationNr, "additionalQuestionsDocument");
            return dRaw == null ? null : JsonConvert.DeserializeObject<AdditionalQuestionsDocumentModel>(dRaw);
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
        }

        public class Response
        {
            public AdditionalQuestionsDocumentModel Document { get; set; }
        }
    }
}