using Newtonsoft.Json;
using nPreCredit.Code.Services.MortgageLoans;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.MortgageLoans
{
    public class CreateMortgageLoanApplicationAndPoaPdfMethod : FileStreamWebserviceMethod<CreateMortgageLoanApplicationAndPoaPdfMethod.Request>
    {
        public override string Path => "MortgageLoan/Create-Application-Poa-Pdf";

        public override bool IsEnabled => NEnv.IsOnlyNonStandardMortgageLoansEnabled;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, CreateMortgageLoanApplicationAndPoaPdfMethod.Request request)
        {
            ValidateUsingAnnotations(request);

            if (request.OnlyPoaForBankName != null && request.OnlyApplication.HasValue)
            {
                return Error("OnlyPoaForBankName and OnlyApplication cannot be combined", errorCode: "combinedOnlyPoaForBankNameOnlyApplication");
            }

            var r = requestContext.Resolver();

            var s = r.Resolve<IMortgageLoanDualApplicationAndPoaService>();
            if (!s.TryGetPrintContext(request.ApplicationNr, request.ApplicantNr.Value, request.OnlyApplication.GetValueOrDefault(), request.OnlyPoaForBankName, out var printContext, out var failedMessage))
                return Error(failedMessage);
            var ms = s.CreateApplicationAndPoaDocument(printContext);

            return PdfFile(ms, downloadFileName: string.IsNullOrWhiteSpace(request.DownloadFilename) ? null : request.DownloadFilename);
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            [Required]
            public int? ApplicantNr { get; set; }

            public string OverrideTemplateName { get; set; }
            public bool? DisableTemplateCache { get; set; }
            public string DownloadFilename { get; set; }
            public bool? OnlyApplication { get; set; }
            public string OnlyPoaForBankName { get; set; }
        }
    }
}