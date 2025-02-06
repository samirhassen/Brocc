using System;

namespace NTech.Banking.PluginApis.CreateApplication
{
    public abstract class CreateCompanyLoanApplicationPlugin<TRequest> : CreateApplicationPluginBase
    {
        public abstract Tuple<bool, CreateApplicationRequestModel, Tuple<string, string>> TryTranslateRequest(TRequest request);

        public Tuple<bool, CreateApplicationRequestModel, Tuple<string, string>> Sucess(CreateApplicationRequestModel r)
        {
            return Tuple.Create(true, r, (Tuple<string, string>)null);
        }

        public override Type RequestType => typeof(TRequest);
    }
}