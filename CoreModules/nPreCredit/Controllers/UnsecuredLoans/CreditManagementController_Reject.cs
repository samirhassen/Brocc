using nPreCredit.Code.Services.LegacyUnsecuredLoans;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    public partial class CreditManagementController
    {
        [HttpPost]
        [Route("RejectApplication")]
        public ActionResult RejectApplication(string applicationNr, bool? wasAutomated)
        {
            try
            {
                var rejectionService = Resolver.Resolve<LegacyUnsecuredLoansRejectionService>();
                var result = rejectionService.RejectApplication(applicationNr, wasAutomated);
                return Json2(new
                {
                    redirectToUrl = Url.Action("CreditApplications"),
                    userWarningMessage = result.WasRejectionEmailFailed ? result.RejectionEmailFailedMessage : null
                });
            }
            catch (NTechCoreWebserviceException ex)
            {
                if (ex.IsUserFacing)
                {
                    return NTechWebserviceMethod.ToFrameworkErrorActionResult(
                           NTechWebserviceMethod.CreateErrorResponse(ex.Message, errorCode: ex.ErrorCode ?? "generic", httpStatusCode: ex.ErrorHttpStatusCode ?? 400));
                }
                else
                    throw;
            }
        }
    }
}