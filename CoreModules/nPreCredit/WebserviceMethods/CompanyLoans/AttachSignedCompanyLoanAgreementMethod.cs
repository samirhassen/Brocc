using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.CompanyLoans;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class AttachSignedCompanyLoanAgreementMethod : TypedWebserviceMethod<AttachSignedCompanyLoanAgreementMethod.Request, AttachSignedCompanyLoanAgreementMethod.Response>
    {
        public override string Path => "CompanyLoan/Attach-Signed-Agreement";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            string archiveKey;
            if (!string.IsNullOrWhiteSpace(request.DocumentArchiveKey))
            {
                archiveKey = request.DocumentArchiveKey;
            }
            else if (!string.IsNullOrWhiteSpace(request.DataUrl) && !string.IsNullOrWhiteSpace(request.Filename))
            {
                var dc = requestContext.Resolver().Resolve<IDocumentClient>();
                if (!FileUtilities.TryParseDataUrl(request.DataUrl, out var mimeType, out var data))
                {
                    return Error("Invalid DataUrl", errorCode: "invalidDataUrl");
                }
                archiveKey = dc.ArchiveStore(data, mimeType, request.Filename);
            }
            else
                return Error("Either DocumentArchiveKey or DataUrl+Filename is required", errorCode: "missingFile");

            var ai = requestContext.Resolver().Resolve<ApplicationInfoService>().GetApplicationInfo(request.ApplicationNr);
            var wasAgreementAccepted = false;
            requestContext.Resolver().Resolve<ICompanyLoanAgreementSignatureService>().
                OnSignatureEvent(ai,
                directUploadDocumentArchiveKey: archiveKey,
                observeAgreementAccepted: x => wasAgreementAccepted = x);

            return new Response
            {
                WasAgreementAccepted = wasAgreementAccepted
            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            public string DataUrl { get; set; }
            public string Filename { get; set; }
            public string DocumentArchiveKey { get; set; }
        }

        public class Response
        {
            public bool WasAgreementAccepted { get; set; }
        }
    }
}