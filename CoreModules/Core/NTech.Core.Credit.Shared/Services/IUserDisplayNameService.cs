using System.Collections.Generic;

namespace nCredit.Code.Services
{
    public interface IUserDisplayNameService
    {
        string GetUserDisplayNameByUserId(string userId);
        Dictionary<string, string> GetUserDisplayNamesByUserId();
    }
}