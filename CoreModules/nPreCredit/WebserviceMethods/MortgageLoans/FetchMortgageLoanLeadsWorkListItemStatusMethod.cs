using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods
{
    public class FetchMortgageLoanLeadsWorkListItemStatusMethod : TypedWebserviceMethod<FetchMortgageLoanLeadsWorkListItemStatusMethod.Request, FetchMortgageLoanLeadsWorkListItemStatusMethod.Response>
    {
        public override string Path => "MortgageLoan/Fetch-Leads-WorkList-Item-Status";

        public override bool IsEnabled => NEnv.IsOnlyNonStandardMortgageLoansEnabled;

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
            var r = requestContext.Resolver();

            var s = r.Resolve<IWorkListService>();
            var state = s.GetWorkListWithUserState(request.WorkListId.Value, userId);
            if (state == null)
                return Error("No such worklist exists", errorCode: "noSuchWorkListExists");

            return new Response
            {
                WorkListHeaderId = state.WorkListHeaderId,
                ItemId = request.ItemId,
                CompletedCount = state.CompletedCount,
                CurrentUserId = userId,
                IsTakenByCurrentUser = state.CurrentUserActiveItemId == request.ItemId,
                IsTakePossible = state.IsTakePossible,
                TakenCount = state.TakenCount,
                TakeOrCompletedByCurrentUserCount = state.TakeOrCompletedByCurrentUserCount,
                TotalCount = state.TotalCount,
                CurrentUserActiveItemId = state.CurrentUserActiveItemId
            };
        }

        public class Request
        {
            public int? UserId { get; set; }
            public bool? UseCurrentUserId { get; set; }

            [Required]
            public int? WorkListId { get; set; }

            [Required]
            public string ItemId { get; set; }
        }

        public class Response
        {
            public string ItemId { get; set; }
            public int WorkListHeaderId { get; set; }
            public int CompletedCount { get; set; }
            public int TakenCount { get; set; }
            public int TotalCount { get; set; }
            public int CurrentUserId { get; set; }
            public bool IsTakenByCurrentUser { get; set; }
            public int TakeOrCompletedByCurrentUserCount { get; set; }
            public bool IsTakePossible { get; set; }
            public string CurrentUserActiveItemId { get; set; }

            /*
             *TakeOrCompletedByCurrentUserCount
             * TakenCount
             *CompletedCount
             * TotalCount
             */
            //public int? ClosedByUserId { get; set; }
            //public string ClosedByUserDisplayName { get; set; }
            //public DateTime CreationDate { get; set; }
            //public int CreatedByUserId { get; set; }
            //public string CreatedByUserDisplayName { get; set; }
            //public DateTime? ClosedDate { get; set; }
            //public int TotalCount { get; set; }

            //public bool IsUnderConstruction { get; set; }
        }
    }
}