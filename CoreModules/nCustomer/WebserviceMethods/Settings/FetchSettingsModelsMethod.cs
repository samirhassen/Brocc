using NTech.Core.Customer.Shared.Settings;
using NTech.Services.Infrastructure.NTechWs;

namespace nCustomer.WebserviceMethods
{
    public class FetchSettingsModelsMethod : TypedWebserviceMethod<FetchSettingsModelsMethod.Request, SettingsModel>
    {
        public override string Path => "Settings/FetchModels";

        protected override SettingsModel DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            return SettingsModelSource.GetSharedSettingsModelSource(NEnv.ClientCfgCore).GetSettingsModel();
        }

        public class Request
        {

        }
    }
}