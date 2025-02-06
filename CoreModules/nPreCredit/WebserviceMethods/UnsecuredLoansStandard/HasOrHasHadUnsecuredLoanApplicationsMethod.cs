using Newtonsoft.Json;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class HasOrHasHadUnsecuredLoanApplicationsMethod : TypedWebserviceMethod<HasOrHasHadUnsecuredLoanApplicationsMethod.Request, HasOrHasHadUnsecuredLoanApplicationsMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/Has-Or-Has-Had-Applications";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            using (var context = new PreCreditContext())
            {
                var customerId = request.CustomerId.Value;

                var counts = context
                    .CreditApplicationHeaders
                    .Where(x => x.CustomerListMemberships.Any(y => y.ListName == "Applicant" && y.CustomerId == customerId) && !x.ArchivedDate.HasValue)
                    .Select(x => new
                    {
                        x.ApplicationNr,
                        IsActive = x.IsActive
                    })
                    .GroupBy(x => x.IsActive)
                    .ToDictionary(x => x.Key, x => x.Count());

                Func<bool, int> getCount = isActive => counts.ContainsKey(isActive) ? counts[isActive] : 0;

                return new Response
                {
                    HasActiveApplications = getCount(true) > 0,
                    HasAnyApplications = (getCount(true) + getCount(false)) > 0
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
            public bool HasActiveApplications { get; set; }
            public bool HasAnyApplications { get; set; }
        }
    }
}