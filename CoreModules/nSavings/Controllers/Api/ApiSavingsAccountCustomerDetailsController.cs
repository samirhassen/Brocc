using nSavings.Code;
using NTech.Services.Infrastructure;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechApi]
    public class ApiSavingsAccountCustomerDetailsController : NController
    {
        [HttpPost]
        [Route("Api/SavingsAccount/Customers")]
        public ActionResult CustomerDetails(string savingsAccountNr)
        {
            if (string.IsNullOrWhiteSpace(savingsAccountNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing savingsAccountNr");
            using (var context = new SavingsContext())
            {
                var date = Clock.Today;

                var mainCustomerId = context
                    .SavingsAccountHeaders
                    .Where(x => x.SavingsAccountNr == savingsAccountNr)
                    .Select(x => (int?)x.MainCustomerId)
                    .SingleOrDefault();

                if (!mainCustomerId.HasValue)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such savings account");

                var customerClient = new CustomerClient();

                var customer = customerClient.BulkFetchPropertiesByCustomerIds(new HashSet<int> { mainCustomerId.Value }, "firstName", "email", "phone", "birthDate").Single();

                var targetToHere = NTechNavigationTarget.CreateCrossModuleNavigationTarget("SavingsAccountOverviewSpecificTab", new Dictionary<string, string> { { "savingsAccountNr", savingsAccountNr }, { "tab", "Customer" } });

                var c = new ExpandoObject() as IDictionary<string, object>;
                c["customerId"] = customer.Key;
                c["isMainCustomer"] = true;
                c["customerCardUrl"] = CustomerClient.GetCustomerCardUri(customer.Value.CustomerId, false, targetToHere).ToString();
                c["customerFatcaCrsUrl"] = CustomerClient.GetCustomerFatcaCrsUri(customer.Value.CustomerId, targetToHere).ToString();
                c["customerPepKycUrl"] = CustomerClient.GetCustomerPepKycUrl(customer.Value.CustomerId, targetToHere).ToString();
                c["customerKycQuestionsUrl"] = CustomerClient.GetCustomerKycQuestionsUrl(customer.Value.CustomerId, targetToHere).ToString(); 
                foreach (var customerCard in customer.Value.Properties)
                {
                    c[customerCard.Name] = customerCard.Value;
                }

                var result = new
                {
                    savingsAccountNr = savingsAccountNr,
                    customers = new[]
                    {
                        c
                    }
                };

                return Json2(result);
            }
        }

        [HttpPost]
        [Route("Api/SavingsAccount/FetchCustomerItems")]
        public ActionResult FetchCustomerItems(int customerId, IList<string> propertyNames)
        {
            var c = new CustomerClient();
            var result = c.BulkFetchPropertiesByCustomerIds(new HashSet<int> { customerId }, propertyNames?.ToArray());
            var items = result.Where(x => x.Key == customerId).SelectMany(x => x.Value.Properties.Select(y => new { name = y.Name, value = y.Value })).ToList();
            return Json2(new { customerId = customerId, items = items });
        }
    }
}