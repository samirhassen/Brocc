using Newtonsoft.Json;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.MortgageLoans;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class CreateMortgageLoanDualAgreementPdfMethod : FileStreamWebserviceMethod<CreateMortgageLoanDualAgreementPdfMethod.Request>
    {
        public override string Path => "MortgageLoan/Create-Dual-Agreement-Pdf";

        public override bool IsEnabled => NEnv.IsOnlyNonStandardMortgageLoansEnabled;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var r = requestContext.Resolver();

            var ai = r.Resolve<ApplicationInfoService>().GetApplicationInfo(request.ApplicationNr);
            var s = r.Resolve<IMortgageLoanDualAgreementService>();
            var printContext = s.GetPrintContext(ai, request.CustomerId.Value);
            var ms = s.CreateAgreementPdf(printContext, request.OverrideTemplateName, request.DisableTemplateCache);

            return PdfFile(ms, downloadFileName: string.IsNullOrWhiteSpace(request.DownloadFilename) ? null : request.DownloadFilename);
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            [Required]
            public int? CustomerId { get; set; }

            public string OverrideTemplateName { get; set; }
            public bool? DisableTemplateCache { get; set; }
            public string DownloadFilename { get; set; }
        }
    }
}