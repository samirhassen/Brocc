using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;

namespace nBackOffice.Controllers
{
    [NTechAuthorizeAdmin]
    public class AdminController : NController
    {
        public ActionResult Index()
        {
            return RedirectToAction("NavMenu", "Secure");
        }

        private NHttp.NHttpCall BeginCallUser()
        {
            return NHttp
                .Begin(new Uri(NEnv.ServiceRegistry.Internal["nUser"]), NHttp.GetCurrentAccessToken());
        }

        public string checkDisplayName(string displayName)
        {
            bool result = displayName.Substring(0, 1).All(Char.IsLetter);
            if (result == false)
                return "DisplayName first character must only contain letters";

            result = displayName.All(c => Char.IsLetterOrDigit(c) || Char.IsWhiteSpace(c));
            if (result == false)
                return "DisplayName must only contain letters/digits and whitespace";

            return "";
        }

        [HttpPost]
        public ActionResult ValidateUserDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return Json(new { userDisplayNameAlreadyInUse = true, errorMessage = "DisplayName cannot be empty" });

            //Check if display name is in use
            var allCurrentUsers = BeginCallUser()
                .PostJson("User/GetAllDisplayNamesAndUserIds", new { })
                .ParseJsonAsAnonymousType(new[] { new { UserId = default(int), DisplayName = default(string) } });
            if (allCurrentUsers != null && allCurrentUsers.Any(x => x.DisplayName.Equals(displayName, StringComparison.InvariantCultureIgnoreCase)))
            {
                return Json(new { userDisplayNameAlreadyInUse = true, errorMessage = "DisplayName already in use" });
            }
            else
            {
                return Json(new { });
            }
        }

        [HttpPost]
        public ActionResult CreateUser2(string displayName, string userType, string adminStartDate, string adminEndDate)
        {
            object request;
            string methodName;

            if (string.IsNullOrWhiteSpace(displayName))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "displayName missing");
            if (string.IsNullOrWhiteSpace(userType))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "userType missing");

            if (!string.IsNullOrEmpty(displayName) && displayName.Length > 1)
            {
                var errorMessage = checkDisplayName(displayName);
                if (!string.IsNullOrEmpty(errorMessage))

                    return Json(new { userDisplayNameAlreadyInUse = true, errorMessage = errorMessage });
            }

            //Check if display name is in use
            var allCurrentUsers = BeginCallUser()
                .PostJson("User/GetAllDisplayNamesAndUserIds", new { })
                .ParseJsonAsAnonymousType(new[] { new { UserId = default(int), DisplayName = default(string) } });

            if (allCurrentUsers != null && allCurrentUsers.Any(x => x.DisplayName.Equals(displayName, StringComparison.InvariantCultureIgnoreCase)))
            {
                return Json(new { userDisplayNameAlreadyInUse = true, errorMessage = "DisplayName already in use" });
            }

            if (userType == "admin")
            {
                if (string.IsNullOrWhiteSpace(adminStartDate))
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "adminStartDate missing");
                if (string.IsNullOrWhiteSpace(adminEndDate))
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "adminEndDate missing");
                request = new
                {
                    displayName = displayName,
                    startDate = DateTime.ParseExact(adminStartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    endDate = DateTime.ParseExact(adminEndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                };
                methodName = "CreateAdminSimple";
            }
            else if (userType == "provider")
            {
                request = new
                {
                    displayName = displayName,
                    providerName = displayName
                };
                methodName = "CreateProviderSimple";
            }
            else if (userType == "systemUser")
            {
                request = new
                {
                    displayName = displayName
                };
                methodName = "CreatSystemUserSimple";
            }
            else if (userType == "user")
            {
                request = new
                {
                    displayName = displayName
                };
                methodName = "CreateRegularUserSimple";
            }
            else
            {
                return Json(new { errorMessage = "Incorrect user type" });
            }

            var response = BeginCallUser()
                .PostJson("User/" + methodName, request);
            if (response.StatusCode == 400)
            {
                return Json(new { errorMessage = response.ReasonPhrase });
            }
            else
            {
                var result = response.ParseJsonAs<dynamic>();
                return Json(new { redirectToUrl = Url.Action("AdministerUser", new { id = result.UserId }), createdUserId = result.UserId });
            }
        }

        public ActionResult AdministerUser(int id)
        {
            if (!NEnv.AllowAccessToLegacyUserAdmin)
                return HttpNotFound();

            var activeLoginMethods = WithCache("nBackoffice.Controllers.NController.ActiveLoginMethods", () =>
            {
                return BeginCallUser()
                    .PostJson("AuthenticationMechanism/GetActiveLoginMethods", new { })
                    .ParseJsonAs<dynamic>();
            }, TimeSpan.FromMinutes(5));

            var result = BeginCallUser()
                .PostJson("GroupMembership/GetDataForAdministerUser", new { userid = id })
                .ParseJsonAs<dynamic>();

            ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.GetEncoding("iso-8859-1").GetBytes(JsonConvert.SerializeObject(new
            {
                loggedInUserId = LoggedInUserId,
                groupMemberships = result.groups,
                expiredGroupMemberships = result.expiredGroups,
                user = result.user,
                activeLoginMethods = activeLoginMethods,
                loginMethods = result.loginMethods,
                isMortgageLoansEnabled = NEnv.IsMortgageLoansEnabled,
                isUnsecuredLoansEnabled = NEnv.IsUnsecuredLoansEnabled
            })));
            return View();
        }

        [HttpPost]
        public ActionResult CreateLoginMethod(
            int userId,
            string adUsername,
            string providerEmail,
            string upwUsername,
            string upwPassword,
            string authenticationType,
            string providerName,
            string userIdentityAndCredentialsType,
            string providerObjectId
            )
        {
            if (!string.IsNullOrEmpty(upwUsername) && upwUsername.Length > 1)
            {
                var errorMessage = checkDisplayName(upwUsername);
                if (!string.IsNullOrEmpty(errorMessage))
                    return Json(new { errorMessage = errorMessage });
            }

            if (!string.IsNullOrEmpty(adUsername) && adUsername.Length > 1)
            {
                var errorResult = adUsername.Substring(0, 1).All(Char.IsLetter);
                if (errorResult == false)
                    return Json(new { errorMessage = "DisplayName first character must only contain letters" });

                errorResult = adUsername.All(c => Char.IsLetterOrDigit(c) || Char.IsWhiteSpace(c) || Char.IsPunctuation(c) || c == '@');
                if (errorResult == false)
                    return Json(new { errorMessage = "DisplayName must only contain letters/digits and whitespace" });

                errorResult = adUsername.Substring(0, 1) == "\\";
                if (errorResult == true)
                    return Json(new { errorMessage = "DisplayName first character cant be a \\" });

                errorResult = adUsername.Substring(adUsername.Length - 1, 1) == "\\";
                if (errorResult == true)
                    return Json(new { errorMessage = "DisplayName last character cant be a \\" });

                errorResult = adUsername.Substring(0, 1) == ".";
                if (errorResult == true)
                    return Json(new { errorMessage = "DisplayName first character cant be a ." });

                errorResult = adUsername.Substring(adUsername.Length - 1, 1) == ".";
                if (errorResult == true)
                    return Json(new { errorMessage = "DisplayName last character cant be a ." });

                errorResult = adUsername.Substring(0, 1) == "@";
                if (errorResult == true)
                    return Json(new { errorMessage = "DisplayName first character cant be a @" });

                errorResult = adUsername.Substring(adUsername.Length - 1, 1) == "@";
                if (errorResult == true)
                    return Json(new { errorMessage = "DisplayName last character cant be a @" });

                int count = adUsername.Count(f => f == '\\');
                if (count > 1)
                    return Json(new { errorMessage = "DisplayName can not contain more than 1 \\" });
            }
            var request = new
            {
                userId,
                adUsername,
                providerEmail,
                upwUsername,
                upwPassword,
                authenticationType,
                providerName,
                userIdentityAndCredentialsType,
                providerObjectId
            };
            var response = BeginCallUser().PostJson("AuthenticationMechanism/Create", request);
            if (response.StatusCode == 400)
            {
                return Json(new { errorMessage = response.ReasonPhrase });
            }
            else
            {
                var result = response.ParseJsonAs<dynamic>();
                return Json(new { addedLoginMethod = result });
            }
        }

        [HttpPost]
        public ActionResult RemoveLoginMethod(int id)
        {
            var response = BeginCallUser()
                .PostJson("AuthenticationMechanism/Remove", new { authenticationMechanismId = id });

            if (response.StatusCode == 400)
            {
                return Json(new { errorMessage = response.ReasonPhrase });
            }
            else
            {
                response.EnsureSuccessStatusCode();
                return Json(new { });
            }
        }

        public ActionResult AdministerUsers()
        {
            if (!NEnv.AllowAccessToLegacyUserAdmin)
                return HttpNotFound();

            var result = BeginCallUser().PostJson("User/GetAll", new { }).ParseJsonAs<dynamic>();
            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                users = result,
                createUserUrl = Url.Action("CreateUser2"),
                validateUserDisplayNameUrl = Url.Action("ValidateUserDisplayName")
            });
            return View();
        }

        [HttpPost]
        public ActionResult BeginGroupmembershipCancellation(int groupMembershipId)
        {
            if (!NEnv.AllowAccessToLegacyUserAdmin)
                return HttpNotFound(); //New ui calls nUser directly

            var result = BeginCallUser()
                .PostJson("GroupMembership/BeginCancellation", new
                {
                    groupMembershipId = groupMembershipId
                })
                .ParseJsonAs<dynamic>();
            return Json(result);
        }

        public class CreateGroupMembershipRequest
        {
            public int UserId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Group { get; set; }
            public string Product { get; set; }
        }

        [HttpPost]
        public ActionResult CreateGroupmembership(CreateGroupMembershipRequest request)
        {
            if (!NEnv.AllowAccessToLegacyUserAdmin)
                return HttpNotFound(); //New ui calls nUser directly

            var result = BeginCallUser()
                .PostJson(
                    request.Group == "Admin"
                        ? "GroupMembership/CreateAdmin"
                        : "GroupMembership/CreateNonAdmin",
                    request)
                .ParseJsonAs<dynamic>();

            var result2 = BeginCallUser()
                .PostJson("GroupMembership/GetById", new { id = result.Id })
                .ParseJsonAs<dynamic>();

            return Json(new { newGroup = result2 });
        }

        [HttpPost]
        public ActionResult FetchGroupsAboutToExpire()
        {
            var result = BeginCallUser().PostJson("GroupMembership/GroupsAboutToExpireCreatedByUser", new
            {
                userId = this.Identity.GetUserId()
            }).ParseJsonAs<dynamic>();

            return Json(result);
        }

        [HttpPost]
        public ActionResult DeactivateUser(int userId)
        {
            if (!NEnv.AllowAccessToLegacyUserAdmin)
                return HttpNotFound(); //New ui calls nUser directly

            var result = BeginCallUser()
                .PostJson("User/DeactivateUser", new { userId })
                .ParseJsonAs<dynamic>();
            return Json(result);
        }

        [HttpPost]
        public ActionResult ReactivateUser(int userId)
        {
            if (!NEnv.AllowAccessToLegacyUserAdmin)
                return HttpNotFound(); //New ui calls nUser directly

            var result = BeginCallUser()
                .PostJson("User/ReactivateUser", new { userId })
                .ParseJsonAs<dynamic>();
            return Json(result);
        }

        [HttpPost]
        public ActionResult LoadUserList(bool loadDeletedUsers = false)
        {
            if (!NEnv.AllowAccessToLegacyUserAdmin)
                return HttpNotFound(); //New ui calls nUser directly

            var result = BeginCallUser().PostJson("User/GetAll", new { showDeleted = loadDeletedUsers }).ParseJsonAs<dynamic>();

            return Json(result);
        }

        [HttpPost]
        public ActionResult FetchServiceStatus(string serviceName)
        {
            var w = Stopwatch.StartNew();
            try
            {
                var response = NHttp
                    .Begin(new Uri(NEnv.ServiceRegistry.Internal[serviceName]), null, timeout: TimeSpan.FromSeconds(10))
                    .Get("hb");

                w.Stop();
                if (response.IsSuccessStatusCode)
                {
                    var result = response.ParseJsonAs<dynamic>();
                    return Json(new
                    {
                        status = result.status,
                        build = result.build,
                        responseTimeInMs = w.ElapsedMilliseconds
                    });
                }
                else
                {
                    var status = response.StatusCode.ToString();
                    return Json(new
                    {
                        status = status
                    });
                }
            }
            catch (Exception)
            {
                return Json(new
                {
                    status = "Down"
                });
            }
        }
    }
}