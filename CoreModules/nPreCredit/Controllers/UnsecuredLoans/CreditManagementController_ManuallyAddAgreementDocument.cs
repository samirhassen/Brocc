using nPreCredit.Code.Services;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    public partial class CreditManagementController
    {
        [Route("AddSignedAgreementDocument")]
        [HttpPost]
        public ActionResult AddSignedAgreementDocument(string applicationNr, int applicantNr, string attachedFileAsDataUrl, string attachedFileName)
        {
            var result = Api.ApiCreditApplicationAddSignedAgreementController.TryAddSignedAgreement(applicationNr, applicantNr, attachedFileAsDataUrl, attachedFileName, null, true, this.CurrentUserId, this.InformationMetadata, this.Clock, this.Service.Resolve<IApplicationCommentServiceComposable>());

            if (!result.Item1)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, result.Item2);
            }
            else
            {
                var wasAgreementStatusChanged = UpdateAgreementStatus(applicationNr);
                var wasCustomerCheckStatusChanged = UpdateCustomerCheckStatus(applicationNr);

                using (var context = new PreCreditContext())
                {
                    var applicationType = context
                        .CreditApplicationHeaders
                        .Where(x => x.ApplicationNr == applicationNr)
                        .Select(x => x.ApplicationType)
                        .Single();
                    return Json2(new
                    {
                        updatedAgreementSigningStatus = GetAgreementSigningStatus(applicationNr, context.CreditApplicationOneTimeTokens.Where(y => y.ApplicationNr == applicationNr && y.TokenType == "SignInitialCreditAgreement" && !y.RemovedBy.HasValue).ToList()),
                        wasAgreementStatusChanged = wasAgreementStatusChanged,
                        wasCustomerCheckStatusChanged = wasCustomerCheckStatusChanged
                    });
                }
            }
        }
    }
}