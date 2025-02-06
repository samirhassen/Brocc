using System;
using System.Collections.Generic;

namespace nCustomer.Code.Services
{
    public class UserService : IUserService
    {
        private readonly Func<string, string> getUserDisplayNameByUserId;
        private readonly Func<Dictionary<string, string>> getUserDisplayNamesByUserId;

        public UserService(Func<string, string> getUserDisplayNameByUserId, Func<Dictionary<string, string>> getUserDisplayNamesByUserId)
        {
            this.getUserDisplayNameByUserId = getUserDisplayNameByUserId;
            this.getUserDisplayNamesByUserId = getUserDisplayNamesByUserId;
        }

        public string GetUserDisplayNameByUserId(string userId)
        {
            return getUserDisplayNameByUserId(userId);
        }

        public Dictionary<string, string> GetUserDisplayNamesByUserId()
        {
            return getUserDisplayNamesByUserId();
        }
    }

    public interface IUserService
    {
        string GetUserDisplayNameByUserId(string userId);
        Dictionary<string, string> GetUserDisplayNamesByUserId();
    }
}