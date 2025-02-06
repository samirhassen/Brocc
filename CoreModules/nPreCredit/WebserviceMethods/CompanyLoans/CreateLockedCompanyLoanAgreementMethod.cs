using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.CompanyLoans;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class CreateLockedCompanyLoanAgreementMethod : TypedWebserviceMethod<CreateLockedCompanyLoanAgreementMethod.Request, CreateLockedCompanyLoanAgreementMethod.Response>
    {
        public override string Path => "CompanyLoan/Create-Locked-Agreement";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var r = requestContext.Resolver();

            var ai = r.Resolve<ApplicationInfoService>().GetApplicationInfo(request.ApplicationNr);
            var s = r.Resolve<ICompanyLoanAgreementService>();
            var dc = r.Resolve<IDocumentClient>();

            //Create agreement
            var printContext = s.GetPrintContext(ai);
            var ms = s.CreateAgreementPdf(printContext);
            var archiveKey = dc.ArchiveStore(ms.ToArray(), "application/pdf", $"UnsignedAgreement-{request.ApplicationNr}.pdf");

            //Find the credit decision
            var d = FetchCompanyLoanCurrentCreditDecisionMethod.GetDecisionShared(request.ApplicationNr);

            if (d.Decision.CompanyLoanOffer == null)
                return Error("Current credit decision is not accepted", errorCode: "creditDecisionRejected");

            var lockedAgreement = r.Resolve<ILockedAgreementService>().LockAgreement(request.ApplicationNr, archiveKey, d.Decision.CompanyLoanOffer.LoanAmount.Value, d.DecisionId.Value);

            return new Response
            {
                LockedAgreement = lockedAgreement
            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
        }

        public class Response
        {
            public LockedAgreementModel LockedAgreement { get; set; }
        }
    }
}