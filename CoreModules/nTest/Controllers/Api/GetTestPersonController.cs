using Newtonsoft.Json;
using NTech.Banking.CivicRegNumbers;
using NTech.Services.Infrastructure;
using nTest.Code;
using nTest.Code.Credit;
using nTest.RandomDataSource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nTest.Controllers
{
    [NTechApi]
    [RoutePrefix("Api")]
    public class GetTestPersonController : NController
    {
        [Route("TestPerson/Get")]
        [HttpPost]
        public ActionResult GetTestPerson(string civicRegNr, string civicRegNrCountry, List<string> requestedProperties, string allWithThisPrefix = null, bool? generateIfNotExists = null, bool? addToCustomerModule = null)
        {
            Func<ActionResult> get = () =>
            {
                if (requestedProperties == null)
                    requestedProperties = new List<string>();
                if (civicRegNr == null || civicRegNrCountry == null || (requestedProperties.Count == 0 && allWithThisPrefix == null))
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing civicRegNr or civicRegNrCountry or requestedProperties+allWithThisPrefix");
                var repo = new TestPersonRepository(NEnv.ClientCfg.Country.BaseCountry, DbSingleton.SharedInstance.Db);
                var r = repo.Get(civicRegNrCountry, civicRegNr);
                var returnAll = requestedProperties != null && requestedProperties.Any(x => x == "*");
                if (r == null)
                    return HttpNotFound();
                else
                {
                    return Json2(r
                        .Where(x =>
                                requestedProperties.Contains(x.Key)
                                || (allWithThisPrefix != null && x.Key.StartsWith(allWithThisPrefix))
                                || returnAll)
                        .ToDictionary(x => x.Key, x => x.Value));
                }
            };
            var result = get();

            if (generateIfNotExists.GetValueOrDefault() && typeof(HttpNotFoundResult) == result.GetType())
            {
                GetOrGenerateTestPersonsShared(new List<GetOrGenerateTestPersonModel>
                {
                    new GetOrGenerateTestPersonModel
                    {
                        CivicRegNr = civicRegNr,
                        IsAccepted = true,
                        Customizations = new Dictionary<string, string>()
                    }
                }, null, null, addToCustomerModule);
                result = get();
            }

            return result;
        }

        [Route("TestPerson/GetOrGenerateSingle")]
        public ActionResult GetOrGenerateSingleTestPerson(string civicRegNr, bool? isAccepted, IDictionary<string, string> customizations, int? seed, bool? addToCustomerModule)
        {
            if (customizations != null)
                customizations = customizations.Where(x => !x.Key.IsOneOfIgnoreCase("controller", "action")).ToDictionary(x => x.Key, x => x.Value);

            var result = GetOrGenerateTestPersonsShared(new List<GetOrGenerateTestPersonModel>
            {
                new GetOrGenerateTestPersonModel
                {
                    CivicRegNr = civicRegNr,
                    IsAccepted = isAccepted,
                    Customizations = customizations
                }
            }, seed, null, addToCustomerModule);

            var r = result.Single();

            var customerId = new CustomerClient().GetCustomerId(new CivicRegNumberParser(NEnv.ClientCfg.Country.BaseCountry).Parse(r.CivicRegNr));

            return Json2(new GetOrGenerateSingleTestPersonResponseModel
            {
                CivicRegNr = r.CivicRegNr,
                CustomerId = customerId,
                WasGenerated = r.WasGenerated,
                Properties = r.Properties.ToDictionary(x => x.Key, x => x.Value)
            });
        }

        public class GetOrGenerateSingleTestPersonResponseModel
        {
            public string CivicRegNr { get; set; }
            public IDictionary<string, string> Properties { get; set; }
            public bool WasGenerated { get; set; }
            public int CustomerId { get; set; }
        }

        private List<GetOrGenerateTestPersonResponseModel> GetOrGenerateTestPersonsShared(List<GetOrGenerateTestPersonModel> persons, int? seed, bool? useCommonAddress, bool? addToCustomerModule)
        {
            var now = TimeMachine.SharedInstance.GetCurrentTime().Date;
            var country = NEnv.ClientCfg.Country.BaseCountry;
            Lazy<CivicRegNumberParser> civicRegNumberParser = new Lazy<CivicRegNumberParser>(() => new CivicRegNumberParser(country));

            var random = new RandomnessSource(seed);
            var repo = new TestPersonRepository(NEnv.ClientCfg.Country.BaseCountry, DbSingleton.SharedInstance.Db);
            var result = new List<GetOrGenerateTestPersonResponseModel>();
            var toGenerate = new List<GetOrGenerateTestPersonModel>();
            foreach (var p in persons)
            {
                var ep = repo.Get(country, p.CivicRegNr);
                if (ep != null)
                {
                    result.Add(new GetOrGenerateTestPersonResponseModel { CivicRegNr = p.CivicRegNr, Properties = ep, WasGenerated = false });
                }
                else
                    toGenerate.Add(p);
            }

            Dictionary<string, string> addressOverride = null;
            Action<IDictionary<string, string>> setupAddressOverride = (p) =>
            {
                addressOverride = new Dictionary<string, string>();
                addressOverride["addressStreet"] = p.Opt("addressStreet");
                addressOverride["addressZipcode"] = p.Opt("addressZipcode");
                addressOverride["addressCity"] = p.Opt("addressCity");
            };

            if (useCommonAddress.GetValueOrDefault() && result.Any())
            {
                setupAddressOverride(result[0].Properties);
            }

            foreach (var p in toGenerate)
            {
                var pg = repo.GenerateNewTestPerson(p.IsAccepted ?? true, random, now, overrides: JoinDicts(p.Customizations, addressOverride), civicRegNr: (string.IsNullOrWhiteSpace(p.CivicRegNr) ? null : civicRegNumberParser.Value.Parse(p.CivicRegNr)), reuseExisting: true);
                if (addressOverride == null && useCommonAddress.GetValueOrDefault())
                    setupAddressOverride(pg.Properties);
                result.Add(new GetOrGenerateTestPersonResponseModel
                {
                    CivicRegNr = string.IsNullOrWhiteSpace(p.CivicRegNr) ? pg.CivicRegNr : p.CivicRegNr,
                    Properties = pg.Properties,
                    WasGenerated = true
                });

                if (addToCustomerModule.GetValueOrDefault())
                {
                    var c = new CustomerClient();
                    if (addToCustomerModule.GetValueOrDefault())
                    {
                        var res = c.CreateOrUpdatePerson(new CustomerClient.CreateOrUpdatePersonRequest
                        {
                            CivicRegNr = pg.CivicRegNr,
                            BirthDate = null,
                            ExpectedCustomerId = null,
                            Properties = pg.Properties.Keys.Where(x => !x.IsOneOf("civicRegNr", "civicRegNrCountry", "iban", "bankAccountNr", "bankAccountNrType")).Select(x => new CustomerClient.CreateOrUpdatePersonRequest.Property
                            {
                                Name = x,
                                Value = pg.Properties[x]
                            }).ToList()
                        });
                    }
                }
            }

            return result;
        }

        [Route("TestPerson/GetOrGenerate")]
        [HttpPost]
        public ActionResult GetOrGenerateTestPersons(List<GetOrGenerateTestPersonModel> persons, int? seed, bool? useCommonAddress, bool? addToCustomerModule = null)
        {
            var result = GetOrGenerateTestPersonsShared(persons, seed, useCommonAddress, addToCustomerModule);

            return Json2(new { Persons = result });
        }

        private Dictionary<string, string> JoinDicts(params IDictionary<string, string>[] args)
        {
            var d = new Dictionary<string, string>();
            foreach (var dd in args)
                if (dd != null)
                {
                    foreach (var k in dd.Keys)
                        d[k] = dd[k];
                }
            return d;
        }

        public class GetOrGenerateTestPersonModel
        {
            public string CivicRegNr { get; set; }
            public bool? IsAccepted { get; set; }
            public IDictionary<string, string> Customizations { get; set; }
        }

        public class GetOrGenerateTestPersonResponseModel
        {
            public string CivicRegNr { get; set; }
            public IDictionary<string, string> Properties { get; set; }
            public bool WasGenerated { get; set; }
        }

        [Route("TestPerson/Generate")]
        [HttpPost]
        public ActionResult GenerateTestPersons(bool? isAccepted, int? seed, int? count, List<string> newPersonCustomizations, bool? useCommonAddress)
        {
            var random = new RandomnessSource(seed);
            var repo = new TestPersonRepository(NEnv.ClientCfg.Country.BaseCountry, DbSingleton.SharedInstance.Db);
            var applicants = new List<string>();

            if (newPersonCustomizations != null && newPersonCustomizations.Count != count.GetValueOrDefault())
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "If using newPersonCustomizations it must have the same nr of entries as count");

            var now = TimeMachine.SharedInstance.GetCurrentTime().Date;
            Dictionary<string, string> overridesTemplate = null;
            foreach (var nr in Enumerable.Range(1, count ?? 1).ToList())
            {
                Dictionary<string, string> overrides = new Dictionary<string, string>();
                if (overridesTemplate != null)
                    overridesTemplate.ToList().ForEach(x => overrides[x.Key] = x.Value);

                DateTime? customBirthDate;
                ICivicRegNumber creditReportCivicRegNr;
                if (!TryParsePersonCustomization(newPersonCustomizations != null && newPersonCustomizations.Count > 0 ? newPersonCustomizations[nr - 1] : null, out customBirthDate, out creditReportCivicRegNr))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid customization. Currently supported are BirthDate=yyyy-mm-dd");
                }
                if (creditReportCivicRegNr != null)
                {
                    overrides["creditReportCivicRegNr"] = creditReportCivicRegNr.NormalizedValue;
                    if (NEnv.IsMortgageLoansEnabled && NEnv.ClientCfg.Country.BaseCountry == "SE")
                    {
                        overrides["creditReportRegisteredMunicipality"] = "Stockholm";
                    }
                }

                if (NEnv.IsMortgageLoansEnabled && (isAccepted ?? true) && !customBirthDate.HasValue)
                {
                    //Hack to try to get through almas super strict age based scoring rules
                    var today = TimeMachine.SharedInstance.GetCurrentTime().Date.Date;
                    customBirthDate = today.AddYears(-random.NextIntBetween(57, 66)).AddMonths(-random.NextIntBetween(0, 10)).AddDays(-random.NextIntBetween(0, 15));
                }

                var p = repo.GenerateNewTestPerson(isAccepted ?? true, random, now, birthDate: customBirthDate, overrides: overrides);

                if (useCommonAddress.GetValueOrDefault() && overridesTemplate == null)
                {
                    overridesTemplate = new Dictionary<string, string>();
                    overridesTemplate["addressStreet"] = p.GetProperty("addressStreet");
                    overridesTemplate["addressZipcode"] = p.GetProperty("addressZipcode");
                    overridesTemplate["addressCity"] = p.GetProperty("addressCity");
                }

                applicants.Add(JsonConvert.SerializeObject(p.Properties as IDictionary<string, string>));
            }

            return Json2(new
            {
                applicants = applicants
            });
        }

        private static bool TryParsePersonCustomization(string c, out DateTime? customBirthDate, out ICivicRegNumber creditReportCivicRegNr)
        {
            customBirthDate = null;
            creditReportCivicRegNr = null;

            if (string.IsNullOrWhiteSpace(c))
            {
                return true;
            }

            c = c.Trim().ToLowerInvariant();
            if (c.StartsWith("birthdate="))
            {
                DateTime d;
                if (!DateTime.TryParseExact(c.Substring("birthdate=".Length), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out d))
                    return false;
                customBirthDate = d;
            }
            else if (c.StartsWith("creditreportcivicregnr="))
            {
                creditReportCivicRegNr = new CivicRegNumberParser(NEnv.ClientCfg.Country.BaseCountry).Parse(c.Substring("creditreportcivicregnr=".Length));
            }
            return true;
        }


        [Route("TestPerson/CreateOrUpdate")]
        [HttpPost]
        public ActionResult CreateOrUpdate(List<string> persons, bool? clearCache)
        {
            var repo = new TestPersonRepository(NEnv.ClientCfg.Country.BaseCountry, DbSingleton.SharedInstance.Db);
            foreach (var person in persons ?? new List<string>())
            {
                var p = repo.AddOrUpdate(JsonConvert.DeserializeObject<IDictionary<string, string>>(person), true);

                if (clearCache ?? false)
                {
                    var parser = new CivicRegNumberParser(p.CivicRegNrTwoLetterCountryIsoCode);
                    var civicRegNr = parser.Parse(p.CivicRegNr);
                    var customerId = new CustomerClient().GetCustomerId(civicRegNr);

                    if (NEnv.ServiceRegistry.ContainsService("nCreditReport"))
                    {
                        var creditReportClient = new CreditReportClient();
                        creditReportClient.RemoveCachedCreditReports(customerId);
                        creditReportClient.RemoveCachedPersonInfo(customerId);
                    }
                }
            }

            return Json2(new
            {

            });
        }
    }
}