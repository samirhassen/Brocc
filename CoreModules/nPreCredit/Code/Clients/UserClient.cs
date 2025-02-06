using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Clients
{
    public interface IUserClient
    {
        Dictionary<string, string> GetUserDisplayNamesByUserId(bool? forceRefresh = false);
        List<int> GetAllUsersInGroup(string forProduct, string groupName);
        List<int> GetUsersIdsInMiddle();
    }

    public class UserClient : AbstractServiceClient, IUserClient
    {
        protected override string ServiceName => "nUser";

        public Dictionary<string, string> GetUserDisplayNamesByUserId(bool? forceRefresh = false)
        {
            Func<Dictionary<string, string>> fetch = () =>
            {
                return Begin()
                    .PostJson("User/GetAllDisplayNamesAndUserIds", new { })
                    .ParseJsonAs<GetUserDisplayNamesByUserIdResult[]>()
                    .ToDictionary(x => x.UserId, x => x.DisplayName);
            };

            const string CacheKey = "nPreCredit.GetUserDisplayNamesByUserId";
            var duration = TimeSpan.FromMinutes(15);
            if (forceRefresh.GetValueOrDefault())
            {
                var val = fetch();
                NTechCache.Set(CacheKey, val, duration);
                return val;
            }
            else
            {
                return NTechCache.WithCache(CacheKey, duration, fetch);
            }
        }

        public List<int> GetAllUsersInGroup(string forProduct, string groupName)
        {
            return Begin()
                .PostJson("GroupMembership/GetUserIdsInGroup", new { forProduct = forProduct, groupName = groupName })
                .ParseJsonAsAnonymousType(new { userIds = (List<int>)null })
                .userIds;
        }

        public List<int> GetUsersIdsInMiddle()
        {
            return Begin()
                .PostJson("GroupMembership/GetUsersIdsInMiddle", new { })
                .ParseJsonAsAnonymousType(new { userIds = (List<int>)null })
                ?.userIds ?? new List<int>();
        }

        private class GetUserDisplayNamesByUserIdResult
        {
            public string UserId { get; set; }
            public string DisplayName { get; set; }
        }
    }
}