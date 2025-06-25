using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Runtime.Caching;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using Newtonsoft.Json;
using nSavings.Code;
using nSavings.Code.nUser;
using nSavings.Code.Services;
using nSavings.Controllers.Api;
using NTech;
using NTech.Banking.Shared.BankAccounts.Fi;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;

namespace nSavings.Controllers
{
    public abstract class NController : Controller
    {
        private static readonly Lazy<MemoryCache> Cache = new(() => MemoryCache.Default);

        protected const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        protected string GetSavingsAccountLink(string savingsAccountNr)
        {
            return Url.Action("GotoSavingsAccount", "SavingsAccount", new { savingsAccountNr });
        }

        private ControllerServiceFactory serviceFactory;

        protected ControllerServiceFactory Service
        {
            get
            {
                return serviceFactory ??= new ControllerServiceFactory(
                    Url,
                    GetUserDisplayNameByUserId,
                    Clock,
                    new Lazy<INTechWsUrlService>(() =>
                        ApiHostController.ApiHost.Value));
            }
        }

        protected int GetCurrentUserIdWithTestSupport()
        {
            if (NEnv.IsProduction)
                return CurrentUserId;

            //Assumes we are past parsing this already
            var p = Request.InputStream.Position;
            Request.InputStream.Position = 0;
            using var ms = new MemoryStream();
            Request.InputStream.CopyTo(ms);
            Request.InputStream.Position = p;
            try
            {
                return JsonConvert.DeserializeAnonymousType(Encoding.UTF8.GetString(ms.ToArray()),
                    new { testUserId = (int?)null })?.testUserId ?? CurrentUserId;
            }
            catch (JsonSerializationException)
            {
                return CurrentUserId;
            }
        }

        /// <summary>
        /// Set in controller:
        ///  ViewBag.JsonInitialData = EncodeInitialData(new { [...] })
        ///
        /// Parse in cshtml:
        /// <script>
        ///   initialData = JSON.parse(atob('@Html.Raw(ViewBag.JsonInitialData)'))
        /// </script>
        ///
        /// </summary>
        protected static string EncodeInitialData<T>(T data)
        {
            return Convert.ToBase64String(
                Encoding.GetEncoding("iso-8859-1").GetBytes(JsonConvert.SerializeObject(data)));
        }

        public ClaimsIdentity Identity => User.Identity as ClaimsIdentity;

        public INTechCurrentUserMetadata GetCurrentUserMetadata()
        {
            return new NTechCurrentUserMetadataImpl(Identity);
        }

        protected IClock Clock { get; } = ClockFactory.SharedInstance;

        protected int CurrentUserId => int.Parse(Identity.FindFirst("ntech.userid").Value);

        protected string CurrentUserAuthenticationLevel => Identity.FindFirst("ntech.authenticationlevel").Value;

        public string InformationMetadata =>
            JsonConvert.SerializeObject(new
            {
                providerUserId = CurrentUserId,
                providerAuthenticationLevel = CurrentUserAuthenticationLevel,
                isSigned = false
            });

        protected static bool TryParseDataUrl(string dataUrl, out string mimeType, out byte[] binaryData)
        {
            var result = Regex.Match(dataUrl, @"data:(?<type>.+?);base64,(?<data>.+)");
            if (!result.Success)
            {
                mimeType = null;
                binaryData = null;
                return false;
            }

            mimeType = result.Groups["type"].Value.Trim();
            binaryData = Convert.FromBase64String(result.Groups["data"].Value.Trim());
            return true;
        }

        protected static string GetUserDisplayNameByUserId(string userId)
        {
            var c = new UserClient();
            var d = c.GetUserDisplayNamesByUserId();
            return d.TryGetValue(userId, out var value) ? value : $"User {userId}";
        }

        protected T WithCache<T>(string key, Func<T> produce) where T : class
        {
            if (Cache.Value.Get(key) is T val)
                return val;
            val = produce();
            Cache.Value.Set(key, val, DateTimeOffset.Now.AddMinutes(5));
            return val;
        }

        private string GetClaim(string name)
        {
            return (User?.Identity as ClaimsIdentity)?.FindFirst(name)?.Value;
        }

        protected void GetProviderUserProperties(List<string> errors, out bool isProvider, out string providerName,
            out int userId, out string informationMetadata)
        {
            var isProviderClaim = GetClaim("ntech.isprovider");
            if (string.IsNullOrWhiteSpace(isProviderClaim))
            {
                errors.Add("Missing claim ntech.isprovider");
                isProvider = false;
            }
            else
            {
                isProvider = (isProviderClaim ?? "false") == "true";
            }

            providerName = GetClaim("ntech.providername");
            if (string.IsNullOrWhiteSpace(providerName))
            {
                errors.Add("Missing claim ntech.providername");
            }

            var userIdClaim = GetClaim("ntech.userid");
            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                userId = 0;
                errors.Add("Missing claim ntech.userid");
            }
            else if (!int.TryParse(userIdClaim, out userId))
            {
                errors.Add("Malformed claim ntech.userid");
            }

            var providerAuthenticationLevel = GetClaim("ntech.authenticationlevel");
            if (string.IsNullOrWhiteSpace(providerAuthenticationLevel))
                errors.Add("Missing claim ntech.authenticationlevel");

            if (errors.Count == 0)
            {
                informationMetadata = JsonConvert.SerializeObject(new
                {
                    providerUserId = userId,
                    providerAuthenticationLevel = providerAuthenticationLevel,
                    isSigned = false
                });
            }
            else
            {
                informationMetadata = null;
            }
        }

        protected void GetSystemUserProperties(List<string> errors, out bool isSystemUser, out int userId,
            out string informationMetadata)
        {
            var isSystemUserClaim = GetClaim("ntech.issystemuser");

            if (string.IsNullOrWhiteSpace(isSystemUserClaim))
            {
                errors.Add("Missing claim ntech.issystemuser");
                isSystemUser = false;
            }
            else
            {
                isSystemUser = (isSystemUserClaim ?? "false") == "true";
            }

            var userIdClaim = GetClaim("ntech.userid");
            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                userId = 0;
                errors.Add("Missing claim ntech.userid");
            }
            else if (!int.TryParse(userIdClaim, out userId))
            {
                errors.Add("Malformed claim ntech.userid");
            }

            var providerAuthenticationLevel = GetClaim("ntech.authenticationlevel");
            if (string.IsNullOrWhiteSpace(providerAuthenticationLevel))
                errors.Add("Missing claim ntech.authenticationlevel");

            if (errors.Count == 0)
            {
                informationMetadata = JsonConvert.SerializeObject(new
                {
                    providerUserId = userId,
                    providerAuthenticationLevel = providerAuthenticationLevel,
                    isSigned = false
                });
            }
            else
            {
                informationMetadata = null;
            }
        }

        /// <summary>
        /// Shown to user on the next page load
        /// </summary>
        /// <param name="title"></param>
        /// <param name="text"></param>
        protected void ShowInfoMessageOnNextPageLoad(string title, string text, Uri link = null)
        {
            var s = this?.HttpContext?.Session;
            if (s != null)
            {
                s["infoMessageOnNextPageLoad"] = CreateUserMessage(title, text, link);
            }
        }

        protected string CreateLinkToSavingsAccountDetails(string savingsAccountNr)
        {
            var s = Url.Action("Index", "SavingsAccount", new { });
            if (s == null)
                throw new Exception("Routing broken for savings accounts");
            return s + $"#!/Details/{savingsAccountNr}";
        }

        private static dynamic CreateUserMessage(string title, string text, Uri link)
        {
            dynamic m = new ExpandoObject();
            m.Title = title;
            m.Text = text;
            m.Link = link == null ? null : link.AbsoluteUri;
            return m;
        }

        protected PartialViewResult CreatePartialViewUserMessage(string title, string text, Uri link = null)
        {
            ViewBag.Message = CreateUserMessage(title, text, link);
            return PartialView("UserMessage");
        }

        protected override ViewResult View(string viewName, string masterName, object model)
        {
            ConsumeInfoMessageOnNextPageLoad();
            return base.View(viewName, masterName, model);
        }

        protected override ViewResult View(IView view, object model)
        {
            ConsumeInfoMessageOnNextPageLoad();
            return base.View(view, model);
        }

        private void ConsumeInfoMessageOnNextPageLoad()
        {
            var s = HttpContext?.Session;
            if (s == null) return;
            ViewBag.Message = s["infoMessageOnNextPageLoad"];
            s["infoMessageOnNextPageLoad"] = null;
        }

        protected static string InferBankNameFromIbanFi(IBANFi iban)
        {
            try
            {
                return NEnv.IBANToBICTranslatorInstance.InferBankName(iban) ?? "Unknown";
            }
            catch
            {
                //If a new bank gets added we dont want the ui to crash. Better to just not show it.
                return "Unknown";
            }
        }

        protected static ActionResult Json2(object data)
        {
            return new JsonNetActionResult
            {
                Data = data
            };
        }
    }
}