using Newtonsoft.Json;
using NTech.Banking.BankAccounts;
using NTech.Banking.Conversion;
using NTech.Banking.OrganisationNumbers;
using NTech.Services.Infrastructure;
using nTest.Code;
using nTest.Code.Credit;
using nTest.RandomDataSource;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nTest.Controllers
{
    [NTechApi]
    [RoutePrefix("Api/Company")]
    public class ApiCompanyLoanController : NController
    {
        [Route("TestCompany/Get")]
        [HttpPost]
        public ActionResult GetTestCompany(string orgnr, string orgnrCountry, bool? generateIfNotExists)
        {
            orgnrCountry = NEnv.ClientCfg.Country.BaseCountry;
            if (orgnr == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing orgnr");
            var repo = new TestCompanyRepository(orgnrCountry, DbSingleton.SharedInstance.Db, CreateStoreHtmlDocumentInArchiveFunc());
            var r = repo.Get(orgnrCountry, orgnr);

            if (r == null)
            {
                if (generateIfNotExists ?? false)
                {
                    var result = GetOrGenerateTestCompanyI(orgnr, orgnrCountry, true, null, null, null);
                    return Json2(result.Properties);
                }
                else
                    return HttpNotFound();
            }
            else
                return Json2(r.ToDictionary(x => x.Key, x => x.Value));
        }

        public static Func<Stream, string> CreateStoreHtmlDocumentInArchiveFunc()
        {
            return s =>
            {
                var dc = new DocumentClient();
                using (var ms = new MemoryStream())
                {
                    s.CopyTo(ms);
                    var data = ms.ToArray();
                    return dc.ArchiveStore(data, "text/html", "file.html");
                }
            };
        }

        [Route("TestCompany/GetOrGenerateBulk")]
        [HttpPost]
        public ActionResult GetOrGenerateTestCompany(int count, bool? isAccepted, int? seed, bool? addToCustomerModule)
        {
            var result = Enumerable.Range(1, count).Select(_ => GetOrGenerateTestCompanyI(null, null, isAccepted ?? true, seed, addToCustomerModule ?? false, null)).ToList();
            return Json2(new { Companies = result });
        }

        [Route("TestCompany/GetOrGenerate")]
        [HttpPost]
        public ActionResult GetOrGenerateTestCompany(string orgnr, string orgnrCountry, bool? isAccepted, int? seed, bool? addToCustomerModule, string bankAccountType)
        {
            var result = GetOrGenerateTestCompanyI(orgnr, orgnrCountry, isAccepted, seed, addToCustomerModule, bankAccountType);
            return Json2(result);
        }

        private GetOrGenerateTestCompanyResponseModel GetOrGenerateTestCompanyI(string orgnr, string orgnrCountry, bool? isAccepted, int? seed, bool? addToCustomerModule, string bankAccountType)
        {
            var now = TimeMachine.SharedInstance.GetCurrentTime().Date;
            var country = orgnrCountry ?? NEnv.ClientCfg.Country.BaseCountry;
            var random = new RandomnessSource(seed);
            IOrganisationNumber orgnrParsed;
            if (orgnr == null)
                orgnrParsed = new OrganisationNumberGenerator(country).Generate((x, y) => random.NextIntBetween(x, y - 1));
            else
                orgnrParsed = new OrganisationNumberParser(country).Parse(orgnr);

            var repo = new TestCompanyRepository(NEnv.ClientCfg.Country.BaseCountry, DbSingleton.SharedInstance.Db, CreateStoreHtmlDocumentInArchiveFunc());

            GetOrGenerateTestCompanyResponseModel result;
            var ep = repo.Get(country, orgnrParsed.NormalizedValue);
            if (ep != null)
            {
                result = new GetOrGenerateTestCompanyResponseModel { Orgnr = orgnrParsed.NormalizedValue, Properties = ep, WasGenerated = false };
            }
            else
            {
                Dictionary<string, string> addressOverride = null;
                Action<IDictionary<string, string>> setupAddressOverride = (p) =>
                {
                    addressOverride = new Dictionary<string, string>();
                    addressOverride["addressStreet"] = p.Opt("addressStreet");
                    addressOverride["addressZipcode"] = p.Opt("addressZipcode");
                    addressOverride["addressCity"] = p.Opt("addressCity");
                };

                BankAccountNumberTypeCode bankAccountNrTypeCode;

                if (!string.IsNullOrWhiteSpace(bankAccountType))
                    bankAccountNrTypeCode = Enums.Parse<BankAccountNumberTypeCode>(bankAccountType).Value;
                else
                    bankAccountNrTypeCode = BankAccountNumberParser.GetDefaultAccountTypeByCountryCode(country);

                var pg = repo.GenerateNewTestCompany(isAccepted ?? true, random, now, overrides: addressOverride, orgnr: orgnrParsed, reuseExisting: true, bankAccountNrType: bankAccountNrTypeCode);

                result = new GetOrGenerateTestCompanyResponseModel
                {
                    Orgnr = orgnrParsed.NormalizedValue,
                    Properties = pg.Properties,
                    WasGenerated = true
                };
            }

            var c = new CustomerClient();
            var customerId = c.GetCustomerId(orgnrParsed);
            result.CustomerId = customerId;

            if (NEnv.ServiceRegistry.ContainsService("nCreditReport"))
            {
                var cc = new CreditReportClient();
                cc.RemoveCachedCreditReports(customerId);
            }

            if (addToCustomerModule.GetValueOrDefault())
            {
                var res = c.CreateOrUpdateCompany(new CustomerClient.CreateOrUpdateCompanyRequest
                {
                    Orgnr = result.Orgnr,
                    CompanyName = result.Properties["companyName"],
                    ExpectedCustomerId = customerId,
                    Properties = result.Properties.Where(x => !x.Key.IsOneOf("orgnr", "orgnrCountry", "iban", "bankAccountNr", "bankAccountNrType")).Select(x => new CustomerClient.CreateOrUpdateCompanyRequest.Property
                    {
                        Name = x.Key,
                        Value = x.Value
                    }).ToList()
                });
            }
            return result;
        }

        public class GetOrGenerateTestCompanyResponseModel
        {
            public string Orgnr { get; set; }
            public IDictionary<string, string> Properties { get; set; }
            public bool WasGenerated { get; set; }
            public int CustomerId { get; set; }
        }

        [Route("TestCompany/CreateOrUpdate")]
        [HttpPost]
        public ActionResult CreateOrUpdate(List<string> companies, bool? clearCache)
        {
            var repo = new TestCompanyRepository(NEnv.ClientCfg.Country.BaseCountry, DbSingleton.SharedInstance.Db, CreateStoreHtmlDocumentInArchiveFunc());
            foreach (var company in companies ?? new List<string>())
            {
                var p = repo.AddOrUpdate(JsonConvert.DeserializeObject<IDictionary<string, string>>(company), true);

                if (clearCache ?? false)
                {
                    var parser = new OrganisationNumberParser(p.OrgnrNrTwoLetterCountryIsoCode);
                    var orgnr = parser.Parse(p.Orgnr);
                    var customerId = new CustomerClient().GetCustomerId(orgnr);

                    if (NEnv.ServiceRegistry.ContainsService("nCreditReport"))
                    {
                        var cc = new CreditReportClient();
                        cc.RemoveCachedCreditReports(customerId);
                        cc.RemoveCachedPersonInfo(customerId);
                    }
                }
            }

            return Json2(new
            {

            });
        }
    }
}