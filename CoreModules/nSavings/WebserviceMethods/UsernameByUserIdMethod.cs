using nSavings.Code.nUser;
using NTech.Services.Infrastructure.NTechWs;

namespace nSavings.WebserviceMethods
{
    public class
        UsernameByUserIdMethod : TypedWebserviceMethod<UsernameByUserIdMethod.Request, UsernameByUserIdMethod.Response>
    {
        public override string Path => "UserName/ByUserId";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, x => { x.Require(y => y.UserId); });

            var client = new UserClient();

            return new Response
            {
                UserName = client.GetUserDisplayNameByUserId(request.UserId.ToString())
            };
        }

        public class Request
        {
            public int? UserId { get; set; }
        }

        public class Response
        {
            public string UserName { get; set; }
        }
    }
}