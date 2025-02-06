using Microsoft.AspNetCore.Mvc;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.Email;

namespace NTech.Core.Host.Controllers
{
    [ApiController]
    public class EmailTestController : Controller
    {
        private readonly INTechEnvironment environment;
        private readonly INTechEmailServiceFactory emailServiceFactory;

        public EmailTestController(INTechEnvironment environment, INTechEmailServiceFactory emailServiceFactory)
        {
            this.environment = environment;
            this.emailServiceFactory = emailServiceFactory;
        }

        /// <summary>
        /// Set the appsetting ntech.core.testemailapi.mailaddress to a valid email and this 
        /// will send a standardized testmail to that email. This is useful to verify the correct setup of a new
        /// mailprovider and probably not much else.
        /// </summary>
        [HttpPost]
        [Route("Api/Email/SendTestEmail")]
        public IActionResult Index()
        {
            var email = environment.OptionalSetting("ntech.core.testemailapi.mailaddress");
            if(email == null)
                throw new NTechCoreWebserviceException("No receiver email provided using ntech.core.testemailapi.mailaddress") { IsUserFacing = true, ErrorHttpStatusCode = 400, ErrorCode = "missingReceiverSetting" };

            if (!emailServiceFactory.HasEmailProvider)
                throw new NTechCoreWebserviceException("No active email provider") { IsUserFacing = true, ErrorHttpStatusCode = 400, ErrorCode = "noActiveMailProvider" };

            var emailService = emailServiceFactory.CreateEmailService();
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            emailService.SendRawEmail(new List<string> { email }, $"Testing ntech mail provider {now}",
                @"Test email<br>Sent {{now}}Sent<br>Charset test: ABCÅÄÖ123€", new Dictionary<string, object> { ["now"] = now }, "ApiTestEmail");

            return Ok();
        }
    }
}
