using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;

namespace nCustomer.WebserviceMethods
{
    public class SendKycReminderMethod : TypedWebserviceMethod<SendKycReminderMethod.Request, SendKycReminderMethod.Response>
    {
        public override string Path => "Kyc-Reminders/Send";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Lazy<bool> isSettingEnabled = new Lazy<bool>(() =>
            {
                var settings = requestContext.Service().Settings;
                return settings.LoadSettings("kycUpdateRequiredSecureMessage").Opt("isEnabled") == "true";
            });

            //NOTE: We do this instead of overriding IsEnabled so this service can be in scheduled jobs even when this is disabled.
            if (!NEnv.ClientCfgCore.IsFeatureEnabled("feature.customerpages.kyc") || !isSettingEnabled.Value)
            {
                return new Response
                {
                    Warnings = new List<string> { "Job disabled. Add feature feature.customerpages.kyc to enable it." }
                };
            }

            var service = requestContext.Service().KycQuestionsUpdate;

            service.SendReminderMessages(onlyConsiderCustomerIds: request?.OnlyConsiderCustomerIds?.ToHashSetShared());

            return new Response { };
        }

        public class Request
        {
            public List<int> OnlyConsiderCustomerIds { get; set; }
        }

        public class Response
        {
            public List<string> Warnings { get; set; }
        }
    }
}