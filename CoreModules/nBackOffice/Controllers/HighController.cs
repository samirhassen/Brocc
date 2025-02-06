using nBackOffice.Code;
using NTech.Services.Infrastructure;
using System;
using System.Security.Claims;
using System.Web.Mvc;

namespace nBackOffice.Controllers
{
    [NTechAuthorizeCreditHigh]
    public class HighController : NController
    {
        private Func<int?> CreateGetUserId()
        {
            return () => (this.Identity as ClaimsIdentity)?.GetUserId();
        }

        private NHttp.NHttpCall BeginCallUser()
        {
            return NHttp.Begin(new Uri(NEnv.ServiceRegistry.Internal["nUser"]), NHttp.GetCurrentAccessToken());
        }

        private AttentionRepository CreateAttentionRepo()
        {
            return new AttentionRepository(
                this.User.IsInRole,
                CreateGetUserId(),
                this.Url);
        }

        public ActionResult Index()
        {
            return RedirectToAction("NavMenu", "Secure");
        }

        public ActionResult UserAdmin()
        {
            if (!NEnv.AllowAccessToLegacyUserAdmin)
                return HttpNotFound();

            var isPredCreditIncluded = NEnv.ServiceRegistry.ContainsService("nPreCredit");
            ViewBag.IsPredCreditIncluded = isPredCreditIncluded;
            return View();
        }

        public ActionResult CreateAdmin()
        {
            if (!NEnv.AllowAccessToLegacyUserAdmin)
                return HttpNotFound();

            var userId = CreateGetUserId()().Value;
            var u = new UserClient();
            ViewBag.JsonInitialData = this.EncodeInitialData(new { allUsers = u.GetAllUsers(), userId = userId });
            return View();
        }

        public class CreateAdminMembershipRequest
        {
            public int UserId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Product { get; set; }
            public string WsFedName { get; set; }
            public string WsFedProviderName { get; set; }
        }

        public class CreateGroupMembershipRequest
        {
            public int UserId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Group { get; set; }
            public string Product { get; set; }
            public bool IsIntegration { get; set; }
            public string WsFedName { get; set; }
            public string WsFedProviderName { get; set; }
        }

        [HttpPost]
        [NTechApi]
        public ActionResult CreateAdminMembership(CreateAdminMembershipRequest request)
        {
            if (!NEnv.AllowAccessToLegacyUserAdmin)
                return HttpNotFound();

            var result = BeginCallUser()
                .PostJson("GroupMembership/CreateAdmin", new CreateGroupMembershipRequest
                {
                    EndDate = request.EndDate,
                    StartDate = request.StartDate,
                    Group = "Admin",
                    IsIntegration = false,
                    Product = request.Product,
                    UserId = request.UserId,
                    WsFedName = request.WsFedName,
                    WsFedProviderName = request.WsFedProviderName
                })
                .ParseJsonAs<dynamic>();
            return Json(new { newGroup = result.Id });
        }

        [HttpPost]
        [NTechApi]
        public ActionResult FetchGroupsNeedingApproval()
        {
            if (!NEnv.AllowAccessToLegacyUserAdmin)
                return HttpNotFound();

            return Json(CreateAttentionRepo().FetchGroupsNeedingApproval());
        }

        [HttpPost]
        [NTechApi]
        public ActionResult CommitGroupmembershipCancellation(int groupMembershipId)
        {
            if (!NEnv.AllowAccessToLegacyUserAdmin)
                return HttpNotFound();

            var result = BeginCallUser()
                    .PostJson("GroupMembership/CommitCancellation", new
                    {
                        groupMembershipId = groupMembershipId
                    })
                    .ParseJsonAs<dynamic>();
            return Json(result);
        }

        [HttpPost]
        [NTechApi]
        public ActionResult UndoGroupmembershipCancellation(int groupMembershipId)
        {
            if (!NEnv.AllowAccessToLegacyUserAdmin)
                return HttpNotFound();

            var result = BeginCallUser()
                    .PostJson("GroupMembership/UndoCancellation", new
                    {
                        groupMembershipId = groupMembershipId
                    })
                    .ParseJsonAs<dynamic>();
            return Json(result);
        }

        [HttpPost]
        [NTechApi]
        public ActionResult FetchGroupMembershipCancellationsToCommit()
        {
            if (!NEnv.AllowAccessToLegacyUserAdmin)
                return HttpNotFound();
            return Json(CreateAttentionRepo().FetchGroupMembershipCancellationsToCommit());
        }

        [HttpPost]
        [NTechApi]
        public ActionResult FetchGroupsAboutToExpire()
        {
            if (!NEnv.AllowAccessToLegacyUserAdmin)
                return HttpNotFound();

            var u = new UserClient();
            return Json(u.FetchGroupsAboutToExpire(CreateGetUserId()));
        }

        [HttpPost]
        [NTechApi]
        public ActionResult HandleApproval(int id, bool isApproved)
        {
            if (!NEnv.AllowAccessToLegacyUserAdmin)
                return HttpNotFound();

            if (isApproved)
            {
                var result = BeginCallUser()
                    .PostJson("GroupMembership/Approve", new
                    {
                        groupMembershipId = id
                    })
                    .ParseJsonAs<dynamic>();
                return Json(result);
            }
            else
            {
                var result = BeginCallUser().PostJson("GroupMembership/Disapprove", new
                {
                    groupMembershipId = id
                }).ParseJsonAs<dynamic>();
                return Json(result);
            }
        }
    }
}