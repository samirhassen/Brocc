using NTech.Services.Infrastructure.NTechWs;

namespace nSavings.WebserviceMethods
{
    public class KeyValueStoreGetMethod : TypedWebserviceMethod<KeyValueStoreGetMethod.Request, KeyValueStoreGetMethod.Response>
    {
        public override string Path => "KeyValueStore/Get";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, x =>
            {
                x.Require(y => y.Key);
                x.Require(y => y.KeySpace);
            });

            var s = requestContext.Service().KeyValueStore(requestContext.CurrentUserMetadataCore());

            return new Response
            {
                Value = s.GetValue(request.Key, request.KeySpace)
            };
        }

        public class Request
        {
            public string Key { get; set; }
            public string KeySpace { get; set; }
        }

        public class Response
        {
            public string Value { get; set; }
        }
    }
}