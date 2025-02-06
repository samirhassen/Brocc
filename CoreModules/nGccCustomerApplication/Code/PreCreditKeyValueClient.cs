using NTech.Services.Infrastructure;

namespace nGccCustomerApplication.Code
{
    public class PreCreditKeyValueClient
    {
        public enum KeySpaceCode
        {
            CustomerApplicationDocumentCheckSession,
        }

        private NHttp.NHttpCall Begin()
        {
            return NHttp.Begin(NEnv.ServiceRegistry.Internal.ServiceRootUri("nPreCredit"), NEnv.GetSelfCachingSystemUserBearerToken());
        }

        public void Set(string key, KeySpaceCode keySpace, string value)
        {
            Begin()
                .PostJson("api/KeyValueStore/Set", new { key = key, keySpace = keySpace.ToString(), value = value })
                .EnsureSuccessStatusCode();
        }

        public string Get(string key, KeySpaceCode keySpace)
        {
            return Begin()
                .PostJson("api/KeyValueStore/Get", new { key = key, keySpace = keySpace.ToString() })
                .ParseJsonAsAnonymousType(new { value = (string)null })
                ?.value;
        }

        public void Remove(string key, KeySpaceCode keySpace)
        {
            Begin()
                .PostJson("api/KeyValueStore/Remove", new { key = key, keySpace = keySpace.ToString() })
                .EnsureSuccessStatusCode();
        }
    }
}