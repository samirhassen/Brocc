using nCustomerPages.Code;
using System.Collections.Generic;
using System.Net;
using System.Web.Mvc;

namespace nCustomerPages.Controllers.EmbeddedCustomerPages
{
    public class GetSecureMessagesUnreadCountController : EmbeddedCustomerPagesControllerBase
    {
        protected override bool IsEnabled => NEnv.IsStandardMlOrUlEnabled && NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.securemessages");

        [Route("api/embedded-customerpages/messages-unread-count")]
        [HttpPost]
        public ActionResult GetUnreadMessagesCount(string markAsReadByCustomerContext, List<string> onlyTheseChannelTypes, string channelType, string channelId)
        {
            var onlyTheseChannelTypesActual = SendSecureMessageController.GetRequestedChannelTypes(onlyTheseChannelTypes);
            if (channelType != null && !SendSecureMessageController.IsChannelAllowed(channelType))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "ChannelType not allowed");

            var c = new CustomerLockedCustomerClient(this.CustomerId);
            var result = c.GetUnreadByCustomerCount(markAsReadByCustomerContext, onlyTheseChannelTypesActual, channelType, channelId);

            return Json2(new { UnreadCount = result });
        }
    }
}