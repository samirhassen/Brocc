using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class CreateAgreementPdfMethod : FileStreamWebserviceMethod<CreateAgreementPdfMethod.Request>
    {
        public override string Path => "UnsecuredLoanStandard/Create-Agreement-Pdf";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var r = requestContext.Resolver();

            var ai = r.Resolve<ApplicationInfoService>().GetApplicationInfo(request.ApplicationNr);
            if (ai == null)
                return Error("No such application found");

            var s = r.Resolve<UnsecuredLoanStandardAgreementService>();
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