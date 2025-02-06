using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods
{
    public class UpdateBankAccountDataShareDataMethod : TypedWebserviceMethod<UpdateBankAccountDataShareDataMethod.Request, UpdateBankAccountDataShareDataMethod.Response>
    {
        public override string Path => "BankAccountDataShare/Update-Data";

        public override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            requestContext.Resolver().Resolve<IBankAccountDataShareService>().OnDataShared(request.ApplicationNr, request.ApplicantNr.Value, request.RawDataArchiveKey, request.PdfPreviewArchiveKey);

            return new Response
            {

            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            [Required]
            public int? ApplicantNr { get; set; }

            public string RawDataArchiveKey { get; set; }

            public string PdfPreviewArchiveKey { get; set; }
        }

        public class Response
        {

        }
    }
}