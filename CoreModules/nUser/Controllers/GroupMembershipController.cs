using NTech.Services.Infrastructure;
using nUser.Code;
using nUser.DbModel;
using Serilog;
using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web.Mvc;

namespace nUser.Controllers
{
    [NTechApi]
    public class GroupMembershipController : NController
    {
        internal enum Groups
        {
            Low,
            Middle,
            High,
            Economy,
            Admin
        }

        internal static string[] AllGroupNames
        {
            get
            {
                return Enum.GetNames(typeof(Groups));
            }
        }

        [HttpPost]
        [NTechAuthorizeHigh]
        [NTechAuthorizeAndPermissions(Permissions = new[] { "editUserCommit" })]
        public ActionResult ApproveWithHigh(int groupMembershipId)
        {
            return Approve(groupMembershipId);
        }

        [HttpPost]
        [NTechAuthorizeAndPermissions(Permissions = new[] { "editUserCommit" })]
        public ActionResult Approve(int groupMembershipId)
        {
            //TODO: Remove and replace with ApproveWithHigh when removing the old backoffice ui
            Func<string, EmptyResult> badRequest = s =>
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                Response.StatusDescription = s;
                return new EmptyResult();
            };

            var userId = (this.User.Identity as ClaimsIdentity)?.FindFirst("ntech.userid")?.Value;

            if (userId == null)
            {
                throw new Exception("UI should not allow this");
            }

            var userIdInt = int.Parse(userId);

            using (var db = new UsersContext())
            {
                var groupMembership = db
                    .GroupMemberships
                    .SingleOrDefault(g => g.Id == groupMembershipId
                    && g.CreatedById != userIdInt //user is not allowed to approve group memberships they created themselves
                );
                if (groupMembership == null)
                    return badRequest("No such group membership.");

                groupMembership.ApprovedDate = DateTime.Now;
                groupMembership.ApprovedById = int.Parse(userId);
                db.SaveChanges();
            }
            return Json2(new { });
        }

        [HttpPost]
        [NTechAuthorizeAndPermissions(Permissions = new[] { "editUserCommit" })]
        [NTechAuthorizeHigh]
        public ActionResult DisapproveWithHigh(int groupMembershipId)
        {
            return Disapprove(groupMembershipId);
        }

        [HttpPost]
        [NTechAuthorizeAndPermissions(Permissions = new[] { "editUserCommit" })]
        public ActionResult Disapprove(int groupMembershipId)
        {
            //TODO: Remove and replace with ApproveWithHigh when removing the old backoffice ui
            Func<string, EmptyResult> badRequest = s =>
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                Response.StatusDescription = s;
                return new EmptyResult();
            };

            var userId = (this.User.Identity as ClaimsIdentity)?.FindFirst("ntech.userid")?.Value;

            if (userId == null)
            {
                throw new Exception("UI should not allow this");
            }

            var userIdInt = int.Parse(userId);

            using (var db = new UsersContext())
            {
                var groupMembership = db
                    .GroupMemberships
                    .SingleOrDefault(g => g.Id == groupMembershipId
                    && g.CreatedById != userIdInt //user is not allowed to disapprove group memberships they created themselves
                );
                if (groupMembership == null)
                    return badRequest("No such group membership.");

                groupMembership.DisapprovedDate = DateTime.Now;
                groupMembership.EndDate = DateTime.Today; //To make sure it doesn't sneak through anywhere that doesn't read disapproveddate
                groupMembership.ApprovedById = int.Parse(userId);
                db.SaveChanges();
            }
            return Json2(new { });
        }

        [HttpPost]
        [NTechAuthorizeAndPermissions(Permissions = new[] { "editUserBegin" })]
        public ActionResult BeginCancellation(int groupMembershipId)
        {
            Func<string, EmptyResult> badRequest = s =>
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                Response.StatusDescription = s;
                return new EmptyResult();
            };

            var userId = (this.User.Identity as ClaimsIdentity)?.FindFirst("ntech.userid")?.Value;
            if (userId == null)
            {
                throw new Exception("UI should not allow this.");
            }
            var userIdInt = int.Parse(userId);

            using (var db = new UsersContext())
            {
                var groupMembership = db
                    .GroupMemberships
                    .SingleOrDefault(g =>
                        g.Id == groupMembershipId
                        && g.User.Id != userIdInt   //user is not allowed to cancel group memberships for themselves
                    );
                if (groupMembership == null)
                {
                    return badRequest("UI should not allow this.");
                }

                if (db.GroupMembershipCancellations.Any(y => y.GroupMembership.Id == groupMembership.Id && !y.CancellationEndDate.HasValue))
                    return badRequest("There is already a pending cancellation for that group membership");

                if (groupMembership.GroupName == "High")
                {
                    int nrOfActiveHighs = UncancelledGroupsOnly(db.GroupMemberships)
                    .Count(g =>
                         g.GroupName == "High"
                         && (g.ApprovedById != null)
                         && g.ForProduct == groupMembership.ForProduct
                         && g.StartDate <= DateTime.Now
                         && DateTime.Now <= g.EndDate
                    );
                    if (nrOfActiveHighs <= 2)
                    {
                        return badRequest("It is not allowed to cancel group memberships so that there are less than two members of group 'High'.");
                    }
                }

                var cancellation = new GroupMembershipCancellation
                {
                    CancellationBeginDate = DateTime.Now,
                    BegunById = int.Parse(userId),
                    GroupMembership = groupMembership
                };

                db.GroupMembershipCancellations.Add(cancellation);
                db.SaveChanges();
            }
            return Json2(new { });
        }

        [HttpPost]
        [NTechAuthorizeAndPermissions(Permissions = new[] { "editUserCommit" })]
        [NTechAuthorizeHigh]
        public ActionResult CommitCancellationWithHigh(int groupMembershipId)
        {
            return CommitCancellation(groupMembershipId);
        }

        [HttpPost]
        [NTechAuthorizeAndPermissions(Permissions = new[] { "editUserCommit" })]
        public ActionResult CommitCancellation(int groupMembershipId)
        {
            //TODO: Remove and replace with ApproveWithHigh when removing the old backoffice ui
            Func<string, ActionResult> badRequest = s =>
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, s);
            };

            var userId = (this.User.Identity as ClaimsIdentity)?.FindFirst("ntech.userid")?.Value;
            if (userId == null)
            {
                throw new Exception("UI should not allow this.");
            }
            var userIdInt = int.Parse(userId);

            using (var db = new UsersContext())
            {
                var groupMembership = db
                    .GroupMemberships
                    .SingleOrDefault(g =>
                        g.Id == groupMembershipId
                        && g.User.Id != userIdInt   //user is not allowed to cancel group memberships for themselves
                    );

                if (groupMembership == null)
                {
                    return badRequest("UI should not allow this.");
                }

                if (groupMembership.GroupName == "High")
                {
                    var expandedProducts = UserServiceModel
                        .GetExpandedGroupsShared(new[] { new UserServiceModel.GroupModel { ForProduct = groupMembership.ForProduct, GroupName = groupMembership.GroupName } })
                        .Select(x => x.ForProduct)
                        .Distinct()
                        .ToList();

                    int nrOfActiveHighs = ActiveGroupsOnly(db.GroupMemberships)
                    .Count(g =>
                        g.GroupName == "High"
                        && (g.ApprovedById != null)
                        && expandedProducts.Contains(g.ForProduct)
                        && g.StartDate <= DateTime.Now
                        && DateTime.Now <= g.EndDate
                    );
                    if (nrOfActiveHighs <= 2)
                    {
                        return Json2(new { errorMsg = "It is not allowed to cancel group memberships so that there are less than two active members of group 'High'." });
                    }
                }

                var cancellations = db
                    .GroupMembershipCancellations
                    .Where(g => g.GroupMembership.Id == groupMembershipId && !g.CommittedById.HasValue && !g.UndoneById.HasValue)
                    .ToList();
                foreach (var c in cancellations)
                {
                    c.CommittedById = userIdInt;
                    c.CancellationEndDate = DateTime.Now;
                }
                db.SaveChanges();
            }
            return Json2(new { });
        }

        [HttpPost]
        [NTechAuthorizeAndPermissions(Permissions = new[] { "editUserCommit" })]
        [NTechAuthorizeHigh]
        public ActionResult UndoCancellationWithHigh(int groupMembershipId)
        {
            return UndoCancellation(groupMembershipId);
        }

        [HttpPost]
        [NTechAuthorizeAndPermissions(Permissions = new[] { "editUserCommit" })]
        public ActionResult UndoCancellation(int groupMembershipId)
        {
            //TODO: Remove and replace with UndoCancellationWithHigh when removing the old backoffice ui
            Func<string, EmptyResult> badRequest = s =>
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                Response.StatusDescription = s;
                return new EmptyResult();
            };

            var userId = (this.User.Identity as ClaimsIdentity)?.FindFirst("ntech.userid")?.Value;
            if (userId == null)
            {
                throw new Exception("UI should not allow this.");
            }
            var userIdInt = int.Parse(userId);

            using (var db = new UsersContext())
            {
                var groupMembership = db
                    .GroupMemberships
                    .SingleOrDefault(g =>
                        g.Id == groupMembershipId
                    );
                if (groupMembership == null)
                {
                    return badRequest("UI should not allow this.");
                }
                else
                {
                    var cancellations = db
                        .GroupMembershipCancellations
                        .Where(g => g.GroupMembership.Id == groupMembershipId && !g.CommittedById.HasValue && !g.UndoneById.HasValue)
                        .ToList();
                    foreach (var c in cancellations)
                    {
                        c.UndoneById = userIdInt;
                        c.CancellationEndDate = DateTime.Now;
                    }
                    db.SaveChanges();
                }
            }
            return Json2(new { });
        }

        [HttpPost]
        public ActionResult GetGroupNamesMappedToAnyUser()
        {
            using (var context = new UsersContext())
            {
                return Json2(context.GroupMemberships.Select(x => x.GroupName).Distinct().ToList());
            }
        }

        public class CreateGroupMembershipRequest
        {
            public int UserId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Group { get; set; }
            public string Product { get; set; }
        }

        [NTechAuthorizeAndPermissions(Permissions = new[] { "editUserBegin" })]
        [HttpPost]
        public ActionResult CreateNonAdmin(CreateGroupMembershipRequest request)
        {
            Func<string, EmptyResult> badRequest = s =>
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                Response.StatusDescription = s;
                return new EmptyResult();
            };

            var u = this.User.Identity as System.Security.Claims.ClaimsIdentity;
            var createdByUserId = u?.FindFirst("ntech.userid")?.Value;

            if (createdByUserId == null)
                return badRequest("Missing ntech.userid");

            if (request == null)
                return badRequest("Missing all arguments");

            if (request.UserId <= 0)
                return badRequest("Missing userId");

            if (request.StartDate > request.EndDate)
                return badRequest("StartDate and EndDate are in reverse order");

            if (!GroupMembershipController.AllGroupNames.Contains(request.Group))
                return badRequest("Invalid group name");

            if (request.Group == "Admin")
                return badRequest("Use the CreateAdmin method instead");

            if (request.Product != "ConsumerCredit")
                return badRequest("Invalid product name");

            GroupMembership groupMembership;
            using (var db = new UsersContext())
            {
                var user = db.Users.SingleOrDefault(x => x.Id == request.UserId);
                if (user == null)
                    return badRequest("The user does not exist");

                if (UncancelledGroupsOnly(db.GroupMemberships).Any(y => y.User.Id == request.UserId && y.GroupName == request.Group && y.ForProduct == request.Product && request.StartDate < y.EndDate && y.StartDate <= request.EndDate))
                    return badRequest("The user is already a member of that group");

                groupMembership = new GroupMembership
                {
                    User = user,
                    CreationDate = DateTime.Now,
                    CreatedById = int.Parse(createdByUserId),
                    GroupName = request.Group,
                    ForProduct = request.Product,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate
                };

                db.GroupMemberships.Add(groupMembership);

                db.SaveChanges();
            }
            return Json2(new { Id = groupMembership.Id });
        }

        [NTechAuthorizeAndPermissions(Permissions = new[] { "editAdminBegin" })]
        [HttpPost]
        public ActionResult CreateAdmin(CreateGroupMembershipRequest request)
        {
            Func<string, EmptyResult> badRequest = s =>
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                Response.StatusDescription = s;
                return new EmptyResult();
            };

            var u = this.User.Identity as System.Security.Claims.ClaimsIdentity;
            var createdByUserId = u?.FindFirst("ntech.userid")?.Value;

            if (createdByUserId == null)
                return badRequest("Missing ntech.userid");

            if (request == null)
                return badRequest("Missing all arguments");

            if (request.UserId <= 0)
                return badRequest("Missing userId");

            if (request.StartDate > request.EndDate)
                return badRequest("StartDate and EndDate are in reverse order");

            if (request.Group != "Admin")
                return badRequest("Use the CreateNonAdmin method instead");

            if (request.Product != "ConsumerCredit")
                return badRequest("Invalid product name");

            GroupMembership groupMembership;
            using (var db = new UsersContext())
            {
                var user = db.Users.SingleOrDefault(x => x.Id == request.UserId);
                if (user == null)
                    return badRequest("The user does not exist");

                if (UncancelledGroupsOnly(user.GroupMemberships.AsQueryable()).Any(y => y.GroupName == request.Group && y.ForProduct == request.Product && request.StartDate <= y.EndDate && y.StartDate <= request.EndDate))
                    return badRequest("The user is already a member of that group for the same time period");

                groupMembership = new GroupMembership
                {
                    User = user,
                    CreationDate = DateTime.Now,
                    CreatedById = int.Parse(createdByUserId),
                    GroupName = request.Group,
                    ForProduct = request.Product,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate
                };
                db.GroupMemberships.Add(groupMembership);

                db.SaveChanges();
            }
            return Json2(new { Id = groupMembership.Id });
        }

        [HttpPost]
        [NTechAuthorizeHigh]
        public ActionResult GroupsAboutToExpireCreatedByUserWithHigh(int userId)
        {
            return GroupsAboutToExpireCreatedByUser(userId);
        }

        [HttpPost]
        public ActionResult GroupsAboutToExpireCreatedByUser(int userId)
        {
            //TODO: Flag away when removing old backoffice ui
            DateTime TwoMonthsAhead = DateTime.Now.AddMonths(2);
            DateTime Yesterday = DateTime.Today;
            using (var db = new UsersContext())
            {
                var result = new UsersContext().GroupMemberships
                    .Where(x => x.CreatedById == userId)
                    .Where(x => x.EndDate < TwoMonthsAhead)
                    .Where(x => x.EndDate > Yesterday) //we don't want to show dates that have already expired, but we add one day to make sure we see memberships that expired earlier today
                    .Select(x => new
                    {
                        UserDisplayName = x.User.DisplayName,
                        DisplayName = x.User.DisplayName,
                        x.ForProduct,
                        x.GroupName,
                        x.Id,
                        x.StartDate,
                        x.EndDate,
                        x.CreationDate
                    }).ToList();

                return Json2(new { groupsAboutToExpire = result });
            }
        }

        [HttpPost]
        [NTechAuthorizeHigh]
        public ActionResult GroupsNeedingApprovalWithHigh()
        {
            return GroupsNeedingApproval();
        }

        [HttpPost]
        public ActionResult GroupsNeedingApproval()
        {
            //TODO: Flag this away and keep the high verson when removing the old backoffice ui
            var userId = (this.User.Identity as ClaimsIdentity)?.FindFirst("ntech.userid")?.Value;

            if (userId == null)
            {
                NLog.Warning("Missing UserId");
                return Json2(new object[] { });
            }

            var userIdInt = int.Parse(userId);

            using (var db = new UsersContext())
            {
                var result = UncancelledGroupsOnly(db.GroupMemberships)
                    .Where(x => !x.ApprovedDate.HasValue && !x.DisapprovedDate.HasValue && x.CreatedById != userIdInt)
                    .Select(x => new
                    {
                        DisplayName = x.User.DisplayName,
                        x.ForProduct,
                        x.GroupName,
                        x.Id,
                        x.StartDate,
                        x.EndDate,
                        x.CreationDate
                    }).ToList();

                return Json2(new { groupsNeedingApproval = result });
            }
        }

        [HttpPost]
        [NTechAuthorizeHigh]
        public ActionResult CancellationsToCommitWithHigh()
        {
            return CancellationsToCommit();
        }

        [HttpPost]
        public ActionResult CancellationsToCommit()
        {
            //TODO: Remove this and keep only the high version when removing the old ui from backoffice
            var userId = (this.User.Identity as ClaimsIdentity)?.FindFirst("ntech.userid")?.Value;

            if (userId == null)
            {
                NLog.Warning("Missing UserId");
                return Json2(new object[] { });
            }

            var userIdInt = int.Parse(userId);

            using (var db = new UsersContext())
            {
                var result = GroupsAwaitingCancellationCommitOnly(db.GroupMemberships)
                    .Where(x =>
                           x.User.Id != userIdInt   //user is not allowed to cancel group memberships for themselves
                        )
                    .Select(x => new
                    {
                        DisplayName = x.User.DisplayName,
                        x.ForProduct,
                        x.GroupName,
                        x.Id,
                        x.StartDate,
                        x.EndDate,
                        x.CreationDate
                    }).ToList();

                return Json2(result);
            }
        }

        [HttpPost]
        public ActionResult GetDataForAdministerUser(int userid)
        {
            using (var db = new UsersContext())
            {
                var user = db
                    .Users
                    .Where(x => x.Id == userid)
                    .Select(x => new
                    {
                        User = new
                        {
                            UserId = x.Id,
                            x.IsSystemUser,
                            x.DisplayName,
                            x.ProviderName,
                            x.DeletionDate,
                            DeletedBy = db.Users.FirstOrDefault(u => u.Id == x.DeletedById).DisplayName
                        },
                        UserEntity = x,
                        LoginMethods = x
                            .AuthenticationMechanisms
                            .Where(y => !y.RemovedById.HasValue)
                            .Select(y => new
                            {
                                y.Id,
                                y.AuthenticationProvider,
                                y.AuthenticationType,
                                y.UserIdentity
                            })
                    })
                    .Single();
                var now = DateTime.Now;

                var groups = UncancelledGroupsOnly(db.GroupMemberships)
                    .Where(x => x.User.Id == userid)
                    .Select(x => new
                    {
                        x.Id,
                        x.CreationDate,
                        x.GroupName,
                        x.ForProduct,
                        x.StartDate,
                        x.EndDate,
                        x.ApprovedById,
                        IsApproved = x.ApprovedDate.HasValue,
                        IsActive = now < x.EndDate && !x.GroupMembershipCancellation.Any(y => y.CommittedById.HasValue),
                        EndedOrCancelledDate = x.GroupMembershipCancellation.Where(y => y.CommittedById.HasValue).Select(y => y.CancellationEndDate).FirstOrDefault() ?? x.EndDate,
                        PendingCancellation = x
                            .GroupMembershipCancellation
                            .Where(y => !y.CancellationEndDate.HasValue).OrderByDescending(y => y.Id)
                            .Select(y => new
                            {
                                y.Id,
                                y.BegunById
                            })
                            .FirstOrDefault()
                    })
                    .ToList();

                return Json2(new
                {
                    user = new
                    {
                        user.User.UserId,
                        user.User.IsSystemUser,
                        user.User.DisplayName,
                        user.User.ProviderName,
                        user.User.DeletionDate,
                        user.User.DeletedBy,
                        IsRemoveAuthenticationMechanismAllowed = AuthenticationMechanismController.IsRemoveAuthenticationMechanismAllowed(user.UserEntity)
                    },
                    loginMethods = user.LoginMethods,
                    groups = groups.Where(x => x.IsActive).ToList(),
                    expiredGroups = groups.Where(x => !x.IsActive).ToList(),
                });
            }
        }

        [HttpPost]
        public ActionResult GetById(int id)
        {
            using (var db = new UsersContext())
            {
                var result = UncancelledGroupsOnly(db.GroupMemberships).Where(x => x.Id == id).Select(GroupMembershipModel.FromGroupMembership).SingleOrDefault();
                if (result == null)
                    return HttpNotFound();
                return Json2(result);
            }
        }

        [HttpPost]
        public ActionResult GetUserIdsInGroup(string forProduct, string groupName)
        {
            using (var context = new UsersContext())
            {
                var normalUsers = Code.NTechUserService
                    .GetUserQueryBase(context.Users)
                    .Where(x => !x.IsSystemUser && x.ProviderName == null);

                var userIds = Code.UserServiceModel
                    .FilterByExpandedGroup(normalUsers, forProduct, groupName)
                    .Select(x => x.Id)
                    .ToList();
                return Json2(new
                {
                    userIds = userIds
                });
            }
        }

        [HttpPost]
        public ActionResult GetUsersIdsInMiddle()
        {
            using (var context = new UsersContext())
            {
                var userIds = Code.NTechUserService
                    .GetUserQueryBase(context.Users)
                    .Where(x =>
                        x.Groups.Any(y => y.GroupName == Groups.Middle.ToString())
                        && !x.IsSystemUser
                        && x.ProviderName == null
                    )
                    .Select(x => x.Id)
                    .ToList();
                return Json2(new
                {
                    userIds = userIds
                });
            }
        }
    }
}