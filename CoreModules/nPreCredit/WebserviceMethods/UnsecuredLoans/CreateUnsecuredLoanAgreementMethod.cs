using Newtonsoft.Json;
using nPreCredit.Code;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace nPreCredit.WebserviceMethods
{
    public class CreateUnsecuredLoanAgreementMethod : FileStreamWebserviceMethod<CreateUnsecuredLoanAgreementMethod.Request>
    {
        public override string Path => "Application/Create-Agreement-Pdf";

        public override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var a = requestContext.Resolver().Resolve<AgreementSigningProvider>();

            byte[] pdfBytes;
            bool isAdditionalLoanOffer;
            string failedMessage;

            if (!a.TryCreateAgreementPdf(request.ApplicationNr, request.ApplicantNr.Value, out pdfBytes, out isAdditionalLoanOffer, out failedMessage))
            {
                return Error("Could not create agreement", errorCode: "couldNotCreateAgreement");
            }

            return PdfFile(new MemoryStream(pdfBytes), downloadFileName: string.IsNullOrWhiteSpace(request.DownloadFilename) ? null : request.DownloadFilename);
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            [Required]
            public int? ApplicantNr { get; set; }

            public string DownloadFilename { get; set; }
        }
    }
}