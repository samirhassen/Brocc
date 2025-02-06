using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;

namespace nPreCredit.WebserviceMethods
{
    public class ArchiveSingleApplicationMethod : TypedWebserviceMethod<ArchiveSingleApplicationMethod.Request, ArchiveSingleApplicationMethod.Response>
    {
        public override string Path => "Application/ArchiveSingle";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, r =>
            {
                r.Require(x => x.ApplicationNr);
            });

            var maxAllowedArchiveLevel = request.MaxAllowedArchiveLevel ?? NEnv.MaxAllowedArchiveLevel;
            if (maxAllowedArchiveLevel != ApplicationArchiveService.ArchiveLevel)
                return Error("Invalid archiveLevel", errorCode: "invalidArchiveLevel");

            var s = requestContext.Resolver().Resolve<IApplicationArchiveService>();
            try
            {
                s.ArchiveApplications(new List<string> { request.ApplicationNr });

                return new Response();
            }
            catch (ServiceException ex)
            {
                if (!ex.IsUserSafeException)
                    throw;

                return Error(ex.Message, httpStatusCode: 400, errorCode: ex.ErrorCode);
            }
        }

        public class Response
        {

        }

        public class Request
        {
            public string ApplicationNr { get; set; }
            public int? MaxAllowedArchiveLevel { get; set; }
        }
    }
}