using System.Collections.Generic;
using nSavings.Code;
using nSavings.Controllers.Api;
using nSavings.DbModel;
using NTech.Services.Infrastructure.NTechWs;

namespace nSavings.WebserviceMethods
{
    public class CreateAndSendFinnishCustomsExportMethod : TypedWebserviceMethod<
        CreateAndSendFinnishCustomsExportMethod.Request, CreateAndSendFinnishCustomsExportMethod.Response>
    {
        public override string Path => "FinnishCustomsAccounts/CreateExportFile";

        public override bool IsEnabled =>
            NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.savingsCustomsAccountsExport.v1");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            return SavingsContext.RunWithExclusiveLock("ntech.scheduledjobs.createsavingscustomsaccountsexport",
                () =>
                {
                    string archiveKey = null;
                    var errors = new List<string>();
                    requestContext.Service().FinnishCustomsAccounts(requestContext.CurrentUserMetadataCore())
                        .CreateAndDeliverUpdate(
                            requestContext.CurrentUserMetadataCore(),
                            observeArchiveKey: x => archiveKey = x,
                            skipDeliver: request.SkipDeliver,
                            observeError: errors.Add);

                    return new Response
                    {
                        ArchiveKey = archiveKey,
                        Errors = errors,
                        Warnings = new List<string>()
                    };
                },
                () => Error("Job is already running", httpStatusCode: 400, errorCode: "jobIsAlreadyRunning"));
        }

        public class Request
        {
            public bool? SkipArchive { get; set; }
            public bool? SkipDeliver { get; set; }
        }

        public class Response
        {
            public string UpdateModelJson { get; set; }
            public string UpdateFileJson { get; set; }
            public string ArchiveKey { get; set; }
            public List<string> Errors { get; internal set; }
            public List<string> Warnings { get; internal set; }
        }
    }
}