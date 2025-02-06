using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
namespace nCredit.WebserviceMethods
{



    public class CreateDirectDebitNotificationsDeliveryMethod : TypedWebserviceMethod<CreateDirectDebitNotificationsDeliveryMethod.Request, CreateDirectDebitNotificationsDeliveryMethod.Response>
    {



        public override string Path => "DirectDebit/CreateNotificationsDelivery";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            if (!NEnv.IsDirectDebitPaymentsEnabled)
            {
                return new Response
                {
                    Warnings = new List<string> { "Direct debit not enabled. No delivery attempted." }
                };
            }

            return CreditContext.RunWithExclusiveLock("ntech.scheduledjobs.createdirectdebitdelivery",
                () =>
                    {
                        var s = requestContext.Service().DirectDebitNotificationDeliveryService;

                        var errors = new List<string>();

                        var r = s.CreateDelivery();

                        return new Response
                        {
                            Errors = r.Errors,
                        };
                    },
                    () => Error("Job is already running", httpStatusCode: 400, errorCode: "jobIsAlreadyRunning"));
        }

        public class Request
        {
            public IDictionary<string, string> SchedulerData { get; set; }
        }

        public class Response
        {
            public List<string> Errors { get; set; }
            public List<string> Warnings { get; set; }
        }
    }
}