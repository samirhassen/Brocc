using nPreCredit.Code;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class FetchListCustomersWithKycStatusMethod : TypedWebserviceMethod<FetchListCustomersWithKycStatusMethod.Request, FetchListCustomersWithKycStatusMethod.Response>
    {
        public override string Path => "CompanyLoan/FetchListCustomersWithKycStatusMethod";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            using (var context = new PreCreditContext())
            {
                var customers = context
                    .CreditApplicationCustomerListMembers
                    .AsNoTracking()
                    .Where(x => request.ListNames.Contains(x.ListName) && x.ApplicationNr == request.ApplicationNr)
                    .GroupBy(x => x.CustomerId)
                    .Select(x => new
                    {
                        CustomerId = x.Key,
                        MemberOfListNames = x.Select(y => y.ListName)
                    })
                    .ToList();

                var customerIds = customers.Select(x => x.CustomerId).ToHashSet();
                var cc = new PreCreditCustomerClient();

                var customerProperties = cc.BulkFetchPropertiesByCustomerIdsSimple(
                    customers.Select(x => x.CustomerId).ToHashSet(),
                    "birthDate", "firstName");

                var onboardingStatuses = cc.FetchCustomerOnboardingStatuses(customerIds, null, null, false);

                var response = new Response { Customers = new List<Customer>() };
                foreach (var customer in customers)
                {
                    response.Customers.Add(new Customer
                    {
                        CustomerId = customer.CustomerId,
                        FirstName = customerProperties?.Opt(customer.CustomerId)?.Opt("firstName"),
                        BirthDate = customerProperties?.Opt(customer.CustomerId)?.Opt("birthDate"),
                        MemberOfListNames = customer.MemberOfListNames.ToList(),
                        IsPep = onboardingStatuses.Opt(customer.CustomerId)?.IsPep,
                        IsSanction = onboardingStatuses.Opt(customer.CustomerId)?.IsSanction,
                        LatestScreeningDate = onboardingStatuses.Opt(customer.CustomerId)?.LatestScreeningDate
                    });
                }

                return response;
            }
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            [Required]
            public List<string> ListNames { get; set; }
        }

        public class Customer
        {
            public string FirstName { get; set; }
            public string BirthDate { get; set; }
            public int CustomerId { get; set; }
            public List<string> MemberOfListNames { get; set; }
            public bool? IsPep { get; set; }
            public bool? IsSanction { get; set; }
            public DateTime? LatestScreeningDate { get; set; }
        }

        public class Response
        {
            public List<Customer> Customers { get; set; }
        }
    }
}