using System;
using System.Collections.Generic;
using System.Linq;
using NTech.Services.Infrastructure;

namespace nSavings.Code.nUser;

public class UserClient : AbstractServiceClient, IUserClient
{
    protected override string ServiceName => "nUser";

    public string GetUserDisplayNameByUserId(string userId)
    {
        var d = GetUserDisplayNamesByUserId();
        return d.TryGetValue(userId, out var value) ? value : $"User {userId}";
    }

    public Dictionary<string, string> GetUserDisplayNamesByUserId()
    {
        return NTechCache.WithCache("nSavings.RealUserClient.GetUserDisplayNamesByUserId", TimeSpan.FromMinutes(15), () =>
        {
            var result = Begin()
                .PostJson("User/GetAllDisplayNamesAndUserIds", new { })
                .ParseJsonAs<GetUserDisplayNamesByUserIdResult[]>();
            return result.ToDictionary(x => x.UserId, x => x.DisplayName);
        });
    }

    private class GetUserDisplayNamesByUserIdResult
    {
        public string UserId { get; set; }
        public string DisplayName { get; set; }
    }
}