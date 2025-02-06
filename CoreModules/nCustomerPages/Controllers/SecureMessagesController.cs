using nCustomerPages.Code;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;
using static nCustomerPages.Code.CustomerLockedCustomerClient;

namespace nCustomerPages.Controllers
{
    [CustomerPagesAuthorize(Roles = LoginProvider.SavingsOrCreditCustomerRoleName)]
    public class SecureMessagesController : BaseController
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            ViewBag.CurrentPageProductGroup = "SecureMessages";
            base.OnActionExecuting(filterContext);
        }

        protected CustomerLockedCustomerClient CreateCustomerClient()
        {
            return new CustomerLockedCustomerClient(this.CustomerId);
        }

        [Route("messages")]
        [PreventBackButton]
        public ActionResult Index()
        {
            if (!NEnv.IsSecureMessagesEnabled)
                return HttpNotFound();

            var c = CreateCustomerClient();
            var messages = c.GetMessages(new CustomerLockedCustomerClient.GetMessagesRequest { IncludeChannels = true, IncludeMessageTexts = true, TakeCount = 15 });
            ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
            {
                translation = GetTranslations(),
                customerSecureMessages = messages,
                today = Clock.Now.DateTime,
                productOverviewUrl = Url.Action("Index", "ProductOverview")
            })));
            return View(messages);
        }

        [Route("Api/CustomerMessage/CreateMessage")]
        [HttpPost]
        public ActionResult CreateMessage(SendMessageRequest sendMessageRequest)
        {
            if (!NEnv.IsSecureMessagesEnabled)
                return HttpNotFound();

            var c = CreateCustomerClient();
            var result = c.SendMessage(sendMessageRequest);
            return Json2(result);
        }

        [Route("Api/CustomerMessage/GetMessages")]
        [HttpPost]
        public ActionResult GetMessages(GetMessagesRequest getMessageRequest)
        {
            if (!NEnv.IsSecureMessagesEnabled)
                return HttpNotFound();

            var c = CreateCustomerClient();
            var result = c.GetMessages(getMessageRequest);
            return Json2(result);
        }

        [Route("Api/CustomerMessage/GetFile")]
        [HttpGet]
        public ActionResult GetFile(string archiveKey)
        {

            var dc = new SystemUserDocumentClient();

            var bytes = dc.FetchRawWithFilename(archiveKey, out var contentType, out var fileName);
            if (bytes != null)
                return File(bytes, contentType, fileName);
            else
                return HttpNotFound();

        }

        [Route("Api/CustomerMessage/AttachMessageDocument")]
        [HttpPost]
        public ActionResult AttachMessageDocument(AttachMessageDocumentRequest attachMessageDocumentRequest)
        {
            if (!NEnv.IsSecureMessagesEnabled)
                return HttpNotFound();

            var c = CreateCustomerClient();
            var result = c.AttachMessage(attachMessageDocumentRequest);
            return Json2(result);
        }


    }
}