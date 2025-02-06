using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    [NTechAuthorizeCreditHigh(ValidateAccessToken = true)]
    public class ApiSendAlternatePaymentPlanMessagesController : NController
    {
        private ActionResult SendAlternatePaymentPlanMessagesI()
        {
            try
            {
                var alternatePaymentPlanSecureMessagesService = Service.AlternatePaymentPlanSecureMessages;
                var (SuccessCount, Warnings, Errors) = alternatePaymentPlanSecureMessagesService.SendEnabledSecureMessages();

                return Json2(new
                {
                    SuccessCount,
                    Warnings,
                    Errors
                });
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "SendAlternatePaymentPlanMessages crashed");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [HttpPost]
        [Route("Api/Credit/SendAlternatePaymentPlanMessages")]
        public ActionResult SendAlternatePaymentPlanMessages()
        {
            return CreditContext.RunWithExclusiveLock("ntech.scheduledjobs.sendalternatepaymentplanmessages",
                    () => SendAlternatePaymentPlanMessagesI(),
                    () => new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Job SendAlternatePaymentPlanMessages is already running")
            );
        }
        
    }
}