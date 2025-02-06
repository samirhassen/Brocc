using nCustomer.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCustomer.WebserviceMethods
{
    public class ArchiveCustomersBasedOnModuleCandidatesMethod : TypedWebserviceMethod<ArchiveCustomersBasedOnModuleCandidatesMethod.Request, ArchiveCustomersBasedOnModuleCandidatesMethod.Response>
    {
        public override string Path => "Archive/Based-On-Module-Candidates";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            var service = new CustomerArchiveService(requestContext.Clock(), requestContext.CurrentUserMetadata());
            if (!NEnv.ServiceRegistry.ContainsService(request.SourceModuleName))
                return Error("No such service", errorCode: "invalidSourceModuleName");

            var archivedCount = service.ArchiveCustomersBasedOnModuleCandidates(request.CandidateCustomerIds.ToHashSet(), "nPreCredit");

            return new Response
            {
                ArchivedCount = archivedCount
            };
        }

        public class Request
        {
            /// <summary>
            /// Source module name
            /// </summary>
            [Required]
            public string SourceModuleName { get; set; }

            /// <summary>
            /// CustomerIds that the the source module thinks are ok to archive
            /// </summary>
            [Required]
            public List<int> CandidateCustomerIds { get; set; }
        }

        public class Response
        {
            public int ArchivedCount { get; set; }
        }
    }
}