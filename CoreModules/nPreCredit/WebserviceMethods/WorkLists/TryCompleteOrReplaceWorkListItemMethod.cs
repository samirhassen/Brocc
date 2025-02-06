using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods
{
    public class TryCompleteOrReplaceWorkListItemMethod : TypedWebserviceMethod<TryCompleteOrReplaceWorkListItemMethod.Request, TryCompleteOrReplaceWorkListItemMethod.Response>
    {
        public override string Path => "WorkLists/TryCompleteOrReplaceWorkListItem";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var s = requestContext.Resolver().Resolve<IWorkListService>();

            var wasReplaced = false;
            var wasCompleted = false;

            if (request.IsReplace.GetValueOrDefault())
                wasReplaced = s.TryReplaceWorkListItem(request.WorkListId.Value, request.ItemId);
            else
                wasCompleted = s.TryCompleteWorkListItem(request.WorkListId.Value, request.ItemId);

            return new Response
            {
                WasReplaced = wasReplaced,
                WasCompleted = wasCompleted
            };
        }

        public class Request
        {
            [Required]
            public int? WorkListId { get; set; }
            [Required]
            public string ItemId { get; set; }

            public bool? IsReplace { get; set; }
        }

        public class Response
        {
            public bool WasReplaced { get; set; }
            public bool WasCompleted { get; set; }
        }
    }
}