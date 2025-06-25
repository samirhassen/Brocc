using System;
using System.Linq;
using System.Security.Claims;
using System.Web.Mvc;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using nUser.DbModel;

namespace nUser
{
    public abstract class NController : Controller
    {
        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (filterContext.Exception is NTechCoreWebserviceException wsException && wsException.IsUserFacing)
            {
                var result = new JsonNetActionResult
                {
                    Data = new
                    {
                        errorMessage = wsException?.Message ?? "generic",
                        errorCode = wsException?.ErrorCode ?? "generic"
                    },
                    CustomHttpStatusCode = wsException?.ErrorHttpStatusCode ?? 500
                };

                filterContext.Result = result;
                filterContext.ExceptionHandled = true;
            }

            base.OnActionExecuted(filterContext);
        }

        protected ActionResult Json2(object data)
        {
            return new JsonNetActionResult
            {
                Data = data
            };
        }

        protected int CurrentUserId
        {
            get
            {
                var u = User.Identity as ClaimsIdentity;
                return int.Parse(u.FindFirst("ntech.userid").Value);
            }
        }

        public INTechCurrentUserMetadata GetCurrentUserMetadataCore() =>
            new NTechCurrentUserMetadataImpl(User.Identity as ClaimsIdentity);

        protected IQueryable<GroupMembership> UncancelledGroupsOnly(IQueryable<GroupMembership> g)
        {
            return g.Where(x => !x.GroupMembershipCancellation.Any(y => y.CommittedById.HasValue));
        }

        protected IQueryable<GroupMembership> ActiveGroupsOnly(IQueryable<GroupMembership> g)
        {
            return g.Where(x =>
                !x.GroupMembershipCancellation.Any(y => y.CommittedById.HasValue)
                && x.ApprovedDate.HasValue
                && x.StartDate < DateTime.Now
                && x.EndDate > DateTime.Now
            );
        }

        protected IQueryable<GroupMembership> GroupsAwaitingCancellationCommitOnly(IQueryable<GroupMembership> g)
        {
            return g.Where(x => x.GroupMembershipCancellation.Any(y => !y.CancellationEndDate.HasValue));
        }
    }
}