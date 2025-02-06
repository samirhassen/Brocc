using Newtonsoft.Json;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class FetchUnsecuredLoanApplicationsForCustomerPagesMethod : TypedWebserviceMethod<FetchUnsecuredLoanApplicationsForCustomerPagesMethod.Request, FetchUnsecuredLoanApplicationsForCustomerPagesMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/CustomerPages/Fetch-Applications";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            using (var context = new PreCreditContext())
            {
                var customerId = request.CustomerId.Value;

                var applications = context
                    .CreditApplicationHeaders
                    .Where(x => x.CustomerListMemberships.Any(y => y.ListName == "Applicant" && y.CustomerId == customerId) && !x.ArchivedDate.HasValue)
                    .OrderBy(x => x.ApplicationDate)
                    .ThenBy(x => x.ApplicationNr)
                    .Select(x => new Response.ApplicationModel
                    {
                        ApplicationNr = x.ApplicationNr,
                        IsActive = x.IsActive,
                        ApplicationDate = x.ApplicationDate,
                        IsCancelled = x.IsCancelled,
                        IsRejected = x.IsRejected,
                        IsFinalDecisionMade = x.IsFinalDecisionMade,
                        CreditNr = x
                            .ComplexApplicationListItems
                            .Where(y => y.ListName == "Application" && y.ItemName == "creditNr" && !y.IsRepeatable && y.Nr == 1 && x.IsFinalDecisionMade)
                            .Select(y => y.ItemValue)
                            .FirstOrDefault()
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
            public List<ApplicationModel> Applications { get; set; }
            public class ApplicationModel
            {
                public string ApplicationNr { get; set; }
                public bool IsActive { get; set; }
                public DateTimeOffset ApplicationDate { get; set; }
                public bool IsCancelled { get; set; }
                public bool IsRejected { get; set; }
                public bool IsFinalDecisionMade { get; set; }
                public string CreditNr { get; set; }
            }
        }
    }
}