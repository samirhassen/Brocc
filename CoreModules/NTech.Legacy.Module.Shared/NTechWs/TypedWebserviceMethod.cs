using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace NTech.Services.Infrastructure.NTechWs
{
    public abstract class TypedWebserviceMethod<TRequest, TResponse> : NTechWebserviceMethod
        where TRequest : class, new()
        where TResponse : class, new()
    {
        protected override ActionResult DoExecute(NTechWebserviceMethodRequestContext requestContext)
        {
            var result = DoExecuteTyped(requestContext, requestContext.ParseJsonRequest<TRequest>());
            return new ActionResult
            {
                JsonResult = JsonConvert.SerializeObject(
                    result,
                    Newtonsoft.Json.Formatting.None,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Converters = new List<JsonConverter> { new StringEnumConverter() } })
            };
        }

        public override Type RequestType
        {
            get
            {
                return typeof(TRequest);
            }
        }

        public override Type ResponseType
        {
            get
            {
                return typeof(TResponse);
            }
        }

        protected abstract TResponse DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, TRequest request);

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
