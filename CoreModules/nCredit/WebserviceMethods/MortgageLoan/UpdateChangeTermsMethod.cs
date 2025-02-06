using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
namespace nCredit.WebserviceMethods
{
    public class UpdateChangeTermsMethod : TypedWebserviceMethod<UpdateChangeTermsMethod.Request, UpdateChangeTermsMethod.Response>
    {
        public override string Path => "MortgageLoans/UpdateChangeTerms";

        public override bool IsEnabled => NEnv.IsMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {

            var c = requestContext.Service().DocumentClientHttpContext;
            return CreditContext.RunWithExclusiveLock("ntech.scheduledjobs.updatemortgageloanschangeterms",
                () =>
                    {
                        var service = requestContext.Service().CreateMortgageLoansChangeTermsService(requestContext.CurrentUserMetadata());

                        var r = service.UpdateChangeTerms();

                        return new Response
                        {
                            SuccessCount = r.NrOfTermChangesDone,
                            Errors = r.Errors,
                            FailCount = r.Errors.Count,
                            TotalMilliseconds = r.TotalMilliseconds,
                            Warnings = r.Warnings
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
            public int SuccessCount { get; set; }
            public int FailCount { get; set; }
            public List<string> Errors { get; set; }
            public long TotalMilliseconds { get; set; }
            public List<string> Warnings { get; set; }
        }
    }
}