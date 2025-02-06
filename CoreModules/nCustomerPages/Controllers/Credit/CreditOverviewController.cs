using nCustomerPages.Code;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;

namespace nCustomerPages.Controllers.Credit
{
    [RoutePrefix("credit")]
    [CustomerPagesAuthorize(Roles = LoginProvider.CreditCustomerRoleName)]
    public class CreditOverviewController : CreditBaseController
    {
        [Route("overview")]
        [PreventBackButton]
        public ActionResult Index()
        {
            ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
            {
                apiUrls = new
                {
                    accountTransactions = Url.Action("GetCreditTransactions", "CreditOverview"),
                    creditDetails = Url.Action("GetCreditDetails", "CreditOverview"),
                    credits = Url.Action("GetCredits", "CreditOverview"),
                    openNotifications = Url.Action("GetOpenNotifications", "CreditOverview"),
                    accountdocuments = Url.Action("GetDocuments", "CreditOverview"),
                    getAmortizationPlanPdfUrl = Url.Action("GetAmortizationPlanPdf", "CreditOverview"),
                },
                productsOverviewUrl = Url.Action("Index", "ProductOverview"),
                translation = GetTranslations()
            })));

            return View();
        }

        [Route("overview/api/credits")]
        [HttpPost]
        public ActionResult GetCredits()
        {
            var c = CreateCustomerLockedCreditClient();
            var result = c.GetCredits();
            return Json2(result);
        }

        [Route("overview/api/opennotifications")]
        [HttpPost]
        public ActionResult GetOpenNotifications()
        {
            var c = CreateCustomerLockedCreditClient();
            var resultNotifications = c
                .GetOpenNotifications()
                ?.Notifications
                .Select(x => new
                {
                    x.CreditNr,
                    x.DueDate,
                    x.Id,
                    x.IsOverdue,
                    x.OcrPaymentReference,
                    x.PaymentIban,
                    x.TotalUnpaidNotifiedAmount,
                    Documents = x.Documents.Select(y => new
                    {
                        y.DocumentId,
                        y.DocumentType,
                        DocumentUrl = Url.Action("GetCreditDocument", "CreditOverview", new
                        {
                            documentType = y.DocumentType,
                            documentId = y.DocumentId
                        })
                    }),
                    x.InitialNotifiedAmount,
                    x.LatestPaymentDate,
                    x.IsOpen
                });

            return Json2(new { Notifications = resultNotifications });
        }

        [Route("overview/api/credit/details")]
        [HttpPost]
        public ActionResult GetCreditDetails(string creditNr, int? maxTransactionsCount, int? startBeforeTransactionId)
        {
            var c = CreateCustomerLockedCreditClient();
            var result = c.GetCreditDetails(creditNr, maxTransactionsCount, startBeforeTransactionId);
            return Json2(result);
        }

        [Route("overview/api/credit/transactions")]
        [HttpPost]
        public ActionResult GetCreditTransactions(string creditNr, int? maxTransactionsCount, int? startBeforeTransactionId)
        {
            var c = CreateCustomerLockedCreditClient();
            var transactions = c.GetCreditTransactions(creditNr, maxTransactionsCount, startBeforeTransactionId);
            return Json2(new { Transactions = transactions });
        }

        [Route("overview/api/creditdocument/{documentType}/{documentId}")]
        [HttpGet()]
        public ActionResult GetCreditDocument(string documentType, string documentId)
        {
            if (string.IsNullOrWhiteSpace(documentType))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing documentType");
            if (string.IsNullOrWhiteSpace(documentId))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing documentId");

            var c = CreateCustomerLockedCreditClient();
            string contentType;
            string fileName;
            byte[] content;
            if (c.TryFetchCreditDocument(documentType, documentId, out contentType, out fileName, out content))
            {
                var r = new FileStreamResult(new MemoryStream(content), contentType)
                {
                    FileDownloadName = fileName
                };
                return r;
            }
            else
            {
                return HttpNotFound();
            }
        }

        [HttpGet]
        [Route("overview/api/creditdocument")]
        public ActionResult GetCreditDocument(string archiveKey)
        {
            var dc = new SystemUserDocumentClient();

            var bytes = dc.FetchRawWithFilename(archiveKey, out var contentType, out var _);
            if (bytes != null)
                return File(bytes, contentType);
            else
                return HttpNotFound();
        }

        [Route("overview/api/accountdocuments")]
        [HttpPost]
        public ActionResult GetDocuments()
        {
            var c = CreateCustomerLockedCreditClient();
            var result = c.GetCreditsAccountDocuments();
            foreach (var item in result.Documents)
            {
                item.DownloadUrl = Url.Action("GetCreditDocument", "CreditOverview", new { archiveKey = item.ArchiveKey });
            }

            return Json2(result);
        }

        [Route("overview/api/GetAmortizationPlanPdf")]
        [HttpGet]
        public ActionResult GetAmortizationPlanPdf(string creditNr)
        {
            var c = CreateCustomerLockedCreditClient();
            var result = c.GetAmortizationPlanPdf(creditNr);
            return File(result, "application/pdf");

        }
    }
}