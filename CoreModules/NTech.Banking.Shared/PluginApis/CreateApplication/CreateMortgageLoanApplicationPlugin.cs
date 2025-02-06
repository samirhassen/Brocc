using System;

namespace NTech.Banking.PluginApis.CreateApplication
{
    public abstract class CreateMortgageLoanApplicationPlugin<TRequest> : CreateApplicationPluginBase
    {
        public abstract Tuple<bool, CreateApplicationRequestModel, Tuple<string, string>> TryTranslateRequest(TRequest request);
        
        public override Type RequestType => typeof(TRequest);
    }
}
