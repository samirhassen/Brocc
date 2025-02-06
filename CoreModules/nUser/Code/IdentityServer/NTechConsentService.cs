using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using NTech.Services.Infrastructure;
using nUser.DbModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace nUser.Code
{
    public class NTechConsentService : IdentityServer3.Core.Services.Default.DefaultConsentService
    {
        public NTechConsentService(IConsentStore store) : base(store)
        {

        }

        private static User GetUserByInternalId(UsersContext c, string userId)
        {
            int uid;
            if (int.TryParse(userId, out uid))
            {
                return c
                .Users
                .Where(x => x.Id == uid)
                .SingleOrDefault();
            }
            else
            {
                return null;
            }
        }

        private static bool HasConsented(string sub)
        {
            if (sub != null)
            {
                return NTechCache.WithCache($"HasConsented:{sub}", TimeSpan.FromMinutes(60), () =>
                {
                    using (var db = new UsersContext())
                    {
                        var user = GetUserByInternalId(db, sub);
                        return new { result = user != null && user.ConsentedDate.HasValue };
                    }
                }).result;
            }
            else
            {
                return false;
            }
        }

        public override Task<bool> RequiresConsentAsync(Client client, ClaimsPrincipal subject, IEnumerable<string> scopes)
        {
            var sub = subject?.FindFirst("sub")?.Value;
            if (sub != null)
            {
                var hasConsented = HasConsented(sub);
                if (hasConsented)
                    return Task.FromResult(false);
            }
            return base.RequiresConsentAsync(client, subject, scopes);
        }

        public override Task UpdateConsentAsync(Client client, ClaimsPrincipal subject, IEnumerable<string> scopes)
        {
            var sub = subject?.FindFirst("sub")?.Value;
            if (sub != null)
            {
                if (!HasConsented(sub))
                {
                    using (var db = new UsersContext())
                    {
                        var user = GetUserByInternalId(db, sub);
                        if (user != null && !user.ConsentedDate.HasValue)
                        {
                            user.ConsentedDate = DateTime.Now;
                            user.ConsentText = NEnv.ConsentText;
                            db.SaveChanges();
                        }
                    }
                }
            }
            return base.UpdateConsentAsync(client, subject, scopes);
        }
    }
}