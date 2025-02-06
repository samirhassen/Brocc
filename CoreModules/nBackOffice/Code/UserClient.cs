using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;

namespace nBackOffice.Code
{
    public class UserClient
    {
        protected NHttp.NHttpCall Begin(string bearerToken = null, TimeSpan? timeout = null)
        {
            return NHttp.Begin(new Uri(NEnv.ServiceRegistry.Internal["nUser"]), bearerToken ?? NHttp.GetCurrentAccessToken(), timeout: timeout);
        }

        private class LoadUserSettingResult
        {
            public bool? HasValue { get; set; }
            public string Value { get; set; }
        }

        public T LoadUserSetting<T>(string userId, string settingName) where T : class
        {
            var rr = Begin()
                .PostJson("UserSetting/Load", new
                {
                    userId = userId,
                    settingName = settingName,
                })
                .ParseJsonAs<LoadUserSettingResult>();
            if (!rr.HasValue.HasValue)
            {
                throw new Exception("Failed to read user setting");
            }
            return (rr.HasValue.Value ? JsonConvert.DeserializeObject<T>(rr.Value) : default(T));
        }

        public void StoreUserSetting<T>(string userId, string settingName, T settingValue) where T : class
        {
            Begin()
                .PostJson("UserSetting/Store", new
                {
                    userId = userId,
                    settingName = settingName,
                    settingValue = settingValue == null ? null : JsonConvert.SerializeObject(settingValue)
                })
                .EnsureSuccessStatusCode();
        }

        public class AboutToExpireResult
        {
            public List<G2> groupsAboutToExpire { get; set; }

            public class G2
            {
                public string GroupName { get; set; }
                public string UserDisplayName { get; set; }
                public DateTime EndDate { get; set; }
            }
        }

        public AboutToExpireResult FetchGroupsAboutToExpire(Func<int?> getUserId)
        {
            if (!NEnv.AllowAccessToLegacyUserAdmin)
                throw new Exception("Not allowed");
            return Begin()
                .PostJson("GroupMembership/GroupsAboutToExpireCreatedByUser", new
                {
                    userId = getUserId()
                })
                .ParseJsonAs<AboutToExpireResult>();
        }

        public dynamic GetAllUsers()
        {
            return Begin()
                .PostJson("User/GetAll", new { })
                .ParseJsonAs<dynamic>();
        }
    }
}