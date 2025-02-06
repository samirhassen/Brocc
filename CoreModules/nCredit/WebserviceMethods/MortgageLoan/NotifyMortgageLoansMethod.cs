using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
namespace nCredit.WebserviceMethods
{
    public class NotifyMortgageLoansMethod : TypedWebserviceMethod<NotifyMortgageLoansMethod.Request, NotifyMortgageLoansMethod.Response>
    {
        public override string Path => "MortgageLoans/Notify";

        public override bool IsEnabled => NEnv.IsMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Func<string, string> getSchedulerData = s => (request?.SchedulerData != null && request.SchedulerData.ContainsKey(s)) ? request?.SchedulerData[s] : null;
            var skipDeliveryExport = request.SkipDeliveryExport.GetValueOrDefault()
                || getSchedulerData("skipDeliveryExport") == "true";
            var useDelayedDocuments = request.UseDelayedDocuments.GetValueOrDefault() || getSchedulerData("useDelayedDocuments") == "true";
            var skipNotify = request.SkipNotify.GetValueOrDefault() || getSchedulerData("skipNotify") == "true";

            var c = requestContext.Service().DocumentClientHttpContext;
            return CreditContext.RunWithExclusiveLock("ntech.scheduledjobs.createmortgageloannotifications",
                () =>
                    {
                        var service = requestContext.Service().GetNotificationService(useDelayedDocuments);
                        var r = service.CreateNotifications(
                            skipDeliveryExport,
                            skipNotify);

                        return new Response
                        {
                            SuccessCount = r.SuccessCount,
                            Errors = r.Errors,
                            FailCount = r.FailCount,
                            TotalMilliseconds = r.TotalMilliseconds,
                            Warnings = r.Warnings
                        };
                    },
                    () => Error("Job is already running", httpStatusCode: 400, errorCode: "jobIsAlreadyRunning"));
        }

        public class Request
        {
            public bool? SkipDeliveryExport { get; set; }
            public bool? SkipNotify { get; set; }
            public bool? UseDelayedDocuments { get; set; }
            public IDictionary<string, string> SchedulerData { get; set; }
        }

        public class Response
        {
            public int SuccessCount { get; set; }
            public int FailCount { get; set; }
            public List<string> Errors { get; set; }
            public long TotalMilliseconds { get; set; }
            public List<string> Warnings { get; set; }
        }
    }
}