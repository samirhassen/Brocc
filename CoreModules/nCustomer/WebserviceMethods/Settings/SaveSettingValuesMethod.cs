using nCustomer.Code;
using nCustomer.Code.Services.Settings;
using Newtonsoft.Json;
using NTech.Core.Customer.Shared.Settings;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace nCustomer.WebserviceMethods
{
    public class SaveSettingValuesMethod : TypedWebserviceMethod<SaveSettingValuesMethod.Request, SaveSettingValuesMethod.Response>
    {
        public override string Path => "Settings/SaveValues";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var keyValueStore = requestContext.Service().KeyValueStore;
            var user = requestContext.CurrentUserMetadata();
            var service = new SettingsService(
                SettingsModelSource.GetSharedSettingsModelSource(NEnv.ClientCfgCore), requestContext.Service().KeyValueStore, user.CoreUser,
                NEnv.ClientCfgCore, EventSubscriberBase.BroadcastCrossServiceEvent);

            var isSystemUser = requestContext.CurrentUserIdentity.FindFirst("ntech.issystemuser")?.Value == "true";
            var groupMemberships = requestContext.CurrentUserIdentity.FindAll("ntech.group").Select(x => x.Value).ToHashSetShared();

            try
            {
                service.SaveSettingsValues(request.SettingCode, request.SettingValues, (IsSystemUser: isSystemUser, GroupMemberships: groupMemberships));
                return new Response
                {
                    IsSaved = true
                };
            }
            catch(NTechCoreWebserviceException ex)
            {
                if (ex.ErrorCode == "saveSettingValuesValidationError")
                {
                    return new Response
                    {
                        IsSaved = false,
                        ValidationErrors = ex.Message.ReadAllLines()
                    };
                }
                else
                    throw;
            }
        }

        public class Request
        {
            [Required]
            public string SettingCode { get; set; }
            [Required]
            public Dictionary<string, string> SettingValues { get; set; }
        }

        public class Response
        {
            public bool IsSaved { get; set; }
            public List<string> ValidationErrors { get; set; }
        }
    }
}