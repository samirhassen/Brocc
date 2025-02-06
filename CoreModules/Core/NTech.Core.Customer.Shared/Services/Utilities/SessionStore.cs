using Dapper;
using nCustomer.Code.Services;
using Newtonsoft.Json;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Linq;

namespace NTech.Core.Customer.Shared.Services.Utilities
{
    public class SessionStore<TSession> where TSession : class
    {
        private readonly string sessionKeySpaceName;
        private readonly string expirationDateKeySpaceName;
        private readonly Func<TSession, string> getSessionId;
        private readonly Func<ICustomerContext> createCustomerContext;
        private readonly string alternateKeySpaceName;
        private readonly ICoreClock clock;

        public SessionStore(string sessionKeySpaceName, string expirationDateKeySpaceName, string alternateKeySpaceName,
            ICoreClock clock, Func<TSession, string> getSessionId, Func<ICustomerContext> createCustomerContext)
        {
            this.sessionKeySpaceName = sessionKeySpaceName;
            this.expirationDateKeySpaceName = expirationDateKeySpaceName;
            this.getSessionId = getSessionId;
            this.createCustomerContext = createCustomerContext;
            this.alternateKeySpaceName = alternateKeySpaceName;
            this.clock = clock;
        }

        public void StoreSession(TSession session, TimeSpan expirationDuration, INTechCurrentUserMetadata user)
        {
            using (var context = createCustomerContext())
            {
                StoreSessionComposable(context, session, expirationDuration, user);
                context.SaveChanges();
            }
        }

        public void StoreSessionComposable(ICustomerContext context, TSession session, TimeSpan expirationDuration, INTechCurrentUserMetadata user)
        {
            KeyValueStoreService.SetValueComposable(context, getSessionId(session), sessionKeySpaceName, JsonConvert.SerializeObject(session), user, clock);
            KeyValueStoreService.SetValueComposable(context, getSessionId(session), expirationDateKeySpaceName, clock.Now.Add(expirationDuration).ToString("o"), user, clock);
        }

        public void ArchiveOldSessions()
        {
            using (var context = createCustomerContext())
            {
                var earliestExpirationDateToDelete = clock.Now;
                var query =
@"with ItemsToDelete as
(
select  k.[Key]
from	KeyValueItem k
where	k.KeySpace = @archiveKeySpace
and		convert(datetimeoffset, k.[Value], 126) <= @earliestExpirationDateToDelete
)
delete from KeyValueItem where [KeySpace] in (@archiveKeySpace, @sessionKeySpace, @alternateKeySpace) and [Key] in (select d.[Key] from ItemsToDelete d)";

                context.GetConnection().Execute(query, param: new
                {
                    earliestExpirationDateToDelete,
                    archiveKeySpace = expirationDateKeySpaceName,
                    sessionKeySpace = sessionKeySpaceName,
                    alternateKeySpace = alternateKeySpaceName
                });

                context.SaveChanges();
            }
        }

        public TSession GetSession(string sessionId)
        {
            using (var context = createCustomerContext())
            {
                return GetSessionComposable(context, sessionId);
            }
        }

        public void SetAlternateSessionKey(string sessionId, string alternateSessionKey, INTechCurrentUserMetadata user)
        {
            using (var context = createCustomerContext())
            {
                KeyValueStoreService.SetValueComposable(context, alternateSessionKey, alternateKeySpaceName, sessionId, user, clock);
                context.SaveChanges();
            }
        }
        public TSession GetSessionComposable(ICustomerContext context, string sessionId)
        {
            var raw = KeyValueStoreService.GetValueComposable(context, sessionId, sessionKeySpaceName);
            return raw == null ? null : JsonConvert.DeserializeObject<TSession>(raw);
        }

        public TSession GetSessionByAlternateKey(string alternateKey)
        {
            using (var context = createCustomerContext())
            {
                var sessionId = context.KeyValueItemsQueryable
                    .Where(x => x.KeySpace == alternateKeySpaceName && x.Value == alternateKey)
                    .Select(x => x.Value).FirstOrDefault();
                return sessionId == null ? null : GetSessionComposable(context, sessionId);
            }
        }
    }
}
