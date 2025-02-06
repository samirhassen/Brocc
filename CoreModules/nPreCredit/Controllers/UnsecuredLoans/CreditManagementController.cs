using nPreCredit.Code;
using nPreCredit.Code.Agreements;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    [NTechAuthorizeCreditMiddle]
    [RoutePrefix("CreditManagement")]
    public partial class CreditManagementController : NController
    {
        private AgreementSigningStatusWithPending GetAgreementSigningStatus(string applicationNr, IEnumerable<CreditApplicationOneTimeToken> tokens)
        {
            IList<CreditApplicationOneTimeToken> _;
            var h = DependancyInjection.Services.Resolve<ICreditApplicationTypeHandler>();
            return h.GetAgreementSigningStatusWithPending(applicationNr, tokens, out _);
        }

        [Route("TestThrowingError")]
        public ActionResult TestThrowingError(string errorMessage = "")
        {
            if (NEnv.IsProduction)
                return HttpNotFound();
            Exception ex = new Exception(errorMessage);
            NLog.Error(ex, "Testing logging an error with message {text}", errorMessage);
            throw ex;
        }

        [Route("TestCreateAgreementPdf")]
        [HttpGet]
        public ActionResult TestCreateAgreementPdf(string applicationNr)
        {
            if (NEnv.IsProduction)
                return HttpNotFound();
            bool isAdditionalLoanOffer;

            using (var context = Service.Resolve<IPreCreditContextFactoryService>().CreateExtended())
            {
                string msg;
                var tmp = AdditionalLoanSupport.HasAdditionalLoanOffer(applicationNr, context, out msg);
                if (!tmp.HasValue)
                    return Json2(new { success = false, failedMessage = msg });

                isAdditionalLoanOffer = tmp.Value;
            }
            var pdfBuilderFactory = DependancyInjection.Services.Resolve<LoanAgreementPdfBuilderFactory>();
            var pdfBuilder = pdfBuilderFactory.Create(isAdditionalLoanOffer);
            byte[] pdfBytes;
            string pdfErrorMessage;

            if (!isAdditionalLoanOffer)
            {
                AddCreditNrIfNeeded(applicationNr, "TestCreateAgreement");
            }
            if (!pdfBuilder.TryCreateAgreementPdf(out pdfBytes, out pdfErrorMessage, applicationNr))
            {
                return Content(pdfErrorMessage);
            }
            else
            {
                return new FileStreamResult(new MemoryStream(pdfBytes), "application/pdf");
            }
        }
    }
}