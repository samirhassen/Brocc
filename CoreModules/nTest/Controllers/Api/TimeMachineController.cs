using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.Email;
using System;
using System.Web.Mvc;

namespace nTest.Controllers
{
    [NTechApi]
    [RoutePrefix("Api")]
    public class TimeMachineController : NController
    {
        [Route("TimeMachine/GetCurrentTime")]
        [HttpPost]
        [AllowAnonymous]
        public ActionResult GetCurrentTime()
        {
            return Json2(new { currentTime = TimeMachine.SharedInstance.GetCurrentTime() });
        }

        [Route("TimeMachine/SetCurrentTime")]
        [HttpPost]
        public ActionResult SetCurrentTime(DateTimeOffset? currentTime)
        {
            TimeMachine.SharedInstance.SetTime(currentTime.Value, true);
            return Json2(new { currentTime = TimeMachine.SharedInstance.GetCurrentTime() });
        }

        [Route("TimeMachine/Reset")]
        [HttpPost()]
        public ActionResult Reset()
        {
            try
            {
                var currentDate = TimeMachine.SharedInstance.GetCurrentTime().Date;
                var c = new CreditDriverCreditClient();
                var td = c.GetMaxTransactionDate();

                if (td.HasValue && td.Value.Date > currentDate)
                    currentDate = td.Value.Date;

                currentDate = currentDate.AddHours(13);
                TimeMachine.SharedInstance.SetTime(currentDate, true);
                return Json2(new { });
            }
            catch (Exception ex)
            {
                return Json2(new { error = ex.Message });
            }
        }

        [Route("EmailProvider-Set-Down")]
        [HttpPost()]
        public ActionResult SetEmailProviderDown(bool isDown)
        {
            NTechEmailServiceFactory.OfflineSimulatingTestEmailService.SetIsDown(isDown);
            return Json2(new { isDown });
        }
    }
}