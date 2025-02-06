using AutoMapper;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.MortgageLoans;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;

namespace nPreCredit.WebserviceMethods
{
    public class FetchMortgageLoanLeadsWorkListStatusesMethod : TypedWebserviceMethod<FetchMortgageLoanLeadsWorkListStatusesMethod.Request, FetchMortgageLoanLeadsWorkListStatusesMethod.Response>
    {
        public override string Path => "MortgageLoan/Fetch-Leads-WorkList-Statuses";

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

            var activeWorkLists = r.Resolve<IWorkListService>().GetActiveWorkListsWithUserState(MortgageLoanLeadsWorkListService.WorkListName, userId);
            var responseLists = new List<Response.WorkListModel>(activeWorkLists.Count);
            var displayNameService = r.Resolve<IUserDisplayNameService>();

            foreach (var a in activeWorkLists)
            {
                var m = Mapper.Map<Response.WorkListModel>(a);
                m.CreatedByUserDisplayName = displayNameService.GetUserDisplayNameByUserId(m.CreatedByUserId.ToString());
                m.ClosedByUserDisplayName = m.ClosedByUserId.HasValue
                    ? displayNameService.GetUserDisplayNameByUserId(m.ClosedByUserId.Value.ToString())
                    : null;
                responseLists.Add(m);
            }

            return new Response
            {
                WorkLists = responseLists
            };
        }

        public class Request
        {
            public int? UserId { get; set; }
            public bool? UseCurrentUserId { get; set; }
        }

        public class Response
        {
            public List<WorkListModel> WorkLists { get; set; }

            public class WorkListModel
            {
                public int WorkListHeaderId { get; set; }
                public int? ClosedByUserId { get; set; }
                public string ClosedByUserDisplayName { get; set; }
                public DateTime CreationDate { get; set; }
                public int CreatedByUserId { get; set; }
                public string CreatedByUserDisplayName { get; set; }
                public DateTime? ClosedDate { get; set; }
                public int TotalCount { get; set; }
                public int CompletedCount { get; set; }
                public string CurrentUserActiveItemId { get; set; }
                public int TakenCount { get; set; }
                public int TakeOrCompletedByCurrentUserCount { get; set; }
                public bool IsTakePossible { get; set; }
                public bool IsUnderConstruction { get; set; }
            }
        }
    }

    public class FetchMortgageLoanLeadsWorkListStatusesMethodAutoMapperProfile : Profile
    {
        public FetchMortgageLoanLeadsWorkListStatusesMethodAutoMapperProfile()
        {
            CreateMap<WorkListStatusModel, FetchMortgageLoanLeadsWorkListStatusesMethod.Response.WorkListModel>();
        }
    }
}