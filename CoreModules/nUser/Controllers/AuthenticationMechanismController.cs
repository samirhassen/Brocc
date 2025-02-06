using NTech.Services.Infrastructure;
using nUser.DbModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nUser.Controllers
{
    public class AuthenticationMechanismController : NController
    {
        [NTechAuthorizeAndPermissions(Permissions = new[] { "editUserBegin" })]
        [HttpPost]
        public ActionResult Remove(int authenticationMechanismId)
        {
            using (var db = new UsersContext())
            {
                var am = db.AuthenticationMechanisms.Include("User").SingleOrDefault(x => x.Id == authenticationMechanismId);
                if (am == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such login method");

                if (!IsRemoveAuthenticationMechanismAllowed(am.User))
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Not allowed");

                am.RemovedById = CurrentUserId;
                am.RemovedDate = DateTime.Now;

                db.SaveChanges();

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }

        public static bool IsRemoveAuthenticationMechanismAllowed(User u)
        {
            //There is a security hole where an admin can move their own accounts around and get around duality
            //so we cannot allow regular users to remove their credentials once used.
            //Consented date is used as a proxy to know if the user has ever logged in.
            //System and provider users cannot create themselves so they are not included
            return u.IsSystemUser || u.ProviderName != null || !u.ConsentedDate.HasValue;
        }

        [NTechAuthorizeAndPermissions(Permissions = new[] { "editUserBegin" })]
        [HttpPost]
        public ActionResult Create(
            int userId,
            string adUsername,
            string providerEmail,
            string upwUsername,
            string upwPassword,
            string authenticationType,
            string providerName,
            string userIdentityAndCredentialsType,
            string providerObjectId)
        {
            if (string.IsNullOrWhiteSpace(authenticationType) || string.IsNullOrWhiteSpace(providerName) || string.IsNullOrWhiteSpace(userIdentityAndCredentialsType))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing provider");

            var activeMethods = GetActiveLoginMethodsI();

            if (!activeMethods.Any(x => x.AuthenticationType == authenticationType && x.ProviderName == x.ProviderName && x.UserIdentityAndCredentialsType == userIdentityAndCredentialsType))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid provider");

            using (var db = new UsersContext())
            {
                if (!db.Users.Any(x => x.Id == userId))
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such user exists");

                var newMech = new AuthenticationMechanism
                {
                    UserId = userId,
                    AuthenticationProvider = providerName,
                    AuthenticationType = authenticationType,
                    CreatedById = CurrentUserId,
                    CreationDate = DateTime.Now
                };

                if (db.AuthenticationMechanisms.Any(x => x.UserId == userId && x.AuthenticationProvider == providerName && !x.RemovedById.HasValue))
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "User already has an active login method for that provider");

                var v = new NTechUserNameValidator();

                if (userIdentityAndCredentialsType == ActiveLoginMethodIdentityAndCredentialsCode.LocalUserNameAndPassword.ToString())
                {
                    if (string.IsNullOrWhiteSpace(upwUsername))
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing username");

                    if (string.IsNullOrWhiteSpace(upwPassword))
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing password");

                    if (db.AuthenticationMechanisms.Any(x => x.UserIdentity == upwUsername && x.AuthenticationProvider == providerName && !x.RemovedById.HasValue))
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "That username is already in use for that provider");

                    if (!v.TryValidateUserName(upwUsername, NTechUserNameValidator.UserNameTypeCode.DisplayUserName, out var invalidUserNameMessage))
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Invalid username: {invalidUserNameMessage}");
                    }

                    newMech.UserIdentity = upwUsername;
                    newMech.Credentials = Code.PasswordHasher.Hash(upwPassword);
                }
                else if (userIdentityAndCredentialsType == ActiveLoginMethodIdentityAndCredentialsCode.FederationUsingEmail.ToString())
                {
                    if (string.IsNullOrWhiteSpace(providerEmail))
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing providerEmail");

                    if (db.AuthenticationMechanisms.Any(x => x.UserIdentity == providerEmail && x.AuthenticationProvider == providerName && !x.RemovedById.HasValue))
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "That email is already in use for that provider");
                    if (!v.TryValidateUserName(providerEmail, NTechUserNameValidator.UserNameTypeCode.EmailUserName, out var invalidUserNameMessage))
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Invalid email: {providerEmail}");
                    }

                    newMech.UserIdentity = providerEmail;
                }
                else if (userIdentityAndCredentialsType == ActiveLoginMethodIdentityAndCredentialsCode.FederationUsingObjectId.ToString())
                {
                    if (string.IsNullOrWhiteSpace(providerObjectId))
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing providerObjectId");

                    if (db.AuthenticationMechanisms.Any(x => x.UserIdentity == providerObjectId && x.AuthenticationProvider == providerName && !x.RemovedById.HasValue))
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "That object id is already in use for that provider");

                    newMech.UserIdentity = providerObjectId;
                }
                else if (userIdentityAndCredentialsType == ActiveLoginMethodIdentityAndCredentialsCode.FederatitionUsingADUsername.ToString())
                {
                    if (string.IsNullOrWhiteSpace(adUsername))
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing adUsername");

                    if (db.AuthenticationMechanisms.Any(x => x.UserIdentity == adUsername && x.AuthenticationProvider == providerName && !x.RemovedById.HasValue))
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "That username is already in use for that provider");

                    if (!v.TryValidateUserName(adUsername, NTechUserNameValidator.UserNameTypeCode.ActiveDirectoryUserName, out var invalidUserNameMessage))
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Invalid username: {invalidUserNameMessage}");
                    }

                    newMech.UserIdentity = adUsername;
                }
                else
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid userIdentityAndCredentialsType");
                }

                db.AuthenticationMechanisms.Add(newMech);
                db.SaveChanges();

                return Json2(new
                {
                    newMech.Id,
                    newMech.AuthenticationProvider,
                    newMech.AuthenticationType,
                    newMech.UserIdentity
                });
            }
        }

        private enum ActiveLoginMethodIdentityAndCredentialsCode
        {
            LocalUserNameAndPassword,
            FederationUsingEmail,
            FederatitionUsingADUsername,
            FederationUsingObjectId
        }

        private class ActiveLoginMethod
        {
            public string DisplayName { get; set; }
            public string ProviderName { get; set; }
            public string AuthenticationType { get; set; }
            public bool IsAllowedForProvider { get; set; }
            public bool IsAllowedForSystemUser { get; set; }
            public bool IsAllowedForRegularUser { get; set; }
            public string UserIdentityAndCredentialsType { get; set; }
        }

        private List<ActiveLoginMethod> GetActiveLoginMethodsI()
        {
            var methods = new List<ActiveLoginMethod>();

            var wl = NEnv.WindowsLogin;
            if (wl != null)
            {
                methods.Add(new ActiveLoginMethod
                {
                    DisplayName = "Windows Active Directory",
                    ProviderName = "Windows",
                    AuthenticationType = "ActiveDirectory",
                    IsAllowedForProvider = false,
                    IsAllowedForRegularUser = true,
                    IsAllowedForSystemUser = false, //TODO: This might be a good idea to make possible. The scheduler could run a an AD-user and then propagate the rights but this would require substansial changes to somehow propagate the permissions without using the windows auth site login
                    UserIdentityAndCredentialsType = ActiveLoginMethodIdentityAndCredentialsCode.FederatitionUsingADUsername.ToString()
                });
            }

            var gl = NEnv.GoogleLogin;
            if (gl != null)
            {
                methods.Add(new ActiveLoginMethod
                {
                    DisplayName = "Google",
                    ProviderName = "Google",
                    AuthenticationType = "WsFederation",
                    IsAllowedForProvider = false,
                    IsAllowedForSystemUser = false,
                    IsAllowedForRegularUser = true,
                    UserIdentityAndCredentialsType = ActiveLoginMethodIdentityAndCredentialsCode.FederationUsingEmail.ToString()
                });
            }

            var azureAd = NEnv.AzureAdLogin;
            if (azureAd != null)
            {
                methods.Add(new ActiveLoginMethod
                {
                    DisplayName = "Azure AD",
                    ProviderName = "AzureAd",
                    AuthenticationType = "OpenIdConnect",
                    IsAllowedForProvider = false,
                    IsAllowedForSystemUser = false,
                    IsAllowedForRegularUser = true,
                    UserIdentityAndCredentialsType = ActiveLoginMethodIdentityAndCredentialsCode.FederationUsingObjectId.ToString()
                });
            }

            methods.Add(new ActiveLoginMethod
            {
                DisplayName = "Username and password",
                ProviderName = "Local",
                AuthenticationType = "UsernamePassword",
                IsAllowedForProvider = true,
                IsAllowedForSystemUser = true,
                IsAllowedForRegularUser = NEnv.IsUsernamePasswordLoginEnabled,
                UserIdentityAndCredentialsType = ActiveLoginMethodIdentityAndCredentialsCode.LocalUserNameAndPassword.ToString()
            });

            return methods;
        }

        [HttpPost]
        public ActionResult GetActiveLoginMethods()
        {
            return Json2(GetActiveLoginMethodsI());
        }
    }
}