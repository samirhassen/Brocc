using Newtonsoft.Json;
using NTech;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace nScheduler.Controllers
{
    public abstract class NController : Controller
    {
        public ClaimsIdentity Identity
        {
            get
            {
                return this.User.Identity as ClaimsIdentity;
            }
        }

        public string CurrentUserAccessToken
        {
            get
            {

                var token = Identity?.FindFirst("access_token")?.Value;
                if (token == null)
                {
                    var h = this.Request.Headers["Authorization"];
                    if (h != null && h.StartsWith("Bearer"))
                    {
                        token = h.Substring("Bearer".Length).Trim();
                    }
                }

                return token;
            }
        }

        protected IClock Clock
        {
            get
            {
                return ClockFactory.SharedInstance;
            }
        }

        protected int CurrentUserId
        {
            get
            {
                return int.Parse(Identity.FindFirst("ntech.userid").Value);
            }
        }

        protected string CurrentUserAuthenticationLevel
        {
            get
            {
                return Identity.FindFirst("ntech.authenticationlevel").Value;
            }
        }

        public string InformationMetadata
        {
            get
            {
                return JsonConvert.SerializeObject(new
                {
                    providerUserId = CurrentUserId,
                    providerAuthenticationLevel = CurrentUserAuthenticationLevel,
                    isSigned = false
                });
            }
        }

        private class GetUserDisplayNamesByUserIdResult
        {
            public string UserId { get; set; }
            public string DisplayName { get; set; }
        }

        protected string GetUserDisplayNameByUserId(string userId)
        {
            var d = UserDisplayNamesByUserId;
            if (d.ContainsKey(userId))
                return d[userId];
            else
                return $"User {userId}";
        }

        protected Dictionary<string, string> UserDisplayNamesByUserId
        {
            get
            {
                return NTechCache.WithCache("nPreCredit.Controllers.NController.GetUserDisplayNamesByUserId", TimeSpan.FromMinutes(5), () =>
                {
                    return NHttp
                        .Begin(NEnv.ServiceRegistryNormal.Internal.ServiceRootUri("nUser"), NHttp.GetCurrentAccessToken())
                        .PostJson("User/GetAllDisplayNamesAndUserIds", new { })
                        .ParseJsonAs<GetUserDisplayNamesByUserIdResult[]>()
                        .ToDictionary(x => x.UserId, x => x.DisplayName);
                });
            }
        }

        protected ActionResult Json2(object data)
        {
            return new JsonNetResult
            {
                Data = data
            };
        }

        public class JsonNetResult : ActionResult
        {
            public Encoding ContentEncoding { get; set; }
            public string ContentType { get; set; }
            public object Data { get; set; }

            public JsonSerializerSettings SerializerSettings { get; set; }
            public Formatting Formatting { get; set; }

            public JsonNetResult()
            {
                SerializerSettings = new JsonSerializerSettings();
            }

            public override void ExecuteResult(ControllerContext context)
            {
                if (context == null)
                    throw new ArgumentNullException("context");

                HttpResponseBase response = context.HttpContext.Response;

                response.ContentType = !string.IsNullOrEmpty(ContentType)
                  ? ContentType
                  : "application/json";

                if (ContentEncoding != null)
                    response.ContentEncoding = ContentEncoding;

                if (Data != null)
                {
                    JsonTextWriter writer = new JsonTextWriter(response.Output) { Formatting = Formatting };

                    JsonSerializer serializer = JsonSerializer.Create(SerializerSettings);
                    serializer.Serialize(writer, Data);

                    writer.Flush();
                }
            }
        }
    }
}