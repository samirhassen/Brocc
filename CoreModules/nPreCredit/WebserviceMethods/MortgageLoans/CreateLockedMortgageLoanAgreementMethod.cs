using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.MortgageLoans;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.MortgageLoans
{
    public class CreateLockedMortgageLoanAgreementMethod : TypedWebserviceMethod<CreateLockedMortgageLoanAgreementMethod.Request, CreateLockedMortgageLoanAgreementMethod.Response>
    {
        public override string Path => "MortgageLoan/Audit-And-Create-Locked-Agreement";

        public override bool IsEnabled => NEnv.IsOnlyNonStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var r = requestContext.Resolver();

            var infoService = r.Resolve<ApplicationInfoService>();

            var ai = infoService.GetApplicationInfo(request.ApplicationNr);
            if (!ai.IsActive)
                return Error("Application is not active", errorCode: "applicationNotActive");

            if (ai.HasLockedAgreement)
                return Error("Application already has a locked agreement", errorCode: "alreadyHasLockedAgreement");

            var wf = r.Resolve<IMortgageLoanWorkflowService>();
            var currentListName = wf.GetCurrentListName(ai.ListNames);
            if (!wf.TryDecomposeListName(currentListName, out var n))
                return Error("Invalid current list");

            var currentStepName = n.Item1;

            var isAuditStep = wf.Model.GetCustomDataAsAnonymousType(currentStepName, new { IsAuditAgreement = (string)null })?.IsAuditAgreement == "yes";
            if (!isAuditStep)
                return Error("Must be on the audit step", errorCode: "notOnAuditStep");

            if (!wf.IsStepStatusInitial(currentStepName, ai.ListNames))
                return Error("Audit step must have status initial", errorCode: "wrongWorkflowStatus");

            var applicants = infoService.GetApplicationApplicants(request.ApplicationNr);

            var a = r.Resolve<IMortgageLoanDualAgreementService>();

            var ls = r.Resolve<ILockedAgreementService>();

            MortgageLoanDualAgreementPrintContextModel.SideChannelData sideChannelData = null;
            var printContexts = new List<Tuple<int, MortgageLoanDualAgreementPrintContextModel>>();
            foreach (var applicant in applicants.AllConnectedCustomerIdsWithRoles)
            {
                var customerId = applicant.Key;
                var customerRoles = applicant.Value;
                if (customerRoles.Contains("Applicant") || customerRoles.Contains("ApplicationObject"))
                {
                    printContexts.Add(Tuple.Create(customerId, a.GetPrintContext(ai, customerId, observeData: x => sideChannelData = x)));
                }
            }

            if (sideChannelData == null)
                return Error("Missing context data");

            var dc = r.Resolve<IDocumentClient>();
            var agreementKeyByCustomerId = new Dictionary<int, string>();
            using (var dbContext = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                foreach (var printContext in printContexts)
                {
                    agreementKeyByCustomerId[printContext.Item1] = dc.ArchiveStore(a.CreateAgreementPdf(printContext.Item2).ToArray(), "application/pdf", "Agreement.pdf");
                }
                var totalLoanAmount = sideChannelData.Loans.Sum(x => x.LoanAmount);

                var lockedAgreement = r.Resolve<ILockedAgreementService>().LockAgreementMulti(request.ApplicationNr, agreementKeyByCustomerId, totalLoanAmount, sideChannelData.CreditDecsionId.Value);

                wf.ChangeStepStatusComposable(dbContext, currentStepName, wf.AcceptedStatusName, applicationNr: request.ApplicationNr);

                dbContext.CreateAndAddComment("Agreement audited and locked", currentStepName, applicationNr: request.ApplicationNr);

                dbContext.SaveChanges();

                return new Response
                {
                    LockedAgreement = lockedAgreement
                };
            }
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