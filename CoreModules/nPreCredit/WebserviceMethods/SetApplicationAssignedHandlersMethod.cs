using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods
{
    public class SetApplicationAssignedHandlersMethod : TypedWebserviceMethod<SetApplicationAssignedHandlersMethod.Request, SetApplicationAssignedHandlersMethod.Response>
    {
        public override string Path => "ApplicationAssignedHandlers/Set";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);
            var r = requestContext.Resolver();

            var handlersWithIsAssigned = new List<(int UserId, bool IsAssigned)>();

            foreach (var userId in request.AssignHandlerUserIds ?? new List<int>())
            {
                handlersWithIsAssigned.Add((userId, true));
            }

            foreach (var userId in request.UnAssignHandlerUserIds ?? new List<int>())
            {
                handlersWithIsAssigned.Add((userId, false));
            }

            var allAssignedUserIds =
                r.Resolve<IApplicationAssignedHandlerService>().ChangeAssignedHandlerStateForUsers(request.ApplicationNr, handlersWithIsAssigned);

            var toResponse = HandlerResponseModel.CreateFactory(r.Resolve<IUserDisplayNameService>());

            return new Response
            {
                AllAssignedHandlers = toResponse(allAssignedUserIds)
            };
        }

        public class Response
        {
            public List<HandlerResponseModel> AllAssignedHandlers { get; set; }

            public class HandlerModel
            {
                public int UserId { get; set; }
                public string UserDisplayName { get; set; }
            }
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            public List<int> AssignHandlerUserIds { get; set; }

            public List<int> UnAssignHandlerUserIds { get; set; }
        }
    }
}