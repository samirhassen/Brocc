using nCustomer.Code.Services;
using nCustomer.DbModel;
using NTech.Banking.CivicRegNumbers;
using NTech.Core.Module.Shared.Clients;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCustomer.Controllers
{
    [NTechAuthorize]
    public partial class CustomerController : NController
    {
        protected override JsonResult Json(object data, string contentType, System.Text.Encoding contentEncoding, JsonRequestBehavior behavior)
        {
            throw new Exception("Dont use this use Json2");
        }

        public ActionResult AddCustomer()
        {
            SetInitialData(new
            {
                translation = GetTranslations()
            });
            return View();
        }

        public ActionResult CustomerCard(int customerId, bool? forceLegacyUi = null, string backTarget = null)
        {
            if (customerId == 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");

            if(forceLegacyUi != true)
            {
                var url = NEnv.ServiceRegistry.Internal.ServiceUrl("nBackoffice", $"s/customer-overview/customer-card/{customerId}", Tuple.Create("backTarget", backTarget));
                return Redirect(url.ToString());
            }

            //NOTE: We have three versions. When the new one in new backoffice works the two old ones should be deleted.
            forceLegacyUi = false;

            bool IsCompanyCustomer()
            {
                Func<CustomersContext, bool> isCompanyCustomer = c => c.CustomerProperties.Count(x => x.CustomerId == customerId && x.Name == "isCompany" && x.IsCurrentData && x.Value == "true") > 0;

                using (var cx = new CustomersContext())
                {
                    return isCompanyCustomer(cx);
                }
            }

            SetInitialData(new
            {
                customerId = customerId,
                isCompanyCustomer = IsCompanyCustomer(),
                useNewUi = !forceLegacyUi.GetValueOrDefault(),
                translation = GetTranslations(),
                isTest = !NEnv.IsProduction
            });
            return View();
        }

        [HttpPost]
        public ActionResult UnlockSensitiveItem(CustomerPropertyModel item)
        {
            using (var db = new CustomersContext())
            {
                var c = CreateSearchRepo(db).GetSensitiveProperty(item.CustomerId, item.Name);
                if (c == null)
                    return HttpNotFound();
                return (Json2(c.Value));
            }
        }

        [HttpPost]
        public ActionResult UnlockSensitiveItemByName(int customerId, string itemName)
        {
            using (var db = new CustomersContext())
            {
                var c = CreateSearchRepo(db).GetSensitiveProperty(customerId, itemName);
                if (c == null)
                    return HttpNotFound();
                return Json2(c.Value);
            }
        }

        [HttpPost]
        public ActionResult GetDecryptedProperties(int customerId, List<string> names)
        {
            using (var db = new CustomersContext())
            {
                var p = CreateSearchRepo(db).GetDecryptedProperties(customerId, onlyTheseNames: names);
                if (p == null)
                    return HttpNotFound();
                return (Json2(p));
            }
        }

        [HttpPost]
        public ActionResult BulkFetchCustomerIdsByCivicRegNrs(List<string> civicRegNrs)
        {
            var cc = new HashSet<ICivicRegNumber>();
            foreach (var c in civicRegNrs)
            {
                cc.Add(NEnv.BaseCivicRegNumberParser.Parse(c));
            }
            return Json2(new
            {
                Items = cc.Select(x =>
                {
                    var result = NTechCache.WithCache($"nCustomer.CustomerIdByCivicRegNr.{x}", TimeSpan.FromHours(12), () => new { cid = CustomerIdSource.GetCustomerIdByCivicRegNr(x) });
                    return new
                    {
                        CustomerId = result.cid,
                        CivicRegNr = x.NormalizedValue
                    };
                })
            });
        }

        [HttpPost]
        public ActionResult GetCustomerIdsByCivicRegNrs(List<string> civicRegNrs)
        {
            if (civicRegNrs == null || civicRegNrs.Count == 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing civicRegNrs");

            var parser = NEnv.BaseCivicRegNumberParser;

            if (civicRegNrs.Any(x => !parser.IsValid(x)))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid civicRegNrs");

            var customerIdsByCivicRegNrs = civicRegNrs
                .Distinct()
                .ToDictionary(x => x, x => CustomerIdSource.GetCustomerIdByCivicRegNr(parser.Parse(x)));

            return Json2(new { CustomerIdsByCivicRegNrs = customerIdsByCivicRegNrs });
        }

        [HttpPost]
        public ActionResult UpdateCustomerGettingCustomerId(string civicRegNr, List<CustomerPropertyModel> items, bool? force)
        {
            if (string.IsNullOrEmpty(civicRegNr))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing civicRegNr");
            }

            if (items?.Any(x => x.Name.ToLowerInvariant().Contains(CustomerProperty.Codes.orgnr.ToString())) ?? false)
                throw new Exception("Companies cannot be created or updated using this api");

            ICivicRegNumber c;
            if (!NEnv.BaseCivicRegNumberParser.TryParse(civicRegNr, out c))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid civicRegNr");
            }
            var customerId = CustomerIdSource.GetCustomerIdByCivicRegNr(c);
            foreach (var i in items)
                i.CustomerId = customerId;

            if (!items.Any(x => x.Name == CustomerProperty.Codes.birthDate.ToString()))
            {
                items.Add(new CustomerPropertyModel
                {
                    IsSensitive = false,
                    CustomerId = customerId,
                    Name = CustomerProperty.Codes.birthDate.ToString(),
                    Group = CustomerProperty.Groups.insensitive.ToString(),
                    Value = c.BirthDate.Value.ToString("yyyy-MM-dd")
                });
            }
            if ((force ?? false) && CustomerWriteRepository.HasAllAddressHashFields(items))
            {
                //Precompute this for performance reasons to enable bulk inserting new customers
                items.Add(new CustomerPropertyModel
                {
                    Name = CustomerProperty.Codes.addressHash.ToString(),
                    Group = CustomerProperty.Groups.insensitive.ToString(),
                    CustomerId = customerId,
                    Value = CustomerWriteRepository.ComputeAddressHash(items)
                });
            }
            UpdateCustomer(items, force ?? false);
            return Json2(new { customerId });
        }

        [HttpPost]
        public ActionResult RepairSearchTerms()
        {
            CustomersContext.RunWithExclusiveLock<object>(
                $"ntech.ncustomer.repairsearchterms",
                () =>
                {
                    var repository = new CustomerSearchTermRepository(() => new CustomersContext(), GetCurrentUserMetadata(), Clock);
                    repository.RepairSearchTerms();
                    return null;
                },
                () =>
                {
                    throw new Exception("Attempt to run several instances of RepairSearchTerms at the same time!");
                }, waitForLock: TimeSpan.FromMilliseconds(3000));

            return new EmptyResult();
        }

        private bool TryUpdateCustomerI(List<CustomerPropertyModel> items, bool force, out string failedMessage)
        {
            if (items == null || items.Count == 0 || items.Any(x => x.CustomerId == 0))
            {
                failedMessage = "Missing items or customerId";
                return false;
            }

            var customerIds = items.Select(x => x.CustomerId).Distinct().ToList();

            string lockName;
            if (customerIds.Count == 1)
                lockName = $"ntech.ncustomer.updatecustomer.{customerIds.Single()}";
            else
                lockName = $"ntech.ncustomer.updatecustomer.bulk";

            if (items?.Any(x => x.Name.ToLowerInvariant().Contains(CustomerProperty.Codes.orgnr.ToString())) ?? false)
                throw new Exception("Companies cannot be created using this api and orgnr cannot be updated");

            CustomersContext.RunWithExclusiveLock<object>(
                lockName,
                () =>
                {
                    using (var db = new CustomersContext())
                    {
                        using (var tr = db.Database.BeginTransaction())
                        {
                            var repository = CreateWriteRepo(db);
                            repository.UpdateProperties(items, force);
                            db.SaveChanges();
                            tr.Commit();
                        }
                    }
                    return null;
                },
                () =>
                {
                    throw new Exception("Concurrent modification attempt on customers: " + string.Join(",", customerIds.Select(x => x.ToString())));
                }, waitForLock: TimeSpan.FromMilliseconds(3000));

            failedMessage = null;
            return true;
        }

        [HttpPost]
        public ActionResult UpdateCustomer(List<CustomerPropertyModel> items, bool force)
        {
            string m;
            if (TryUpdateCustomerI(items, force, out m))
            {
                return new EmptyResult();
            }
            else
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing items or customerId");
        }

        [HttpPost]
        public ActionResult GetCustomerIdsWithSameData(string name, string value)
        {
            return Json2(GetCustomerIdsWithSameDataI(name, value));
        }

        private List<int> GetCustomerIdsWithSameDataI(string name, string value)
        {
            return GetBulkCustomerIdsWithSameDataI(new List<Tuple<string, string>> { Tuple.Create(name, value) })[name];
        }

        private Dictionary<string, List<int>> GetBulkCustomerIdsWithSameDataI(List<Tuple<string, string>> items)
        {
            using (var db = new CustomersContext())
            {
                var result = new Dictionary<string, List<int>>(items.Count);
                foreach (var i in items)
                {
                    var n = i.Item1;
                    var v = i.Item2;
                    result[n] = db
                        .CustomerProperties
                        .Where(x =>
                            x.IsCurrentData &&
                            x.Name == n &&
                            x.Value == v
                         )
                        .Select(x => x.CustomerId)
                        .ToList()
                        .Distinct()
                        .ToList();
                }

                return result;
            }
        }

        [HttpPost]
        public ActionResult FindCustomerIdsMatchingAllSearchTerms(List<CustomerSearchTermModel> terms)
        {
            using (var db = new CustomersContext())
            {
                var repo = CreateSearchRepo(db);
                return Json2(new { customerIds = repo.FindCustomersMatchingAllSearchTerms(terms.Select(x => Tuple.Create(x.TermCode, x.TermValue)).ToArray()) });
            }
        }

        [HttpPost]
        public ActionResult FindCustomerIdsMatchingName(string name)
        {
            using (var db = new CustomersContext())
            {
                var repo = CreateSearchRepo(db);
                return Json2(new { customerIds = repo.FindCustomersByName(name) });
            }
        }

        [HttpPost]
        public ActionResult FindCustomerIdsExactName(string name)
        {
            using (var db = new CustomersContext())
            {
                var repo = CreateSearchRepo(db);
                var searchTerm = CustomerSearchService.GetExactMatchSearchTermOrNull(name);
                List<int> customerIds;
                if (searchTerm == null)
                    customerIds = new List<int>();
                else
                    customerIds = repo.FindCustomersByExactFirstName(name);

                return Json2(new { customerIds });
            }
        }

        [HttpPost]
        public ActionResult FindCustomerIdsOmni(string searchQuery)
        {
            var searchService = new CustomerSearchService(CreateSearchRepo, () => Service.CompanyLoanNameSearch);
            var customerIds = searchService.FindCustomersByOmniQuery(searchQuery);
            return Json2(new { customerIds });
        }

        [HttpPost]
        public ActionResult GetCustomerIdsWithSameAddress(int customerId, bool? treatMissingAddressAsNoHits)
        {
            using (var db = new CustomersContext())
            {
                var p = db
                    .CustomerProperties
                    .Where(x => x.CustomerId == customerId && x.IsCurrentData && x.Name == CustomerProperty.Codes.addressHash.ToString())
                    .SingleOrDefault();
                if (p == null)
                {
                    if (treatMissingAddressAsNoHits.HasValue && treatMissingAddressAsNoHits.Value)
                        return Json2(new List<int>());
                    else
                    {
                        NLog.Warning("No address hash exists for customer {customerId}", customerId);
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No address hash exists");
                    }
                }

                string addressHash = p.Value;
                List<int> customerProperties = db
                    .CustomerProperties
                    .Where(x =>
                        x.IsCurrentData &&
                        x.Name == CustomerProperty.Codes.addressHash.ToString() &&
                        x.Value == addressHash &&
                        x.CustomerId != customerId
                     )
                    .Select(x => x.CustomerId)
                .ToList();

                return (Json2(customerProperties));
            }
        }

        private CustomerPropertyStatusService CreatePropertyService()
        {
            return new CustomerPropertyStatusService(this.Service.CustomerContextFactory);
        }

        [HttpPost()]
        public ActionResult CheckPropertyStatus(int customerId, List<string> propertyNames)
        {
            string m;
            var service = CreatePropertyService();
            CustomerPropertyStatusService.CheckPropertyStatusResult r;
            if (service.TryCheckPropertyStatus(customerId, propertyNames, out m, out r))
            {
                return Json2(new
                {
                    missingPropertyNames = r?.MissingPropertyNames
                });
            }
            else
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, m);
        }

        [HttpPost()]
        public ActionResult BulkFetchPropertiesByCustomerIds(string[] propertyNames, int[] customerIds)
        {
            if (customerIds == null)
                customerIds = new int[] { };

            using (var db = new CustomersContext())
            {
                var repo = CreateSearchRepo(db);
                var uniqueCustomerIds = new HashSet<int>(customerIds);
                var result = repo.BulkFetch(uniqueCustomerIds, new HashSet<string>(propertyNames));

                foreach (var missingCustomerId in uniqueCustomerIds.Except(result.Select(x => x.Key)))
                {
                    //Just to make parsing the result less error prone. Better that they get an empty list than KeyNotFoundException or similar if the customer has none of these properties
                    result[missingCustomerId] = new List<CustomerPropertyModel>();
                }

                return Json2(new
                {
                    customers = result.Select(x => new
                    {
                        CustomerId = x.Key,
                        Properties = x.Value.Select(y => new
                        {
                            y.Name,
                            y.Group,
                            y.IsSensitive,
                            y.Value
                        })
                    })
                });
            }
        }
    }
}