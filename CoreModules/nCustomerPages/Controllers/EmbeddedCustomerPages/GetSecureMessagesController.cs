using nCustomerPages.Code;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCustomerPages.Controllers.EmbeddedCustomerPages
{
    public class GetSecureMessagesController : EmbeddedCustomerPagesControllerBase
    {
        protected override bool IsEnabled => NEnv.IsStandardMlOrUlEnabled && NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.securemessages");

        [Route("api/embedded-customerpages/messages")]
        [HttpPost]
        public ActionResult SendSecureMessage(int? takeCount, int? skipCount,
            List<string> onlyTheseChannelTypes, string markAsReadByCustomerContext,
            string channelType, string channelId)
        {
            var onlyTheseChannelTypesActual = SendSecureMessageController.GetRequestedChannelTypes(onlyTheseChannelTypes);

            if (channelType != null && !SendSecureMessageController.IsChannelAllowed(channelType))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "ChannelType not allowed");

            var c = new CustomerLockedCustomerClient(this.CustomerId);
            var result = c.GetMessages(new CustomerLockedCustomerClient.GetMessagesRequest
            {
                IncludeChannels = true,
                IncludeMessageTexts = true,
                TakeCount = takeCount ?? 15,
                SkipCount = skipCount ?? 0,
                OnlyTheseChannelTypes = onlyTheseChannelTypesActual,
                ChannelType = channelType,
                ChannelId = channelId
            });

            var toCustomerMessages = result?.Messages?.Where(x => !x.IsFromCustomer);
            if (!string.IsNullOrWhiteSpace(markAsReadByCustomerContext) && toCustomerMessages?.Count() > 0)
            {
                var latestId = toCustomerMessages.Max(x => x.Id);
                c.MarkMessagesAsReadByCustomer(markAsReadByCustomerContext, latestId);
            }

            return Json2(result);
        }
    }
}