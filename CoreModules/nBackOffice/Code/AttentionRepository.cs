using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace nBackOffice.Code
{
    public class AttentionRepository
    {
        private Func<string, bool> isUserInRole;
        private Func<int?> getUserId;
        UrlHelper url;

        public AttentionRepository(Func<string, bool> isUserInRole, Func<int?> getUserId, UrlHelper url)
        {
            this.getUserId = getUserId;
            this.isUserInRole = isUserInRole;
            this.url = url;
        }

        public class Attention
        {
            public string Text { get; set; }
            public string ActionUrl { get; set; }
        }

        //TODO: Cache and/or speed up
        public List<Attention> GetAttentions()
        {
            var attentions = new List<Attention>();
            if (isUserInRole("ConsumerCredit.High") && HasUserAdminTasks())
            {
                attentions.Add(new Attention
                {
                    Text = "Pending user changes!",
                    ActionUrl = url.Action("UserAdmin", "High")
                });
            }

            if (isUserInRole("Admin"))
            {
                var u = new UserClient();
                var gs = u.FetchGroupsAboutToExpire(getUserId);
                foreach (var g in gs.groupsAboutToExpire)
                {
                    attentions.Add(new Attention
                    {
                        Text = $"The membership in {g.GroupName} expires for user {g.UserDisplayName} at {g.EndDate.ToString("yyyy-MM-dd")}",
                        ActionUrl = url.Action("AdministerUsers", "Admin")
                    });
                }
            }

            return attentions;
        }

        public bool HasUserAdminTasks()
        {
            var u = new UserClient();
            var count1 = u.FetchGroupsAboutToExpire(getUserId)?.groupsAboutToExpire.Count;
            if (count1.HasValue && count1.Value > 0) return true;

            var count2 = FetchGroupsNeedingApproval()?.groupsNeedingApproval.Count;
            if (count2.HasValue && count2.Value > 0) return true;

            var count3 = FetchGroupMembershipCancellationsToCommit().Count;
            return count3 > 0;
        }

        public List<dynamic> FetchGroupMembershipCancellationsToCommit()
        {
            if (!NEnv.AllowAccessToLegacyUserAdmin)
                throw new Exception("Not allowed");

            return NHttp
                .Begin(new Uri(NEnv.ServiceRegistry.Internal["nUser"]), NHttp.GetCurrentAccessToken())
                .PostJson("GroupMembership/CancellationsToCommit", new { })
                .ParseJsonAs<List<dynamic>>();
        }

        public class NeedingApprovalResult
        {
            public List<dynamic> groupsNeedingApproval { get; set; }
        }

        public NeedingApprovalResult FetchGroupsNeedingApproval()
        {
            if (!NEnv.AllowAccessToLegacyUserAdmin)
                throw new Exception("Not allowed");

            return NHttp
                .Begin(new Uri(NEnv.ServiceRegistry.Internal["nUser"]), NHttp.GetCurrentAccessToken())
                .PostJson("GroupMembership/GroupsNeedingApproval", new { })
                .ParseJsonAs<NeedingApprovalResult>();
        }
    }
}