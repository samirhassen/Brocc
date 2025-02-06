using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.WebserviceMethods
{
    public class FetchApplicationAssignedHandlersMethod : TypedWebserviceMethod<FetchApplicationAssignedHandlersMethod.Request, FetchApplicationAssignedHandlersMethod.Response>
    {
        public override string Path => "ApplicationAssignedHandlers/Fetch";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var applicationNr = string.IsNullOrWhiteSpace(request.ApplicationNr) ? null : request.ApplicationNr;
            if (request.ReturnAssignedHandlers.GetValueOrDefault() && applicationNr == null)
                return Error("ApplicationNr required when using ReturnAssignedHandlers", errorCode: "returnAssignedHandlersRequiresApplicationNr");

            var r = requestContext.Resolver();

            var assignedHandlerService = r.Resolve<IApplicationAssignedHandlerService>();

            var toResponse = HandlerResponseModel.CreateFactory(r.Resolve<IUserDisplayNameService>());

            List<HandlerResponseModel> assignedHandlers = null;
            if (request.ReturnAssignedHandlers.GetValueOrDefault())
            {
                var assignedHandlerUserIds = assignedHandlerService.GetAssignedHandlerUserIds(request.ApplicationNr);
                assignedHandlers = toResponse(assignedHandlerUserIds);
            }

            List<HandlerResponseModel> possibleHandlers = null;
            if (request.ReturnPossibleHandlers.GetValueOrDefault())
            {
                var possibleHandlerUserIds = assignedHandlerService.GetPossibleHandlerUserIds();
                possibleHandlers = toResponse(possibleHandlerUserIds);
            }

            return new Response
            {
                AssignedHandlers = assignedHandlers,
                PossibleHandlers = possibleHandlers
            };
        }

        public class Response
        {
            public List<HandlerResponseModel> AssignedHandlers { get; set; }
            public List<HandlerResponseModel> PossibleHandlers { get; set; }
        }

        public class Request
        {
            public string ApplicationNr { get; set; }
            public bool? ReturnAssignedHandlers { get; set; }
            public bool? ReturnPossibleHandlers { get; set; }

        }
    }

    public class HandlerResponseModel
    {
        public int UserId { get; set; }
        public string UserDisplayName { get; set; }

        public static Func<IEnumerable<int>, List<HandlerResponseModel>> CreateFactory(IUserDisplayNameService d)
        {
            var displayNameByUserId = d.GetUserDisplayNamesByUserId();
            Func<IEnumerable<int>, List<HandlerResponseModel>> toResponse = x =>
            {
                return (x ?? new List<int>()).Select(y => new HandlerResponseModel
                {
                    UserDisplayName = displayNameByUserId?.Opt(y.ToString()) ?? $"User {y}",
                    UserId = y
                }).ToList();
            };
            return toResponse;
        }
    }
}