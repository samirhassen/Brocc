using nPreCredit.Code;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    public partial class CreditManagementController
    {
        [HttpPost]
        [Route("SendAdditionalQuestionsEmail")]
        public ActionResult SendAdditionalQuestionsEmail(string applicationNr)
        {
            var di = DependancyInjection.Services;

            var sender = di.Resolve<IAdditionalQuestionsSender>();
            var result = sender.SendSendAdditionalQuestionsEmail(applicationNr);
            return Json2(result);
        }
    }
}