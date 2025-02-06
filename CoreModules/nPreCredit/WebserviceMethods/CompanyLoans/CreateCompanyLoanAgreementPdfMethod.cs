using Newtonsoft.Json;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.CompanyLoans;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class CreateCompanyLoanAgreementPdfMethod : FileStreamWebserviceMethod<CreateCompanyLoanAgreementPdfMethod.Request>
    {
        public override string Path => "CompanyLoan/Create-Agreement-Pdf";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, CreateCompanyLoanAgreementPdfMethod.Request request)
        {
            ValidateUsingAnnotations(request);
            var r = requestContext.Resolver();

            var ai = r.Resolve<ApplicationInfoService>().GetApplicationInfo(request.ApplicationNr);
            var s = r.Resolve<ICompanyLoanAgreementService>();
            var printContext = s.GetPrintContext(ai);
            var ms = s.CreateAgreementPdf(printContext, request.OverrideTemplateName, request.DisableTemplateCache);

            return PdfFile(ms, downloadFileName: string.IsNullOrWhiteSpace(request.DownloadFilename) ? null : request.DownloadFilename);
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
            public string OverrideTemplateName { get; set; }
            public bool? DisableTemplateCache { get; set; }
            public string DownloadFilename { get; set; }
        }
    }
}