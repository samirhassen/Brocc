using nCreditReport.Code;
using NTech;
using NTech.Banking.OrganisationNumbers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCreditReport.Controllers
{
    [RoutePrefix("CompanyCreditReport")]
    public class CompanyCreditReportController : NController
    {
        [Route("BuyNew")]
        [HttpPost]
        public ActionResult BuyNew(string providerName, string orgnr, int customerId, List<string> returningItemNames, Dictionary<string, string> additionalParameters, string reasonType, string reasonData)
        {
            var errors = new List<string>();
            int userId;
            string username;
            string metadata;
            GetUserProperties(errors, out userId, out username, out metadata);
            if (string.IsNullOrWhiteSpace(orgnr) || customerId <= 0)
                errors.Add("orgnr or customerId");

            if (errors.Count > 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, string.Join(";", errors));

            if (CompanyProviderFactory.Exists(providerName))
            {
                CompanyBaseCreditReportService service;

                service = CompanyProviderFactory.Create(providerName, additionalParameters);

                IOrganisationNumber c;
                var p = new OrganisationNumberParser(service.ForCountry);
                if (!p.TryParse(orgnr, out c))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid orgnr");
                }

                var result = service.TryBuyCreditReport(c, new CreditReportRequestData
                {
                    UserId = userId,
                    Username = username,
                    InformationMetadata = metadata,
                    AdditionalParameters = additionalParameters,
                    ReasonType = reasonType,
                    ReasonData = reasonData
                });

                if (result.IsError)
                {
                    if (result.IsInvalidCredentialsError || result.IsTimeoutError)
                    {
                        return Json2(new { Success = false, IsInvalidCredentialsError = result.IsInvalidCredentialsError, IsTimeoutError = result.IsTimeoutError, ErrorMessage = result.ErrorMessage });
                    }
                    else
                    {
                        return Json2(new { Success = false, ErrorMessage = result.ErrorMessage });
                    }
                }

                var enc = NEnv.EncryptionKeys;
                var repo = new CreditReportRepository(enc.CurrentKeyName, enc.AsDictionary());

                var creditReportId = repo.Save(result.CreditReport, customerId);

                if (returningItemNames != null && returningItemNames.Count > 0)
                {
                    var fetchAll = returningItemNames.Any(x => x == "*");
                    var returnResult = fetchAll ? repo.FetchAll(creditReportId) : repo.Fetch(creditReportId, returningItemNames);

                    return Json2(new
                    {
                        Success = true,
                        CreditReportId = creditReportId,
                        Items = returnResult.Items.Select(x => new
                        {
                            x.Name,
                            x.Value
                        }).ToList()
                    });
                }
                else
                {
                    return Json2(new
                    {
                        Success = true,
                        CreditReportId = creditReportId
                    });
                }
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid providerName");
            }
        }

        [Route("Find")]
        [HttpPost]
        public ActionResult Find(string providerName, int customerId)
        {
            if (customerId <= 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");

            if (CompanyProviderFactory.Exists(providerName))
            {
                var enc = NEnv.EncryptionKeys;
                var repo = new CreditReportRepository(enc.CurrentKeyName, enc.AsDictionary());

                var result = repo.FindForProvider(customerId, providerName);

                return Json2(result.Items.Select(x => new
                {
                    x.RequestDate,
                    x.CreditReportId,
                    AgeInDays = (int)ClockFactory.SharedInstance.Now.Date.Subtract(x.RequestDate.Date).TotalDays
                }));
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid providerName");
            }
        }

        [Route("GetById")]
        [HttpPost]
        public ActionResult GetById(int creditReportId, List<string> itemNames)
        {
            if (creditReportId <= 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid id");
            if (itemNames == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "itemNames missing");

            var enc = NEnv.EncryptionKeys;
            var repo = new CreditReportRepository(enc.CurrentKeyName, enc.AsDictionary());

            var fetchAll = itemNames.Any(x => x == "*");

            var result = fetchAll ? repo.FetchAll(creditReportId) : repo.Fetch(creditReportId, itemNames);

            if (result == null)
                return HttpNotFound();
            else
                return Json2(new
                {
                    result.CreditReportId,
                    result.RequestDate,
                    result.CustomerId,
                    result.ProviderName,
                    Items = result.Items.Select(x => new
                    {
                        x.Name,
                        x.Value
                    }).ToList(),
                    AgeInDays = (int)ClockFactory.SharedInstance.Now.Date.Subtract(result.RequestDate.Date).TotalDays
                });
        }
    }
}