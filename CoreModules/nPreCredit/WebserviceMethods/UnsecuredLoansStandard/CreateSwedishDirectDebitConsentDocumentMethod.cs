using nPreCredit.Code.Services;
using NTech.Banking.BankAccounts.Se;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class CreateSwedishDirectDebitConsentDocumentMethod : TypedWebserviceMethod<CreateSwedishDirectDebitConsentDocumentMethod.Request, CreateSwedishDirectDebitConsentDocumentMethod.Response>
    {
        public override string Path => "DirectDebit/Create-Unsigned-Consent-Pdf";

        public override bool IsEnabled => NEnv.ClientCfg.Country.BaseCountry == "SE" && NEnv.IsStandardUnsecuredLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var ai = requestContext.Resolver().Resolve<ApplicationInfoService>().GetApplicationInfo(request.ApplicationNr, true);
            if (ai == null)
                return Error("No such application");

            BankAccountNumberSe directDebitBankAccountNrOverride = null;
            if (!string.IsNullOrWhiteSpace(request.DirectDebitBankAccountNr))
            {
                //NOTE: We dont read this and the applicantNr from the application since it's unclear if we want to generate this document before or after commiting these to the application.
                if (!BankAccountNumberSe.TryParse(request.DirectDebitBankAccountNr, out directDebitBankAccountNrOverride, out var _))
                    return Error("Invalid DirectDebitBankAccountNr");
            }

            var service = requestContext.Resolver().Resolve<SwedishDirectDebitConsentDocumentService>();

            if (!service.TryCreateUnsignedDirectDebitConsentPdfForApplication(ai, out var archiveKey, out var failedCode,
                directDebitBankAccountNrOverride: directDebitBankAccountNrOverride,
                directDebitApplicantNrOverride: request.DirectDebitApplicantNr))
            {
                return Error(failedCode, httpStatusCode: 400, errorCode: failedCode);
            }

            return new Response
            {
                DocumentArchiveKey = archiveKey,
                DocumentArchiveUrl = NEnv.ServiceRegistry.External.ServiceUrl("nDocument", "Archive/Fetch", Tuple.Create("key", archiveKey)).ToString()
            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
            public int? DirectDebitApplicantNr { get; set; }
            public string DirectDebitBankAccountNr { get; set; }
        }

        public class Response
        {
            public string DocumentArchiveKey { get; set; }
            public string DocumentArchiveUrl { get; set; }
        }
    }
}