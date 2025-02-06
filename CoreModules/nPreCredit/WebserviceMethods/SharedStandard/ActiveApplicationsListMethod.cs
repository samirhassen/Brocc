using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class ActiveApplicationsListMethod : TypedWebserviceMethod<ActiveApplicationsListMethod.Request, ActiveApplicationsListMethod.Response>
    {
        public override string Path => "LoanStandard/Applications/List-Active";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled || NEnv.IsStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            using (var context = new PreCreditContext())
            {
                var applications = context
                    .CreditApplicationHeaders
                    .Where(x => x.CustomerListMemberships.Any(y => y.ListName == "Applicant" && y.CustomerId == request.CustomerId) && x.IsActive)
                    .Select(x => new Response.Application
                    {
                        ApplicationDate = x.ApplicationDate,
                        ApplicationNr = x.ApplicationNr
                    })
                    .ToList();

                return new Response
                {
                    Applications = applications
                };
            }
        }

        public class Request
        {
            [Required]
            public int? CustomerId { get; set; }
        }

        public class Response
        {
            public List<Application> Applications { get; set; }

            public class Application
            {
                public string ApplicationNr { get; set; }
                public DateTimeOffset ApplicationDate { get; set; }
            }
        }
    }
}