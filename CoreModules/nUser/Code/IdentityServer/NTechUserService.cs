using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services.Default;
using NTech.Services.Infrastructure;
using nUser.DbModel;

namespace nUser.Code
{
    public class NTechUserService : UserServiceBase
    {
        public class NTechUser
        {
            public string Subject { get; set; }
            public string Name { get; set; }
            public DateTime? ConsentedDate { get; set; }
            public List<Tuple<string, string>> Claims { get; set; }
        }

        public static NTechUser GetUserByExternalId(UsersContext c, string provider, string id, ProviderMetadata md)
        {
            return GetUser(
                c
                    .Users
                    .Where(x => x.AuthenticationMechanisms.Any(y =>
                        !y.RemovedDate.HasValue
                        && y.AuthenticationProvider == provider
                        && y.UserIdentity == id)), md);
        }

        public static NTechUser GetUserWithUsernamePasswordLogin(UsersContext c, string username,
            string plaintextPassword, ProviderMetadata md)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(plaintextPassword))
                return null;

            var am = c
                .AuthenticationMechanisms
                .Include("User")
                .Include("User.GroupMemberships")
                .Where(x => !x.RemovedById.HasValue)
                .SingleOrDefault(x =>
                    x.AuthenticationType == "UsernamePassword" && x.AuthenticationProvider == "Local" &&
                    x.UserIdentity == username);

            if (am == null)
                return null;

            return PasswordHasher.IsValid(plaintextPassword, am.Credentials)
                ? GetUser(new[] { am.User }.AsQueryable(), md)
                : null;
        }

        public static IQueryable<UserServiceModel> GetUserQueryBase(IQueryable<User> all)
        {
            return all
                .Select(x => new UserServiceModel
                {
                    IsSystemUser = x.IsSystemUser,
                    DisplayName = x.DisplayName,
                    ProviderName = x.ProviderName,
                    Id = x.Id,
                    ConsentedDate = x.ConsentedDate,
                    Groups = x.GroupMemberships.Where(y =>
                            y.StartDate < DateTime.Now
                            && y.EndDate > DateTime.Now
                            && y.ApprovedDate.HasValue
                            && !y.GroupMembershipCancellation.Any(z => z.CommittedById.HasValue))
                        .Select(y => new UserServiceModel.GroupModel
                            { ForProduct = y.ForProduct, GroupName = y.GroupName })
                });
        }

        private static NTechUser GetUser(IQueryable<User> baseQuery, ProviderMetadata md)
        {
            var user = GetUserQueryBase(baseQuery).FirstOrDefault();
            if (user == null)
                return null;

            var u = new NTechUser
            {
                Subject = user.Id.ToString(),
                Name = user.DisplayName,
                ConsentedDate = user.ConsentedDate,
                Claims = new List<Tuple<string, string>>
                {
                    Tuple.Create("ntech.username", user.DisplayName)
                }
            };

            if (!string.IsNullOrWhiteSpace(user.ProviderName))
            {
                u.Claims.Add(Tuple.Create("ntech.isprovider", "true"));
                u.Claims.Add(Tuple.Create("ntech.issystemuser", "false"));
                u.Claims.Add(Tuple.Create("ntech.providername", user.ProviderName));

                //NOTE: Providers (external service users) dont have roles
            }
            else if (user.IsSystemUser)
            {
                u.Claims.Add(Tuple.Create("ntech.isprovider", "false"));
                u.Claims.Add(Tuple.Create("ntech.issystemuser", "true"));
            }
            else
            {
                u.Claims.Add(Tuple.Create("ntech.isprovider", "false"));
                u.Claims.Add(Tuple.Create("ntech.issystemuser", "false"));

                var expandedGroups = user.GetExpandedGroups();
                foreach (var g in expandedGroups)
                {
                    if (g.GroupName == "Admin")
                    {
                        u.Claims.Add(Tuple.Create("ntech.role", "Admin"));
                    }
                    else
                    {
                        u.Claims.Add(Tuple.Create("ntech.role", $"{g.ForProduct}.{g.GroupName}"));
                        switch (g.ForProduct)
                        {
                            case "ConsumerCreditFi":
                            {
                                //TODO: Add support for separate savings permission
                                u.Claims.Add(Tuple.Create("ntech.role", $"ConsumerSavingsFi.{g.GroupName}"));
                                if (NEnv.IsMortgageLoansEnabled)
                                {
                                    //TODO: Add support for separate mortgage loans permission
                                    u.Claims.Add(Tuple.Create("ntech.role", $"MortgageLoan.{g.GroupName}"));
                                }

                                break;
                            }
                            case "ConsumerCredit":
                                u.Claims.Add(Tuple.Create("ntech.role", $"ConsumerSavings.{g.GroupName}"));
                                break;
                        }
                    }
                }

                foreach (var productName in expandedGroups.Select(x => x.ForProduct).Distinct().ToList())
                {
                    u.Claims.Add(Tuple.Create("ntech.product", productName));
                }

                foreach (var groupName in expandedGroups.Select(x => x.GroupName).Distinct().ToList())
                {
                    u.Claims.Add(Tuple.Create("ntech.group", groupName));
                }
            }

            u.Claims.Add(Tuple.Create("ntech.authenticationlevel", md.ProviderAuthenticationLevel));

            u.Claims.Add(Tuple.Create("ntech.userid", user.Id.ToString()));

            return u;
        }

        public class ProviderMetadata
        {
            public string UserIdentityClaimName { get; set; }
            public string ProviderAuthenticationLevel { get; set; }
        }

        private ProviderMetadata GetProviderMetadataByExternalIdentity(ExternalIdentity id)
        {
            switch (id?.Provider)
            {
                case "Google":
                    return new ProviderMetadata
                    {
                        ProviderAuthenticationLevel = "UsernamePassword",
                        UserIdentityClaimName = "email"
                    };
                case "Windows":
                    return new ProviderMetadata
                    {
                        ProviderAuthenticationLevel = "ActiveDirectory",
                        UserIdentityClaimName = "name"
                    };
                case "AzureAd":
                    return new ProviderMetadata
                    {
                        ProviderAuthenticationLevel = "AzureAd",
                        UserIdentityClaimName = "oid"
                    };
                default:
                    return new ProviderMetadata
                    {
                        ProviderAuthenticationLevel = "UsernamePassword", //Assume the worst
                        UserIdentityClaimName = null
                    };
            }
        }

        private static string GetUserIdentityByProvider(ProviderMetadata p, ExternalIdentity id)
        {
            return p.UserIdentityClaimName != null
                ? id?.Claims?.Where(x => x.Type == p.UserIdentityClaimName).Select(x => x.Value).FirstOrDefault()
                : id?.ProviderId;
        }

        public override Task AuthenticateExternalAsync(ExternalAuthenticationContext context)
        {
            var providerName = context?.ExternalIdentity?.Provider;
            var metadata = GetProviderMetadataByExternalIdentity(context?.ExternalIdentity);
            var userId = GetUserIdentityByProvider(metadata, context?.ExternalIdentity);

            using (var db = new UsersContext())
            {
                var u = GetUserByExternalId(db, providerName, userId, metadata);
                if (u != null)
                {
                    context.AuthenticateResult = new AuthenticateResult(
                        u.Subject,
                        u.Name,
                        u.Claims.Select(x => new Claim(x.Item1, x.Item2)).ToArray(),
                        identityProvider: context.ExternalIdentity.Provider);
                }
            }

            return Task.FromResult(0);
        }

        public override Task AuthenticateLocalAsync(LocalAuthenticationContext context)
        {
            using (var db = new UsersContext())
            {
                var u = GetUserWithUsernamePasswordLogin(db, context?.UserName, context?.Password,
                    new ProviderMetadata { ProviderAuthenticationLevel = "UsernamePassword" });
                if (u != null)
                {
                    context.AuthenticateResult = new AuthenticateResult(
                        u.Subject,
                        u.Name,
                        ToClaims(u.Claims));
                }
            }

            return Task.FromResult(0);
        }

        private static Claim[] ToClaims(IEnumerable<Tuple<string, string>> t)
        {
            return t.Select(x => new Claim(x.Item1, x.Item2)).ToArray();
        }

        public override Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var uid = context?.Subject?.FindFirst("sub")?.Value;
            if (!IsActiveUser(uid)) return Task.FromResult(0);

            context.AllClaimsRequested = true;
            context.IssuedClaims = context.Subject.Claims;

            return Task.FromResult(0);
        }

        public static bool IsActiveUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return false;

            var key = $"IsActiveUser:{userId}";
            if (NTechCache.Get<string>(key) == "1")
                return true;

            if (!int.TryParse(userId, out var uid)) return false;

            bool isActive;
            using (var db = new UsersContext())
            {
                isActive = db.Users.Any(x => x.Id == uid);
            }

            if (isActive)
            {
                NTechCache.Set(key, "1", TimeSpan.FromMinutes(15));
            }

            return isActive;
        }
    }

    public class UserServiceModel
    {
        public bool IsSystemUser { get; set; }
        public string DisplayName { get; set; }
        public string ProviderName { get; set; }
        public int Id { get; internal set; }
        public DateTime? ConsentedDate { get; set; }
        public IEnumerable<GroupModel> Groups { get; set; }

        /// <summary>
        /// Ensures that if any Group exists on the format
        /// ForProduct: ConsumerCredit, GroupName: x or ForProduct: ConsumerCreditFi, GroupName: x
        /// We also ensure that the other is present. We are replacing ConsumerCreditFi with ConsumerCredit
        /// but want existing permissions and users sessions to keep working.
        /// Over time we can hopefully remove all use of ConsumerCreditFi and if we ever get it totally removed
        /// this code can be removed aswell
        /// </summary>
        public List<GroupModel> GetExpandedGroups()
        {
            return GetExpandedGroupsShared(Groups);
        }

        public static List<GroupModel> GetExpandedGroupsShared(IEnumerable<GroupModel> groups)
        {
            var result = new List<GroupModel>();
            if (groups == null)
                return result;

            var consumerCreditFiGroups = groups.Where(x => x.ForProduct == "ConsumerCreditFi").Select(x => x.GroupName)
                .ToHashSet();
            var consumerCreditGroups =
                groups.Where(x => x.ForProduct == "ConsumerCredit").Select(x => x.GroupName).ToHashSet();

            foreach (var group in groups)
            {
                result.Add(group);
                if (group.ForProduct == "ConsumerCreditFi" && !consumerCreditGroups.Contains(group.GroupName))
                    result.Add(new GroupModel { ForProduct = "ConsumerCredit", GroupName = group.GroupName });
                if (group.ForProduct == "ConsumerCredit" && !consumerCreditFiGroups.Contains(group.GroupName))
                    result.Add(new GroupModel { ForProduct = "ConsumerCreditFi", GroupName = group.GroupName });
            }

            return result;
        }

        public static IQueryable<UserServiceModel> FilterByExpandedGroup(IQueryable<UserServiceModel> query,
            string forProduct, string groupName)
        {
            if (forProduct == "ConsumerCredit" || forProduct == "ConsumerCreditFi")
                return query.Where(x => x.Groups.Any(y =>
                    (y.ForProduct == "ConsumerCredit" || y.ForProduct == "ConsumerCreditFi") &&
                    y.GroupName == groupName));
            return query.Where(x => x.Groups.Any(y => y.ForProduct == forProduct && y.GroupName == groupName));
        }

        public class GroupModel
        {
            public string ForProduct { get; set; }
            public string GroupName { get; set; }
        }
    }
}