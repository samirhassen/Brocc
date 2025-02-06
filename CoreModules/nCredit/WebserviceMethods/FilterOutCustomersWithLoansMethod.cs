using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCredit.WebserviceMethods
{
    /// <summary>
    /// Returns the customerIds sent in, if the customer does not have a loan. 
    /// </summary>
    public class FilterOutCustomersWithLoansMethod : TypedWebserviceMethod<FilterOutCustomersWithLoansMethod.Request, FilterOutCustomersWithLoansMethod.Response>
    {
        public override string Path => "Credit/Filter-Out-Customers-With-Loans";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);


            using (var context = new CreditContext())
            {
                var candidateCustomerIds = request.CustomerIds.ToHashSet();

                //We veto any customer with  loans 

                var creditCustomerIds = context
                    .CreditCustomers
                    .Where(x => candidateCustomerIds.Contains(x.CustomerId))
                    .Select(x => x.CustomerId).ToHashSet();

                candidateCustomerIds.ExceptWith(creditCustomerIds);

                var listCustomerIds = context
                    .CreditCustomerListMembers
                    .Where(x => candidateCustomerIds.Contains(x.CustomerId))
                    .Select(x => x.CustomerId)
                    .ToHashSet();

                candidateCustomerIds.ExceptWith(listCustomerIds);

                return new Response
                {
                    ArchivableCustomerIds = candidateCustomerIds.ToList()
                };
            }
        }


        public class Response
        {
            public List<int> ArchivableCustomerIds { get; set; }
        }

        public class Request
        {
            [Required]
            public List<int> CustomerIds { get; set; }
        }
    }
}
