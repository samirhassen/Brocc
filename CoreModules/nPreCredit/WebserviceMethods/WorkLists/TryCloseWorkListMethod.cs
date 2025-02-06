using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods
{
    public class TryCloseWorkListItemMethod : TypedWebserviceMethod<TryCloseWorkListItemMethod.Request, TryCloseWorkListItemMethod.Response>
    {
        public override string Path => "WorkLists/TryCloseWorkList";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            int userId;
            if (request.UserId.HasValue)
                userId = request.UserId.Value;
            else if (request.UseCurrentUserId.GetValueOrDefault())
                userId = requestContext.CurrentUserMetadata().UserId;
            else
                return Error("UserId or UseCurrentUserId = true is required", errorCode: "missingUser");

            var s = requestContext.Resolver().Resolve<IWorkListService>();

            var wasClosed = s.TryCloseWorkList(request.WorkListId.Value, userId);

            return new Response
            {
                WasClosed = wasClosed
            };
        }

        public class Request
        {
            [Required]
            public int? WorkListId { get; set; }
            public int? UserId { get; set; }
            public bool? UseCurrentUserId { get; set; }
        }

        public class Response
        {
            public bool WasClosed { get; set; }
        }
    }
}