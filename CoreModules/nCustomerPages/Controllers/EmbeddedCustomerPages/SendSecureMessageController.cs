using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCustomerPages.Controllers.EmbeddedCustomerPages
{
    public class SendSecureMessageController : EmbeddedCustomerPagesControllerBase
    {
        protected override bool IsEnabled => NEnv.IsStandardMlOrUlEnabled && NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.securemessages");

        [Route("api/embedded-customerpages/send-secure-message")]
        [HttpPost]
        public ActionResult SendSecureMessage()
        {
            //We read the request this strange way to avoid JavaScriptSerializer size errors with images
            RequestModel request;
            Request.InputStream.Position = 0;
            using (var r = new StreamReader(Request.InputStream))
            {
                request = JsonConvert.DeserializeObject<RequestModel>(r.ReadToEnd());
            }

            var requestRaw = JObject.FromObject(request ?? new RequestModel());

            requestRaw.AddOrReplaceJsonProperty("IsFromCustomer", new JValue(true), true);

            if (!TrySetOrReplaceCustomerIdFromLoggedInUser(requestRaw))
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            if (!IsChannelAllowed(request?.ChannelType))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "ChannelType not allowed");

            var createResult = SendPartialForwardApiCallDirect(requestRaw, "nCustomer", "Api/CustomerMessage/CreateMessage");

            if (createResult.IsSuccessStatusCode)
            {
                var messageId = createResult.ParseJsonAsAnonymousType(new { CreatedMessage = new { Id = new int?() } })?.CreatedMessage?.Id;

                if (!string.IsNullOrWhiteSpace(request.AttachedFileAsDataUrl))
                {
                    SendPartialForwardApiCallDirect(JObject.FromObject(new
                    {
                        MessageId = messageId,
                        AttachedFileAsDataUrl = request.AttachedFileAsDataUrl,
                        AttachedFileName = request.AttachedFileName
                    }), "nCustomer", "Api/CustomerMessage/AttachMessageDocument");
                }

                return new RawJsonActionResult
                {
                    JsonData = createResult.ParseAsRawJson()
                };
            }
            else
                return new HttpStatusCodeResult(createResult.StatusCode, createResult.ReasonPhrase);
        }

        public static bool IsChannelAllowed(string channelType) => GetAllowedChannelTypes().Contains(channelType ?? "");

        public static List<string> GetRequestedChannelTypes(List<string> onlyTheseChannelTypes)
        {
            if (onlyTheseChannelTypes == null || onlyTheseChannelTypes.Count == 0)
                return GetAllowedChannelTypes().ToList();
            else
                return onlyTheseChannelTypes.Intersect(GetAllowedChannelTypes()).ToList();
        }

        private static HashSet<string> GetAllowedChannelTypes()
        {
            var h = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            h.Add("General");

            var cfg = NEnv.ClientCfg;

            if (cfg.IsFeatureEnabled("ntech.feature.unsecuredloans.standard"))
            {
                h.Add("Credit_UnsecuredLoan");
                h.Add("Application_UnsecuredLoan");
            }

            if (cfg.IsFeatureEnabled("ntech.feature.mortgageloans.standard"))
            {
                h.Add("Credit_MortgageLoan");
                h.Add("Application_MortgageLoan");
            }

            if (cfg.IsFeatureEnabled("ntech.feature.savingsstandard"))
                h.Add("SavingsAccount_StandardAccount");

            return h;
        }

        public class RequestModel
        {
            public string ChannelType { get; set; }
            public string ChannelId { get; set; }
            public string Text { get; set; }
            public string TextFormat { get; set; }
            public string AttachedFileAsDataUrl { get; set; }
            public string AttachedFileName { get; set; }
        }
    }
}