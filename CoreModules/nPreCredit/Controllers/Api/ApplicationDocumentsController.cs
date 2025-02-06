using nPreCredit.Code.Services;
using NTech.Services.Infrastructure;
using System.Collections.Generic;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api/ApplicationDocuments")]
    public class ApplicationDocumentsController : NController
    {
        private readonly IApplicationDocumentService applicationDocumentService;

        public ApplicationDocumentsController(IApplicationDocumentService applicationDocumentService)
        {
            this.applicationDocumentService = applicationDocumentService;
        }

        [HttpPost]
        [Route("FetchForApplication")]
        public ActionResult FetchForApplication(string applicationNr, List<string> documentTypes)
        {
            return Json2(applicationDocumentService.FetchForApplication(applicationNr, documentTypes));
        }

        [HttpPost]
        [Route("FetchFreeformForApplication")]
        public ActionResult FetchFreeformForApplication(string applicationNr)
        {
            return Json2(applicationDocumentService.FetchFreeformForApplication(applicationNr));
        }

        [HttpPost]
        [Route("AddAndRemove")]
        public ActionResult Add(string applicationNr, string documentType, int? applicantNr, string dataUrl, string filename, int? documentIdToRemove, int? customerId, string documentSubType)
        {
            ApplicationDocumentModel addedDocument;
            string failedMessage;
            bool isOk;
            if (!documentIdToRemove.HasValue)
                isOk = applicationDocumentService.TryAddDocument(applicationNr, documentType, applicantNr, customerId, documentSubType, dataUrl, filename, out addedDocument, out failedMessage);
            else
                isOk = applicationDocumentService.TryAddDocumentAndRemoveExisting(applicationNr, documentType, applicantNr, customerId, documentSubType, dataUrl, filename, documentIdToRemove.Value, out addedDocument, out failedMessage);

            if (isOk)
                return Json2(addedDocument);
            else
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
        }

        [HttpPost]
        [Route("Remove")]
        public ActionResult Remove(string applicationNr, int documentId)
        {
            string failedMessage;
            if (applicationDocumentService.TryRemoveDocument(applicationNr, documentId, out failedMessage))
                return Json2(new { });
            else
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
        }

        [HttpPost]
        [Route("FetchSingle")]
        public ActionResult FetchSingle(string applicationNr, int documentId)
        {
            return Json2(applicationDocumentService.FetchSingle(applicationNr, documentId));
        }

        [HttpPost]
        [Route("UpdateMortgageLoanDocumentCheckStatus")]
        public ActionResult UpdateMortgageLoanDocumentCheckStatus(string applicationNr)
        {
            ApplicationDocumentCheckStatusUpdateResult result;
            string failedMessage;
            if (applicationDocumentService.TryUpdateMortgageLoanDocumentCheckStatus(applicationNr, out result, out failedMessage))
                return Json2(result);
            else
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
        }

        [HttpPost]
        [Route("SetVerified")]
        public ActionResult FetchSingle(string applicationNr, int documentId, bool isVerified)
        {
            return applicationDocumentService.TrySetDocumentVerified(applicationNr, documentId, isVerified, out var document)
                ? Json2(document)
                : new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Could not set verifed");
        }
    }
}