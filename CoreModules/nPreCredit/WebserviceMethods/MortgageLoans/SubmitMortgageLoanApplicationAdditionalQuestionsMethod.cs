using Newtonsoft.Json;
using nPreCredit.Code.Plugins;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;

namespace nPreCredit.WebserviceMethods.MortgageLoans
{
    public class SubmitMortgageLoanApplicationAdditionalQuestionsMethod : RawRequestWebserviceMethod<SubmitMortgageLoanApplicationAdditionalQuestionsMethod.Response>
    {
        public override string Path => "MortgageLoan/Submit-AdditionalQuestions";

        public override bool IsEnabled => NEnv.IsOnlyNonStandardMortgageLoansEnabled;

        public override Type RequestType
        {
            get
            {
                return PluginMortgageLoanSubmitAdditionalQuestionsRequestTranslator.GetRequestType();
            }
        }

        protected override Response DoExecuteRaw(NTechWebserviceMethodRequestContext requestContext, string jsonRequest)
        {
            var request = JsonConvert.DeserializeObject(jsonRequest, RequestType);

            ValidateUsingAnnotations(request);

            var resolver = requestContext.Resolver();

            var tr = resolver.Resolve<PluginMortgageLoanSubmitAdditionalQuestionsRequestTranslator>();
            if (!tr.TranslateApplicationRequest(request, out var internalRequest, out var errorCodeAndMessage))
                return Error(errorCodeAndMessage.Item2, errorCode: errorCodeAndMessage.Item1);

            if (!resolver.Resolve<IMortgageLoanApplicationAlterationService>().TryAlterApplication(internalRequest, out var failedMessage))
                return Error(failedMessage);

            return new Response { };
        }

        public class Response
        {
        }
    }
}