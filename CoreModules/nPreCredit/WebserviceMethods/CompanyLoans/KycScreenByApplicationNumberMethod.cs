using nPreCredit.Code;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class KycScreenByApplicationNumberMethod : TypedWebserviceMethod<KycScreenByApplicationNumberMethod.Request, KycScreenByApplicationNumberMethod.Response>
    {
        public override string Path => "CompanyLoan/KycScreenByApplicationNr";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            using (var context = new PreCreditContext())
            {
                var customerClient = new PreCreditCustomerClient();
                var customerIds = context
                    .CreditApplicationCustomerListMembers
                    .AsNoTracking()
                    .Where(x => (x.ListName == "companyLoanBeneficialOwner" || x.ListName == "companyLoanAuthorizedSignatory") && x.ApplicationNr == request.ApplicationNr)
                    .Select(x => x.CustomerId)
                    .Distinct()
                    .ToHashSet();

                var isKycScreeningSuccess = customerClient.KycScreen(customerIds, request.ScreenDate, true);

                if (isKycScreeningSuccess)
                    return new Response { Success = true };
            }
            return new Response { Success = false };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            [Required]
            public DateTime ScreenDate { get; set; }
        }


        public class Response
        {
            public bool Success { get; set; }
        }
    }
}