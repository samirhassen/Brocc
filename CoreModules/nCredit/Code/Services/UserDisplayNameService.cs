using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;

namespace nCredit.Code.Services
{
    public class UserDisplayNameService : IUserDisplayNameService
    {
        private readonly IUserClient userClient;

        public UserDisplayNameService(IUserClient userClient)
        {
            this.userClient = userClient;
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
            return NTechCache.WithCache("nCredit.Controllers.NController.GetUserDisplayNamesByUserId", TimeSpan.FromMinutes(5), () => new UserClient().GetUserDisplayNamesByUserId());
        }
    }
}