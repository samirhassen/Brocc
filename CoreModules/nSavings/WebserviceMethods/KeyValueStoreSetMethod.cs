using nSavings.Controllers.Api;
using NTech.Services.Infrastructure.NTechWs;

namespace nSavings.WebserviceMethods
{
    public class
        KeyValueStoreSetMethod : TypedWebserviceMethod<KeyValueStoreSetMethod.Request, KeyValueStoreSetMethod.Response>
    {
        public override string Path => "KeyValueStore/Set";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, x =>
            {
                x.Require(y => y.Key);
                x.Require(y => y.KeySpace);
                x.Require(y => y.Value);
            });

            var s = requestContext.Service().KeyValueStore(requestContext.CurrentUserMetadataCore());

            s.SetValue(request.Key, request.KeySpace, request.Value);

            return new Response();
        }

        public class Request
        {
            public string Key { get; set; }
            public string KeySpace { get; set; }
            public string Value { get; set; }
        }

        public class Response
        {
        }
    }
}