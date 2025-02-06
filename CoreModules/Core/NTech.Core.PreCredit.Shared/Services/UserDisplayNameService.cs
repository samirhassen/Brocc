using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;

namespace nPreCredit.Code.Services
{
    public class UserDisplayNameService : IUserDisplayNameService
    {
        private readonly IUserClient userClient;
        private readonly FewItemsCache cache;

        public UserDisplayNameService(IUserClient userClient, FewItemsCache cache)
        {
            this.userClient = userClient;
            this.cache = cache;
        }

        public string GetUserDisplayNameByUserId(string userId)
        {
            var d = GetUserDisplayNamesByUserId();
            if (d.ContainsKey(userId))
                return d[userId];
            else
                return $"User {userId}";
        }

        public Dictionary<string, string> GetUserDisplayNamesByUserId()
        {
            return cache.WithCache("nPreCredit.Controllers.NController.GetUserDisplayNamesByUserId",
                TimeSpan.FromMinutes(5),
                () => userClient.GetUserDisplayNamesByUserId());
        }
    }

    public interface IUserDisplayNameService
    {
        string GetUserDisplayNameByUserId(string userId);
        Dictionary<string, string> GetUserDisplayNamesByUserId();
    }
}