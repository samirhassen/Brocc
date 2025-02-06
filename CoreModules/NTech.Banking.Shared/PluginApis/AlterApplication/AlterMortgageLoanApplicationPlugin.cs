using System;

namespace NTech.Banking.PluginApis.AlterApplication
{
    public abstract class AlterMortgageLoanApplicationPlugin<TRequest> : AlterApplicationPluginBase
    {
        public abstract Tuple<bool, AlterApplicationRequestModel, Tuple<string, string>> TryTranslateRequest(TRequest request);

        public override Type RequestType => typeof(TRequest);
    }
}