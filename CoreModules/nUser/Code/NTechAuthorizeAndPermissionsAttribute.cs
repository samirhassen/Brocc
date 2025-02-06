using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace nUser
{
    public class NTechAuthorizeAndPermissionsAttribute : System.Web.Mvc.AuthorizeAttribute
    {
        public string[] Permissions { get; set; }

        private static string[] SplitString(string original)
        {
            if (String.IsNullOrEmpty(original))
            {
                return new string[0];
            }

            var split = from piece in original.Split(',')
                        let trimmed = piece.Trim()
                        where !String.IsNullOrEmpty(trimmed)
                        select trimmed;
            return split.ToArray();
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var isAuthorized = base.AuthorizeCore(httpContext);

            if (isAuthorized && Permissions != null && Permissions.Length > 0)
            {
                var u = httpContext.User.Identity as System.Security.Claims.ClaimsIdentity;

                var userGroupNames = new HashSet<string>(((u?.FindAll("ntech.role")?.Select(x => x.Value)?.ToList()) ?? new List<string>())
                    .Select(x => x.Split('.').Last()));

                var userPermissions = Controllers.AuthorizeController.GetPermissionsByGroups(userGroupNames);

                //TODO: Log this
                if (!Permissions.All(x => userPermissions.Contains(x)))
                    isAuthorized = false;
            }

            if (Log.IsEnabled(Serilog.Events.LogEventLevel.Debug))
            {
                var userid = (httpContext.User.Identity as System.Security.Claims.ClaimsIdentity)?.FindFirst("ntech.userid")?.Value;
                NLog.Debug("User {userid} tried to access {Url} requring {permissions}. Access was {accessResult}",
                    userid, httpContext.Request.RawUrl, Permissions, isAuthorized ? "Granted" : "Denied");
            }

            return isAuthorized;
        }
    }
}