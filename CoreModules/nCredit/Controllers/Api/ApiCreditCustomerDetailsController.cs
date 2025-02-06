using Microsoft.Ajax.Utilities;
using nCredit.Code;
using NTech.Core.Module.Shared.Clients;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IdentityModel;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class ApiCreditCustomerDetailsController : NController
    {
        private class CustomerTmp
        {
            public int? ApplicantNr { get; set; }
            public string ListName { get; set; }
            public int CustomerId { get; set; }
        }

        [HttpPost]
        [Route("Api/Credit/Customers-Simple")]
        public ActionResult CreditCustomersSimple(string creditNr)
        {
            if (string.IsNullOrWhiteSpace(creditNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing creditNr");
            using (var context = new CreditContext())
            {
                var date = Clock.Today;

                var result = context
                    .CreditHeaders
                    .Where(x => x.CreditNr == creditNr)
                    .Select(x => new
                    {
                        x.CreditType,
                        ListCustomers = x.CustomerListMembers.Select(y => new
                        {
                            y.ListName,
                            y.CustomerId
                        }),
                        CreditCustomers = x.CreditCustomers.Select(y => new
                        {
                            y.CustomerId,
                            y.ApplicantNr
                        })
                    })
                    .SingleOrDefault();
                return Json2(result);
            }
        }

        [HttpPost]
        [Route("Api/Credit/Customers")]
        public ActionResult CreditCustomerDetails(string creditNr, string backTarget)
        {
            if (string.IsNullOrWhiteSpace(creditNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing creditNr");
            using (var context = new CreditContext())
            {
                var date = Clock.Today;

                var customersPre = context
                    .CreditHeaders
                    .Where(x => x.CreditNr == creditNr)
                    .Select(x => new
                    {
                        x.CreditType,
                        ListCustomers = x.CustomerListMembers.Select(y => new
                        {
                            y.ListName,
                            y.CustomerId
                        }),
                        CreditCustomers = x.CreditCustomers.Select(y => new
                        {
                            y.CustomerId,
                            y.ApplicantNr
                        })
                    })
                    .ToList();

                ICustomerClient customerClient = new CreditCustomerClient();

                var customers = customersPre.SelectMany(x => x.CreditCustomers.Select(y => new CustomerTmp
                {
                    ApplicantNr = y.ApplicantNr,
                    CustomerId = y.CustomerId
                })).Union(customersPre.SelectMany(x => x.ListCustomers.Select(y => new CustomerTmp
                {
                    CustomerId = y.CustomerId,
                    ListName = y.ListName
                })))
                    .ToList();

                var customerCardResult = customerClient.BulkFetchPropertiesByCustomerIdsD(new HashSet<int>(customers.Select(x => x.CustomerId)), "firstName", "email", "phone", "birthDate", "companyName", "isCompany");

                Action<CustomerTmp, List<IDictionary<string, object>>> addCustomer = (customer, customerModels) =>
                    {
                        var c = new ExpandoObject() as IDictionary<string, object>;
                        c["applicantNr"] = customer.ApplicantNr;
                        c["customerId"] = customer.CustomerId;
                        c["customerCardUrl"] = CreditCustomerClient.GetCustomerCardUri(customer.CustomerId, backTarget).ToString();
                        c["pepKycCustomerUrl"] = NEnv.ServiceRegistry.Internal.ServiceUrl("nCustomer",
                            "Ui/KycManagement/Manage", CreditCustomerClient.GetCustomerCardArgsTupleArr(customer.CustomerId, backTarget));
                        c["customerFatcaCrsUrl"] = NEnv.ServiceRegistry.Internal.ServiceUrl("nCustomer", "Ui/KycManagement/FatcaCrs",
                            CreditCustomerClient.GetCustomerCardArgsTupleArr(customer.CustomerId, backTarget));
                        c["isDirectDebitPaymentsEnabled"] = NEnv.IsDirectDebitPaymentsEnabled;

                        foreach (var customerCard in customerCardResult[customer.CustomerId])
                        {
                            c[customerCard.Key] = customerCard.Value;
                        }

                        customerModels.Add(c);
                    };

                var applicantModels = new List<IDictionary<string, object>>();
                foreach (var customer in customers.Where(x => x.ApplicantNr.HasValue).OrderBy(x => x.ApplicantNr.Value))
                {
                    addCustomer(customer, applicantModels);
                }

                var lists = new Dictionary<string, List<IDictionary<string, object>>>();
                foreach (var listCustomer in customers.Where(x => !x.ApplicantNr.HasValue).OrderBy(x => x.CustomerId))
                {
                    if (!lists.ContainsKey(listCustomer.ListName))
                        lists[listCustomer.ListName] = new List<IDictionary<string, object>>();
                    addCustomer(listCustomer, lists[listCustomer.ListName]);
                }

                var result = new
                {
                    creditNr = creditNr,
                    customers = applicantModels,
                    listCustomers = lists
                };

                return Json2(result);
            }
        }

        [HttpPost]
        [Route("Api/Credit/FetchCustomerItems")]
        public ActionResult FetchCustomerItems(int customerId, IList<string> propertyNames)
        {
            var c = new CreditCustomerClient();
            var result = c.BulkFetchPropertiesByCustomerIdsD(new HashSet<int> { customerId }, propertyNames?.ToArray());
            var items = result.Where(x => x.Key == customerId).SelectMany(x => x.Value.Select(y => new { name = y.Key, value = y.Value })).ToList();
            return Json2(new { customerId = customerId, items = items });
        }

        [HttpPost]
        [Route("Api/Credit/RemoveCompanyConnection")]
        public ActionResult RemoveCompanyConnection(int customerId, string creditNr, string listName)
        {
            if (customerId == 0 || creditNr.IsNullOrWhiteSpace() || listName.IsNullOrWhiteSpace())
            {
                throw new BadRequestException("customerId must be set, creditNr must have a value and listName must have a value. ");
            }

            var listService = Service.CreditCustomerListService;
            var now = Clock.Now;

            using (var context = new CreditContextExtended(GetCurrentUserMetadata(), Clock))
            {
                var businessEvent = new BusinessEvent
                {
                    EventDate = now,
                    EventType = BusinessEventType.RemovedCompanyConnection.ToString(),
                    BookKeepingDate = now.Date,
                    TransactionDate = now.ToLocalTime().Date,
                    ChangedById = CurrentUserId,
                    ChangedDate = now,
                    InformationMetaData = InformationMetadata,
                };
                context.BusinessEvents.Add(businessEvent);

                listService.SetMemberStatusComposable(context, listName, false, customerId, creditNr, null, businessEvent);

                context.SaveChanges();

                return Json2(new { });
            }
        }

        [HttpPost]
        [Route("Api/Credit/AddCompanyConnections")]
        public ActionResult AddCompanyConnections(int customerId, string creditNr, List<string> listNames)
        {
            if (customerId == 0 || creditNr.IsNullOrWhiteSpace() || listNames is null || !listNames.Any())
            {
                throw new BadRequestException("customerId must be set, creditNr must have a value and at least one listName must be set. ");
            }

            var listService = Service.CreditCustomerListService;
            var now = Clock.Now;

            using (var context = new CreditContextExtended(GetCurrentUserMetadata(), Clock))
            {
                var businessEvent = new BusinessEvent
                {
                    EventDate = now,
                    EventType = BusinessEventType.AddedCompanyConnection.ToString(),
                    BookKeepingDate = now.Date,
                    TransactionDate = now.ToLocalTime().Date,
                    ChangedById = CurrentUserId,
                    ChangedDate = now,
                    InformationMetaData = InformationMetadata,
                };
                context.BusinessEvents.Add(businessEvent);
                foreach (var listName in listNames)
                {
                    listService.SetMemberStatusComposable(context, listName, true, customerId, creditNr, null, businessEvent);
                }

                context.SaveChanges();

                return Json2(new { });
            }
        }

    }
}