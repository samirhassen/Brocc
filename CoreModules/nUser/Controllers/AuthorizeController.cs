using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace nUser.Controllers
{
    public class AuthorizeController : NController
    {
        public class Group
        {
            public string Name { get; set; }
        }

        public static ISet<string> GetPermissionsByGroups(ISet<string> groupNames)
        {
            var permissions = new HashSet<string>();
            foreach (var g in groupNames.Select(x => (x ?? "").ToLowerInvariant()))
            {
                switch (g)
                {
                    case "low":
                        permissions.Add("comment");
                        permissions.Add("personalDataRead");
                        permissions.Add("creditDataRead");
                        break;
                    case "middle":
                        permissions.Add("comment");
                        permissions.Add("personalDataEdit");
                        permissions.Add("creditDataEdit");
                        permissions.Add("approveAccountBegin");
                        permissions.Add("approveAccountCommit");
                        permissions.Add("editSystemVariableBegin");
                        permissions.Add("personalDataRead");
                        permissions.Add("creditDataRead");
                        break;
                    case "high":
                        permissions.Add("comment");
                        permissions.Add("personalDataEdit");
                        permissions.Add("creditDataEdit");
                        permissions.Add("approveAccountBegin");
                        permissions.Add("approveAccountCommit");
                        permissions.Add("approveCustomerBegin");
                        permissions.Add("approveCustomerCommit");
                        permissions.Add("editAdminBegin");
                        permissions.Add("editAdminCommit");
                        permissions.Add("editUserCommit");
                        permissions.Add("editSystemVariableBegin");
                        permissions.Add("editSystemVariableCommit");
                        permissions.Add("personalDataRead");
                        permissions.Add("creditDataRead");
                        permissions.Add("aggregatedData");
                        break;
                    case "economy":
                        permissions.Add("comment");
                        permissions.Add("creditDataRead");
                        permissions.Add("aggregatedData");
                        break;
                    case "admin":
                        permissions.Add("editUserBegin");
                        permissions.Add("editAdminBegin");
                        permissions.Add("personalDataRead");
                        permissions.Add("aggregatedData");
                        break;
                }
            }

            return permissions;
        }

        public static ISet<string> GetPermissionsByGroups(Group[] groups)
        {
            return GetPermissionsByGroups(
                new HashSet<string>(groups.GroupBy(x => (x.Name ?? "").ToLowerInvariant().Trim()).Select(x => x.Key)));
        }

        [AllowAnonymous]
        public ActionResult PermissionsByGroups(Group[] groups)
        {
            var permissions = GetPermissionsByGroups(groups);
            return Json2(permissions);
        }
    }
}