using Newtonsoft.Json;
using System;

namespace NTech.Services.Infrastructure.NTechWs
{
    public abstract class RawRequestWebserviceMethod<TResponse> : NTechWebserviceMethod
        where TResponse : class, new()
    {
        protected override ActionResult DoExecute(NTechWebserviceMethodRequestContext requestContext)
        {
            var result = DoExecuteRaw(requestContext, requestContext.RequestJson);
            return new ActionResult
            {
                JsonResult = JsonConvert.SerializeObject(
                    result,
                    Newtonsoft.Json.Formatting.None,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })
            };
        }

        public override Type RequestType
        {
            get
            {
                return typeof(string);
            }
        }

        public override Type ResponseType
        {
            get
            {
                return typeof(TResponse);
            }
        }

        protected abstract TResponse DoExecuteRaw(NTechWebserviceMethodRequestContext requestContext, string jsonRequest);

        protected TResponse Error(string errorMessage, int? httpStatusCode = null, string errorCode = null)
        {
            throw new NTechWebserviceMethodException(errorMessage)
            {
                ErrorCode = errorCode,
                ErrorHttpStatusCode = httpStatusCode,
                IsUserFacing = true
            };
        }
    }
}
