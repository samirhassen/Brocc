using NTech.Services.Infrastructure.NTechWs;

namespace nSavings.WebserviceMethods
{
    public class KeyValueStoreRemoveMethod : TypedWebserviceMethod<KeyValueStoreRemoveMethod.Request, KeyValueStoreRemoveMethod.Response>
    {
        public override string Path => "KeyValueStore/Remove";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, x =>
            {
                x.Require(y => y.Key);
                x.Require(y => y.KeySpace);
            });

            var s = requestContext.Service().KeyValueStore(requestContext.CurrentUserMetadataCore());

            s.RemoveValue(request.Key, request.KeySpace);

            return new Response { };
        }

        public class Request
        {
            public string Key { get; set; }
            public string KeySpace { get; set; }
        }

        public class Response
        {

        }
    }
}