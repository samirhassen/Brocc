using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace nBackOffice
{
    public static class NTechClaimsIdentityExtensions
    {
        public const string UserIdClaimType = "ntech.userid";
        public const string RoleClaimType = "ntech.role";
        public const string PermissionClaimType = "ntech.permission";

        public static int? GetUserId(this ClaimsIdentity source)
        {
            var f = source.FindFirst(UserIdClaimType);
            if (f == null)
                return null;
            return int.Parse(f.Value);
        }

        public static IEnumerable<string> GetRoles(this ClaimsIdentity source)
        {
            return source.Claims.Where(x => x.Type == source.RoleClaimType).Select(x => x.Value);
        }

        public static List<string> GetPermissions(this ClaimsIdentity source)
        {
            return source
                .Claims
                .Where(x => x.Type == PermissionClaimType)
                .Select(x => x.Value)
                .ToList();
        }

        public static bool HasPermissions(this ClaimsIdentity source, params string[] requestedPermissions)
        {
            var actualPermissions = GetPermissions(source);
            return requestedPermissions.All(rp => actualPermissions.Contains(rp));
        }
    }
}