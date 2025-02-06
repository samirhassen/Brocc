using nPreCredit.Code;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.SharedStandard;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.SharedStandard
{
    public class BuyNewCreditReportLoanMethod : TypedWebserviceMethod<BuyNewCreditReportLoanMethod.Request, BuyNewCreditReportLoanMethod.Response>
    {
        public override string Path => "LoanStandard/CreditReport/BuyNewForApplication";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled || NEnv.IsStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            if (request.CustomerId.HasValue == request.ApplicantNr.HasValue)
            {
                return Error("Exactly one of CustomerId or ApplicantNr must be used");
            }

            int customerId;
            if (request.ApplicantNr.HasValue)
            {
                var applicants = requestContext.Resolver().Resolve<ApplicationInfoService>().GetApplicationApplicants(request.ApplicationNr);
                customerId = applicants.CustomerIdByApplicantNr[request.ApplicantNr.Value];
            }
            else
                customerId = request.CustomerId.Value;

            var service = requestContext.Resolver().Resolve<LoanApplicationCreditReportService>();
            var civicRegNr = new PreCreditCustomerClient().BulkFetchPropertiesByCustomerIdsSimple(new HashSet<int> { customerId }, "civicRegNr")
                .Opt(customerId)?.Opt("civicRegNr");

            if (civicRegNr == null)
                return Error("Missing civicRegNr");

            service.BuyNew(request.ApplicationNr, customerId, NEnv.BaseCivicRegNumberParser.Parse(civicRegNr), null, true);

            return new Response();
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            public int? ApplicantNr { get; set; }

            public int? CustomerId { get; set; }
        }

        public class Response
        {

        }
    }
}