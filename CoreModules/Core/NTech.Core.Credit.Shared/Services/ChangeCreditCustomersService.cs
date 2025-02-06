using nCredit;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services
{
    public class ChangeCreditCustomersService
    {
        private readonly CreditContextFactory creditContextFactory;

        public ChangeCreditCustomersService(CreditContextFactory creditContextFactory)
        {
            this.creditContextFactory = creditContextFactory;
        }

        public RemoveCreditCustomerResponse RemoveCreditCustomer(RemoveCreditCustomerRequest request)
        {
            using(var context = creditContextFactory.CreateContext())
            {
                int customerId;
                if (request.ApplicantNr.HasValue)
                {
                    var customer = context.CreditCustomersQueryable.SingleOrDefault(x => x.CreditNr == request.CreditNr && x.ApplicantNr == request.ApplicantNr.Value);
                    if (customer == null || (request.CustomerId.HasValue && request.CustomerId.Value != customer.CustomerId))
                        throw new NTechCoreWebserviceException("No such customer exists") { IsUserFacing = true, ErrorHttpStatusCode = 400 };
                    customerId = customer.CustomerId;
                }
                else if (request.CustomerId.HasValue)
                {
                    var customer = context.CreditCustomersQueryable.SingleOrDefault(x => x.CreditNr == request.CreditNr && x.CustomerId == request.CustomerId.Value);
                    if (customer == null)
                        throw new NTechCoreWebserviceException("No such customer exists") { IsUserFacing = true, ErrorHttpStatusCode = 400 };
                    customerId = customer.CustomerId;
                }
                else
                    throw new NTechCoreWebserviceException("At least one of ApplicantNr and CreditNr must be set") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                var evt = BusinessEventManagerOrServiceBase.AddBusinessEventShared(BusinessEventType.RemoveCreditCustomer, context);

                context.AddBusinessEvent(evt);

                RemoveCreditCustomer(request.CreditNr, customerId, context, evt: evt);

                context.SaveChanges();

                return new RemoveCreditCustomerResponse { };
            }
        }

        public void RemoveCreditCustomer(string creditNr, int customerId, ICreditContextExtended context, BusinessEvent evt = null)
        {
            var credit = context
                .CreditHeadersQueryable
                .Where(x => x.CreditNr == creditNr)
                .Select(x => new
                {
                    Header = x,
                    x.CreditCustomers,
                    x.Documents,
                    x.CustomerListMembers
                })
                .Single();

            if(credit.CreditCustomers.Count == 1)
            {
                throw new NTechCoreWebserviceException("Cannot remove the only customer") {  IsUserFacing = true, ErrorHttpStatusCode = 400 };
            }
            var customerIds = credit.CreditCustomers.Select(x => x.CustomerId).ToList();

            var customerToRemove = credit.CreditCustomers.Single(x => x.CustomerId == customerId);
            CreditCustomer newFirstApplicantCustomer = null;
            if(customerToRemove.ApplicantNr == 1)
            {
                //The should always be an ApplicantNr = 1 since that is often used as a proxy for the loan i various contexts
                newFirstApplicantCustomer = credit.CreditCustomers.Where(x => x.CustomerId != customerId).OrderBy(x => x.ApplicantNr).First();
            }

            context.RemoveCreditCustomers(customerToRemove);

            var customerIdsFilterable = customerIds.Select(x => (int?)x).ToList();
            var documents = context.CreditDocumentsQueryable.Where(x => x.CustomerId != null && customerIdsFilterable.Contains(x.CustomerId)).ToList();
            foreach(var document in documents)
            {
                if(document.CustomerId == customerToRemove.CustomerId)
                {
                    document.ApplicantNr = null; //Leave this for history using the removed customers list but not as belonging to an applicant
                }
                else if(newFirstApplicantCustomer != null && document.CustomerId == newFirstApplicantCustomer.CustomerId && document.ApplicantNr == newFirstApplicantCustomer.ApplicantNr)
                {
                    document.ApplicantNr = 1;
                }
            }

            string commentText = $"Removed customer {customerToRemove.ApplicantNr}";
            if(newFirstApplicantCustomer != null)
            {
                commentText += $" and changed customer {newFirstApplicantCustomer.ApplicantNr} to now be customer 1";
                newFirstApplicantCustomer.ApplicantNr = 1;
            }

            BusinessEventManagerOrServiceBase.AddCommentShared(commentText, evt?.EventType ?? "RemovedCreditCustomer", context, creditNr: creditNr, evt: evt);

            var listService = new CreditCustomerListServiceComposable();
            
            foreach (var listName in credit.CustomerListMembers.Where(x => x.CustomerId == customerId).Select(x => x.ListName))
            {
                listService.SetMemberStatusComposable(context, listName, false, customerId, creditNr: creditNr);
            }

            listService.SetMemberStatusComposable(context, "removedCustomer", true, customerId, creditNr: creditNr, evt: evt);

            var creditHeader = credit.Header;
            creditHeader.NrOfApplicants = creditHeader.NrOfApplicants - 1;
        }
    }

    public class RemoveCreditCustomerRequest
    {
        public int? ApplicantNr { get; set; }
        public int? CustomerId { get; set; }

        [Required]
        public string CreditNr { get; set; }
    }

    public class RemoveCreditCustomerResponse
    {

    }
}
