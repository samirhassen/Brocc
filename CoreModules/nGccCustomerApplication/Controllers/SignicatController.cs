using Newtonsoft.Json;
using nGccCustomerApplication.Code;
using nGccCustomerApplication.Code.Clients;
using nGccCustomerApplication.Controllers.Login;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace nGccCustomerApplication.Controllers
{
    public class SignicatController : AnonymousBaseController
    {
        //private readonly Func<string> getBearerToken;
        //public SignicatController(Func<string> getBearerToken)
        //{
        //    this.getBearerToken = getBearerToken;
        //}
        //private NHttp.NHttpCall Begin(TimeSpan? timeout = null)
        //{
        //    return NHttp
        //        .Begin(
        //            NEnv.ServiceRegistry.Internal.ServiceRootUri("nPreCredit"),
        //            getBearerToken(),
        //            timeout ?? TimeSpan.FromSeconds(45));
        //}
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!NEnv.IsEmbeddedSiteEidLoginApiEnabled)
            {
                filterContext.Result = HttpNotFound();
            }
            base.OnActionExecuting(filterContext);
        }

        private bool TryGetAbsoluteLocalUrl(string relativeLocalUrl, NTechServiceRegistry s, out Uri uri)
        {
            uri = null;

            if (string.IsNullOrWhiteSpace(relativeLocalUrl))
                return false;

            if (!relativeLocalUrl.All(x => Char.IsLetterOrDigit(x) || x == '/' || x == '-' || x == '#'))
                return false;

            var tempUri = s.External.ServiceUrl("nGccCustomerApplication", relativeLocalUrl);
            if (tempUri.Host != s.External.ServiceRootUri("nGccCustomerApplication").Host) //Guard against any sneaky url edgecases we missed
                return false;

            uri = tempUri;
            return true;
        }

        [Route("api/signicat/create-local-login-session")]
        public ActionResult CreateLocalLoginSession(string expectedCivicRegNr, string successRedirectUrl, string failedRedirectUrl, string customData)
        {
            if (!NEnv.BaseCivicRegNumberParser.TryParse(expectedCivicRegNr, out var parsedCivicRegNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid expectedCivicRegNr");

            var s = NEnv.ServiceRegistry;

            if (!TryGetAbsoluteLocalUrl(successRedirectUrl, s, out var parsedSuccessUrl))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid successUrl. Must be a relative local like 'a/b");
            if (!TryGetAbsoluteLocalUrl(failedRedirectUrl, s, out var parsedFailedUrl))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid successUrl. Must be a relative local like 'a/b");

            var client = new SystemUserCustomerClient();
            var returnUrl = NEnv.ServiceRegistry.External.ServiceUrl("nGccCustomerApplication", "login/eid/local-api/{localSessionId}/return");

            var localSession = client.CreateElectronicIdAuthenticationSession(parsedCivicRegNr.NormalizedValue, new Dictionary<string, string>
                {
                    { "Source", "[nGccCustomerApplication]/api/signicatcreate-local-login-session" },
                    { "LocalLoginSessionCustomData", customData },
                    { "LocalLoginSuccessUrl", parsedSuccessUrl.ToString() },
                    { "LocalLoginFailedUrl", parsedFailedUrl.ToString() }
                }, returnUrl.ToString());

            return Json2(new
            {
                SignicatInitialUrl = localSession.BeginLoginRedirectUrl
            });
        }

        
        // Need to show this API For Sir
        //[Route("api/signicat/complete-local-login-session")]
        //public ActionResult CompleteLocalLoginSession(string sessionId, string loginToken)
        //{
        //    //See the return method for why this is so wierd
        //    //var pc = new PreCreditClient(() => NEnv.SystemUserBearerToken);
        //    if (!TryGetTemporarilyEncryptedData(sessionId, out var data, removeAfter: true))
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such session exists");
        //    }

        //    var storedResult = JsonConvert.DeserializeAnonymousType(data, new { LocalSessionId = "", RequestParameters = (Dictionary<string, string>)null });

        //    var client = new SystemUserCustomerClient();
        //    var result = client.HandleElectronicIdAuthenticationProviderEvent(storedResult.LocalSessionId, storedResult.RequestParameters);

        //    if (!result.WasAuthenticated)
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Authentication failed");

        //    var session = result.Session;

        //    var key = CreateLoginSessionDataToken( new LoginSessionData
        //    {
        //        Version = CurrentLoginSessionDataVersion, //Change this if the format ever changes so readers can optionally choose to handle legacy values differently
        //        ProviderName = session.ProviderName,
        //        SessionId = session.LocalSessionId,
        //        UserInfo = session.AuthenticatedUser != null ? new LoginSessionData.UserInfoModel
        //        {
        //            CivicRegNr = session.AuthenticatedUser.CivicRegNumber,
        //            FirstName = session.AuthenticatedUser.FirstName,
        //            LastName = session.AuthenticatedUser.LastName
        //        } : null
        //    });

        //    return Json2(new
        //    {
        //        CivicRegNr = session.AuthenticatedUser.CivicRegNumber,
        //        FirstName = session.AuthenticatedUser.FirstName,
        //        LastName = session.AuthenticatedUser.LastName,
        //        LoginSessionDataToken = key, //Makes it possible to commit applications or similar including personal data without allow the user to tamper with them.
        //        CustomData = session.CustomData.Opt("LocalLoginSessionCustomData")
        //    });
        //}
        //[AllowAnonymous]
        //[HttpGet]
        //[Route("login/eid/local-api/{localSessionId}/return")]
        //public ActionResult Return(string localSessionId)
        //{
        //    /*
        //     This is a hack since all angular apps need to be changed to not be hardcoded to signicat. To make that happen without
        //     changes there we imitate the signicat flow facing them by faking loginToken and sessionId back to the angular apps.
             
        //     They will just call complete-local-login-session with these and we then use either one to get the
        //     actual values that we stored in encrypted storage. These are then sent to the common provider
        //     in nCustomer which will now work not just for signicat.
             
        //    If that is not clear, requestParameters will have the real loginToken and sessionId for signicat.
        //     */
        //    var client = new SystemUserCustomerClient();
        //    var localSession = client.GetElectronicIdAuthenticationSession(localSessionId);
        //    if (localSession == null || localSession.IsClosed)
        //        return Content("No such session exists");

        //    var pc = new PreCreditClient(() => NEnv.SystemUserBearerToken);

        //    var requestParameters = EidSignatureLoginController.QueryParamsAndExtras(Request);

        //    var key = pc.StoreTemporarilyEncryptedData(JsonConvert.SerializeObject(new
        //    {
        //        LocalSessionId = localSessionId,
        //        RequestParameters = requestParameters
        //    }), expireAfterHours: 8);
        //    var url = AppendQueryStringParams(new Uri(localSession.CustomData["LocalLoginSuccessUrl"]),
        //        Tuple.Create("sessionId", key),
        //        Tuple.Create("loginToken", key));
        //    return Redirect(url.ToString());
        //}

        private const string CurrentLoginSessionDataVersion = "201906111"; //Change this if the format ever changes so readers can optionally choose to handle legacy values differently

        public class LoginSessionData
        {
            public string Version { get; set; }
            public string ProviderName { get; set; }
            public string SessionId { get; set; }
            public UserInfoModel UserInfo { get; set; }

            public class UserInfoModel
            {
                public string CivicRegNr { get; set; }
                public string FirstName { get; set; }
                public string LastName { get; set; }
            }
        }

        //public static string CreateLoginSessionDataToken(PreCreditClient pc, LoginSessionData d)
        //{
        //    return pc.StoreTemporarilyEncryptedData(JsonConvert.SerializeObject(d), 2);
        //}

        //public static bool TryConsumeLoginSessionDataToken(string loginSessionDataToken, PreCreditClient pc, out LoginSessionData d)
        //{
        //    d = null;

        //    if (!pc.TryGetTemporarilyEncryptedData(loginSessionDataToken, out var plainText, removeAfter: true))
        //        return false;

        //    d = JsonConvert.DeserializeObject<LoginSessionData>(plainText);

        //    return true;
        //}

        private Uri AppendQueryStringParams(Uri uri, params Tuple<string, string>[] parameters)
        {
            var uriBuilder = new UriBuilder(uri);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            foreach (var p in parameters.Where(x => !string.IsNullOrWhiteSpace(x.Item2)))
                query[p.Item1] = p.Item2;

            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri;
        }
    }
}