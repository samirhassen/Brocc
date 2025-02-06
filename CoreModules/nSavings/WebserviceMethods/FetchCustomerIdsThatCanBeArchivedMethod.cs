using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nSavings.WebserviceMethods
{
    /// <summary>
    /// Intended usage is that the customer module will basically allow all other installed modules to vote
    /// on if a customer can be archived or not.
    /// 
    /// Only the ones that all modules think can be archived will be archived.
    /// </summary>
    public class FetchCustomerIdsThatCanBeArchivedMethod : TypedWebserviceMethod<FetchCustomerIdsThatCanBeArchivedMethod.Request, FetchCustomerIdsThatCanBeArchivedMethod.Response>
    {
        public override string Path => "Savings/Fetch-CustomerIds-That-Can-Be-Archived";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            using (var context = new SavingsContext())
            {
                var candidateCustomerIds = request.CandidateCustomerIds.ToHashSet();

                //We veto any customer that we have ever seen. This is likely to be relaxed as the archiving feature is built out.
                var accountMainCustomerIds = context.SavingsAccountHeaders.Where(x => candidateCustomerIds.Contains(x.MainCustomerId)).Select(x => x.MainCustomerId).ToHashSet();
                candidateCustomerIds.ExceptWith(accountMainCustomerIds);

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
            public List<int> CandidateCustomerIds { get; set; }
        }
    }
}