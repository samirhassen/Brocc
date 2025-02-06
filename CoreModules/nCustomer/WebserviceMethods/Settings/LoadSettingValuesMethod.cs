using nCustomer.Code;
using nCustomer.Code.Services.Settings;
using Newtonsoft.Json;
using NTech.Core.Customer.Shared.Settings;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nCustomer.WebserviceMethods
{
    public class LoadSettingValuesMethod : TypedWebserviceMethod<LoadSettingValuesMethod.Request, LoadSettingValuesMethod.Response>
    {
        public override string Path => "Settings/LoadValues";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var service = new SettingsService(SettingsModelSource.GetSharedSettingsModelSource(
                NEnv.ClientCfgCore), requestContext.Service().KeyValueStore, requestContext.CurrentUserMetadata().CoreUser, NEnv.ClientCfgCore, EventSubscriberBase.BroadcastCrossServiceEvent);

            return new Response
            {
                SettingValues = service.LoadSettingsValues(request.SettingCode)
            };
        }

        public class Request
        {
            [Required]
            public string SettingCode { get; set; }
        }

        public class Response
        {
            public Dictionary<string, string> SettingValues { get; set; }
        }
    }
}