using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web.Mvc;
using NTech.Services.Infrastructure;
using nUser.DbModel;

namespace nUser.Controllers
{
    public class UserController : NController
    {
        private static ActionResult BadRequest(string msg) => new HttpStatusCodeResult(HttpStatusCode.BadRequest, msg);

        [AllowAnonymous]
        [HttpPost]
        public ActionResult IsValidCivicRegNr(string civicRegNr)
        {
            if (FinnishCivicRegNumber.IsValidFinnishCivicRegNr(civicRegNr, out var normalizedValue))
            {
                return Json2(new
                {
                    isValid = true,
                    value = normalizedValue
                });
            }

            return Json2(new
            {
                isValid = false,
                value = civicRegNr
            });
        }

        //This exists in addition to GetAll to eventually remove GetAll so we can start getting explicit roles on the apis
        [NTechAuthorizeAdmin]
        [HttpPost]
        public ActionResult GetAllWithAdmin(bool showDeleted = false)
        {
            return GetAll(showDeleted);
        }

        [HttpPost]
        public ActionResult GetAll(bool showDeleted = false)
        {
            using (var context = new UsersContext())
            {
                var usersQuery = context.Users.AsQueryable();
                if (!showDeleted)
                {
                    usersQuery = usersQuery.Where(u => u.DeletionDate == null);
                }

                var result = usersQuery.Select(u => new UserModel
                {
                    Id = u.Id,
                    CreatedById = u.CreatedById,
                    CreationDate = u.CreationDate,
                    Name = u.DisplayName,
                    DeletionDate = u.DeletionDate,
                    DeletedBy = u.DeletedById == null
                        ? null
                        : context.Users.FirstOrDefault(x => x.Id == u.DeletedById).DisplayName
                });

                return Json2(result.ToList());
            }
        }

        [HttpPost]
        public ActionResult GetAllDisplayNamesAndUserIds()
        {
            return Json2(new UsersContext().Users.ToList().Select(x => new { UserId = x.Id, x.DisplayName }).ToList());
        }

        [HttpPost]
        public ActionResult GetDisplayNamesAndUserIds(List<int> userIds)
        {
            userIds = userIds ?? new List<int>();
            return Json2(new UsersContext().Users.Where(x => userIds.Contains(x.Id)).ToList()
                .Select(x => new { UserId = x.Id, x.DisplayName }).ToList());
        }

        [HttpPost]
        public ActionResult GetProviderNameForCurrentUser()
        {
            using (var c = new UsersContext())
            {
                var p = c.Users.Where(x => x.Id == this.CurrentUserId)
                    .Select(x => new { x.ProviderName, x.IsSystemUser }).SingleOrDefault();
                return Json2(new
                {
                    providerName = p?.ProviderName, isProvider = !string.IsNullOrWhiteSpace(p?.ProviderName),
                    userExists = p != null, isSystemUser = (p?.IsSystemUser ?? false)
                });
            }
        }

        public class CreateUserRequest
        {
            public string Name { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Group { get; set; }
            public string Product { get; set; }
            public string WsFedName { get; set; }
            public string WsFedProviderName { get; set; }
        }

        [HttpPost]
        [NTechAuthorizeAndPermissions(Permissions = new[] { "editAdminBegin" })]
        public ActionResult CreateAdmin(CreateUserRequest request)
        {
            return Create(request, r => r.Group != "Admin" ? "Use CreateNonAdmin instead" : null);
        }

        [HttpPost]
        [NTechAuthorizeAndPermissions(Permissions = new[] { "editUserBegin" })]
        public ActionResult CreateNonAdmin(CreateUserRequest request)
        {
            return Create(request, r => r.Group == "Admin" ? "Use CreateAdmin instead" : null);
        }

        private bool TryCreateUserEntity(string createdByUserId, string displayName, out User user,
            out string failedMessage, Action<User> initAdditional = null)
        {
            user = null;

            var v = new NTechUserNameValidator();
            if (!v.TryValidateUserName(displayName, NTechUserNameValidator.UserNameTypeCode.DisplayUserName,
                    out failedMessage))
            {
                return false;
            }

            user = new User
            {
                CreationDate = DateTime.Now,
                CreatedById = int.Parse(createdByUserId),
                DisplayName = displayName.Trim()
            };

            initAdditional?.Invoke(user);

            failedMessage = null;
            return true;
        }

        [HttpPost]
        [NTechAuthorizeAndPermissions(Permissions = new[] { "editAdminBegin" })]
        public ActionResult CreateAdminSimple(string displayName, DateTime startDate, DateTime endDate)
        {
            var u = User.Identity as ClaimsIdentity;
            var userId = u?.FindFirst("ntech.userid")?.Value;

            if (userId == null)
                return BadRequest("Missing userid");

            if (string.IsNullOrWhiteSpace(displayName))
                return BadRequest("Missing displayName");

            if (startDate > endDate)
                return BadRequest("StartDate and EndDate are in reverse order");

            using (var db = new UsersContext())
            {
                if (!TryCreateUserEntity(userId, displayName, out var user, out var failedMessage))
                    return BadRequest(failedMessage);

                var groupMembership = new GroupMembership
                {
                    User = user,
                    CreationDate = user.CreationDate,
                    CreatedById = user.CreatedById,
                    GroupName = "Admin",
                    ForProduct = "Admin",
                    StartDate = startDate,
                    EndDate = endDate
                };

                db.Users.Add(user);
                db.GroupMemberships.Add(groupMembership);

                db.SaveChanges();

                return Json2(new { UserId = user.Id });
            }
        }

        [HttpPost]
        [NTechAuthorizeAndPermissions(Permissions = new[] { "editUserBegin" })]
        public ActionResult CreateRegularUserSimple(string displayName)
        {
            var u = User.Identity as ClaimsIdentity;
            var userId = u?.FindFirst("ntech.userid")?.Value;

            if (userId == null)
                return BadRequest("Missing userid");

            if (string.IsNullOrWhiteSpace(displayName))
                return BadRequest("Missing displayName");

            using (var db = new UsersContext())
            {
                if (!TryCreateUserEntity(userId, displayName, out var user, out var failedMessage))
                    return BadRequest(failedMessage);

                db.Users.Add(user);

                db.SaveChanges();

                return Json2(new { UserId = user.Id });
            }
        }

        [HttpPost]
        [NTechAuthorizeAndPermissions(Permissions = new[] { "editUserBegin" })]
        public ActionResult CreateProviderSimple(string displayName, string providerName)
        {
            var u = User.Identity as ClaimsIdentity;
            var userId = u?.FindFirst("ntech.userid")?.Value;

            if (userId == null)
                return BadRequest("Missing userid");

            if (string.IsNullOrWhiteSpace(displayName))
                return BadRequest("Missing displayName");

            if (string.IsNullOrWhiteSpace(providerName))
                return BadRequest("Missing providerName");

            var v = new NTechUserNameValidator();
            if (!v.TryValidateUserName(providerName, NTechUserNameValidator.UserNameTypeCode.DisplayUserName,
                    out var invalidProviderNameMessage))
            {
                return BadRequest("Invalid providerName: " + invalidProviderNameMessage);
            }

            using (var db = new UsersContext())
            {
                if (!TryCreateUserEntity(userId, displayName, out var user, out var failedMessage,
                        initAdditional: x => x.ProviderName = providerName.Trim()))
                {
                    return BadRequest(failedMessage);
                }

                db.Users.Add(user);

                db.SaveChanges();

                return Json2(new { UserId = user.Id });
            }
        }

        [HttpPost]
        [NTechAuthorizeAndPermissions(Permissions = new[] { "editAdminBegin" })]
        public ActionResult CreatSystemUserSimple(string displayName)
        {
            var u = User.Identity as ClaimsIdentity;
            var userId = u?.FindFirst("ntech.userid")?.Value;

            if (userId == null)
                return BadRequest("Missing userid");

            if (string.IsNullOrWhiteSpace(displayName))
                return BadRequest("Missing displayName");

            using (var db = new UsersContext())
            {
                if (!TryCreateUserEntity(userId, displayName, out var user, out var failedMessage,
                        initAdditional: x => x.IsSystemUser = true))
                {
                    return BadRequest(failedMessage);
                }

                db.Users.Add(user);

                db.SaveChanges();

                return Json2(new { UserId = user.Id });
            }
        }

        [HttpPost]
        [NTechAuthorizeAndPermissions(Permissions = new[] { "editUserBegin" })]
        public ActionResult DeactivateUser(int userId)
        {
            var loggedInUserId = CurrentUserId;

            if (loggedInUserId == userId)
            {
                return BadRequest("User cannot remove itself. ");
            }

            using (var context = new UsersContext())
            {
                var userToDelete = context.Users.SingleOrDefault(u => u.Id == userId);
                if (userToDelete == null)
                {
                    return BadRequest("User to deactivate does not exist. ");
                }

                if (userToDelete.DeletedById != null || userToDelete.DeletionDate != null)
                {
                    return BadRequest("User has already been deleted. ");
                }

                userToDelete.DeletedById = loggedInUserId;
                userToDelete.DeletionDate = DateTime.Now;

                var groups = context.GroupMemberships.Where(gm => gm.User.Id == userId)
                    .Include(groupMembership => groupMembership.GroupMembershipCancellation)
                    .ToList();
                foreach (var group in groups)
                {
                    context.GroupMembershipCancellations.RemoveRange(group.GroupMembershipCancellation);
                    context.GroupMemberships.Remove(group);
                }

                context.SaveChanges();

                return Json2(new { UserId = userId });
            }
        }

        [HttpPost]
        [NTechAuthorizeAndPermissions(Permissions = new[] { "editUserBegin" })]
        public ActionResult ReactivateUser(int userId)
        {
            var loggedInUserId = this.CurrentUserId;

            if (loggedInUserId == userId)
            {
                return BadRequest("User cannot reactivate itself. ");
            }

            using (var context = new UsersContext())
            {
                var userInDatabase = context.Users.Single(u => u.Id == userId);
                if (userInDatabase.DeletionDate is null || userInDatabase.DeletedById is null)
                {
                    return BadRequest("Cannot reactivate a user that is not deleted. ");
                }

                userInDatabase.DeletionDate = null;
                userInDatabase.DeletedById = null;

                context.SaveChanges();

                return Json2(new { UserId = userId });
            }
        }

        private ActionResult Create(CreateUserRequest request, Func<CreateUserRequest, string> checkGroup)
        {
            Func<string, EmptyResult> badRequest = s =>
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                Response.StatusDescription = s;
                return new EmptyResult();
            };

            var u = this.User.Identity as ClaimsIdentity;
            var userId = u?.FindFirst("ntech.userid")?.Value;

            if (userId == null)
                return badRequest("Missing userid");

            if (request == null)
                return badRequest("Missing all arguments");

            if (request.StartDate > request.EndDate)
                return badRequest("StartDate and EndDate are in reverse order");

            if (!GroupMembershipController.AllGroupNames.Contains(request.Group))
                return badRequest("Invalid group name");

            var g = checkGroup(request);
            if (g != null)
                return badRequest(g);

            if (request.Product != "ConsumerCredit")
                return badRequest("Invalid product name");

            GroupMembership groupMembership;
            using (var db = new UsersContext())
            {
                if (db.Users.Any(x => x.DisplayName == request.Name))
                    return badRequest("A user with that DisplayName already exists");
                if (request.WsFedName != null && request.WsFedProviderName != null)
                {
                    if (db.Users.Any(x => x.AuthenticationMechanisms.Any(y =>
                            y.AuthenticationProvider == request.WsFedProviderName &&
                            y.UserIdentity == request.WsFedName)))
                        return badRequest("A user already exists with that federated account");
                }

                if (!TryCreateUserEntity(userId, request?.Name, out var user, out var failedMessage))
                {
                    return badRequest(failedMessage);
                }

                groupMembership = new GroupMembership
                {
                    User = user,
                    CreationDate = user.CreationDate,
                    CreatedById = user.CreatedById,
                    GroupName = request.Group,
                    ForProduct = request.Product,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate
                };

                db.Users.Add(user);
                db.GroupMemberships.Add(groupMembership);

                if (request.WsFedName != null && request.WsFedProviderName != null)
                {
                    var v = new NTechUserNameValidator();
                    if (!v.TryValidateUserName(request.WsFedName,
                            NTechUserNameValidator.UserNameTypeCode.ActiveDirectoryUserName, out failedMessage))
                    {
                        return badRequest("Invalid WsFedName: " + failedMessage);
                    }

                    var authMech = new AuthenticationMechanism
                    {
                        User = user,
                        AuthenticationProvider = request.WsFedProviderName,
                        AuthenticationType = "WsFederationAzureAD",
                        CreatedById = user.CreatedById,
                        CreationDate = user.CreationDate,
                        UserIdentity = request.WsFedName
                    };
                    db.AuthenticationMechanisms.Add(authMech);
                }

                db.SaveChanges();
            }

            return Json2(new { Id = groupMembership.Id });
        }
    }
}