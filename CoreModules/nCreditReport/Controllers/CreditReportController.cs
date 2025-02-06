using nCreditReport.Code;
using nCreditReport.Controllers.Api;
using nCreditReport.Models;
using NTech;
using NTech.Banking.CivicRegNumbers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCreditReport.Controllers
{
    [RoutePrefix("CreditReport")]
    public class CreditReportController : NController
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="providerName">Provider name</param>
        /// <param name="civicRegNr">civic regnr</param>
        /// <param name="customerId">customer id</param>
        /// <param name="returningItemNames">list of item names to return</param>
        /// <param name="additionalParameters">provider specific custom data</param>
        /// <param name="reasonType">Type of reason the report was purchased. For instance CreditApplication.</param>
        /// <param name="reasonData">Type specific data. For instance for reasonType CreditApplication this will be application nr.</param>
        /// <returns></returns>
        [Route("BuyNew")]
        [HttpPost]
        public ActionResult BuyNew(string providerName, string civicRegNr, int customerId, List<string> returningItemNames, Dictionary<string, string> additionalParameters, string reasonType, string reasonData)
        {
            var errors = new List<string>();
            GetUserProperties(errors, out var userId, out var username, out var metadata);
            if (string.IsNullOrWhiteSpace(civicRegNr) || customerId <= 0)
                errors.Add("Missing civicRegNr or customerId");

            if (errors.Count > 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, string.Join(";", errors));

            if (PersonProviderFactory.Exists(providerName))
            {
                PersonBaseCreditReportService service;

                service = PersonProviderFactory.Create(providerName);

                var p = new CivicRegNumberParser(service.ForCountry);
                if (!p.TryParse(civicRegNr, out var civicRegNumber))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid civicRegNr");
                }

                var result = service.TryBuyCreditReport(civicRegNumber, new CreditReportRequestData
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
                        return Json2(new { Success = false, IsInvalidCredentialsError = result.IsInvalidCredentialsError, IsTimeoutError = result.IsTimeoutError });
                    }
                    else
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, result.ErrorMessage);
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

            if (PersonProviderFactory.Exists(providerName))
            {
                var enc = NEnv.EncryptionKeys;
                var repo = new CreditReportRepository(enc.CurrentKeyName, enc.AsDictionary());

                var result = repo.FindForProvider(customerId, providerName);

                var providerService = PersonProviderFactory.Create(providerName);
                var canFetchTabledValues = providerService.CanFetchTabledValues();

                return Json2(result.Items.Select(x => new
                {
                    x.RequestDate,
                    x.CreditReportId,
                    AgeInDays = (int)ClockFactory.SharedInstance.Now.Date.Subtract(x.RequestDate.Date).TotalDays,
                    CanFetchTabledValues = canFetchTabledValues
                }));
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid providerName");
            }
        }

        [Route("FindForProviders")]
        [HttpPost]
        public ActionResult FindForProviders(string[] providers, int customerId)
        {
            if (customerId <= 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");

            if (providers == null || providers.Length == 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing providers in request. ");

            var enc = NEnv.EncryptionKeys;
            var repo = new CreditReportRepository(enc.CurrentKeyName, enc.AsDictionary());

            var result = repo.FindForProviders(providers, customerId);

            return Json2(result.Items.Select(x => new
            {
                x.RequestDate,
                x.CreditReportId,
                AgeInDays = (int)ClockFactory.SharedInstance.Now.Date.Subtract(x.RequestDate.Date).TotalDays,
                CanFetchTabledValues = PersonProviderFactory.Create(x.ProviderName).CanFetchTabledValues()
            }));
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

        [Route("FetchTabledValues")]
        [HttpPost]
        public ActionResult FetchTabledValues(int creditReportId)
        {
            var enc = NEnv.EncryptionKeys;
            var repo = new CreditReportRepository(enc.CurrentKeyName, enc.AsDictionary());

            var creditReport = repo.FetchAll(creditReportId);

            var isInjectedTestReport = creditReport.Items.Any(x => x.Name == "isInjectedTestReport" && x.Value == "true");
            if(isInjectedTestReport)
            {
                var list = new[] { new { title = "-Injected test report", value = "" }, new { title = "--Items", value = "" } }.Concat(creditReport.Items.Select(x => new
                {
                    title = x.Name,
                    value = x.Value
                }));

                return Json2(list);
            }
            else
            {
                var providerService = PersonProviderFactory.Create(creditReport.ProviderName);
                var result = providerService.FetchTabledValues(creditReport);

                var list = result.Select(x => new { title = (string)x.Key, value = (string)x.Value });

                return Json2(list);
            }
        }

        [Route("FindForCustomer")]
        [HttpPost]
        public ActionResult FindForCustomer(int? customerId, bool? isCompany, int? batchSize, int? skipCount)
        {
            if (!customerId.HasValue || customerId.Value <= 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");

            if (!isCompany.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing isCompany");

            using (var context = new CreditReportContext())
            {
                var query = context.CreditApplicationHeaders.Where(x => x.CustomerId == customerId).OrderByDescending(x => x.Id).AsQueryable();
                if (skipCount.HasValue)
                    query = query.Skip(skipCount.Value);

                int remainingReportsCount = 0;

                if (batchSize.HasValue)
                {
                    query = query.Take(batchSize.Value);
                    remainingReportsCount = query.Skip(batchSize.Value).Count();
                }

                var result = ApiFindCreditReportsByReasonController.FindFromQuery(context, query, isCompany.GetValueOrDefault());

                return Json2(new { CreditReportsBatch = result, RemainingReportsCount = remainingReportsCount });
            }
        }

        [Route("FetchReason")]
        [HttpPost]
        public ActionResult FetchReason(int? creditReportId)
        {
            if (!creditReportId.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing creditReportId");

            using (var context = new CreditReportContext())
            {
                var result = context
                    .CreditApplicationHeaders
                    .Where(x => x.Id == creditReportId.Value)
                    .Select(x => new
                    {
                        ReasonType = x.SearchTerms.Where(y => y.Name == "reasonType").Select(y => y.Value).FirstOrDefault(),
                        ReasonData = x.SearchTerms.Where(y => y.Name == "reasonData").Select(y => y.Value).FirstOrDefault(),
                    })
                    .FirstOrDefault();

                return Json2(new
                {
                    ReasonType = result?.ReasonType,
                    ReasonData = result?.ReasonData
                });
            }
        }

        [Route("FetchProviderMetadataBulk")]
        [HttpPost]
        public ActionResult FetchProviderMetadataBulk(List<string> providerNames)
        {
            var nonExistingProviderNames = new List<string>();
            var result = (providerNames ?? new List<string>()).Select(x =>
            {
                BaseCreditReportService provider;
                if (PersonProviderFactory.Exists(x))
                    provider = PersonProviderFactory.Create(x);
                else if (CompanyProviderFactory.Exists(x))
                    provider = CompanyProviderFactory.Create(x, null);
                else
                {
                    provider = null;
                    nonExistingProviderNames.Add(x);
                }
                if (provider == null)
                    return null;

                return new
                {
                    ProviderName = x,
                    IsCompanyProvider = provider.IsCompanyProvider,
                    IsActive = true //TODO: Allow this to be changed so we can have list only providers
                };
            }).Where(x => x != null).ToDictionary(x => x.ProviderName);

            return Json2(new
            {
                NonExistingProviderNames = nonExistingProviderNames,
                ProvidersByName = result
            });
        }

        [Route("ClearCache")]
        [HttpPost]
        public ActionResult ClearCache(int? customerId, bool? allCustomers)
        {
            //NOTE: We have allCustomers = true explicitly instead of doing that for missing customerId to avoid deleting everthing by accident when for instance misspelling customerId
            using (var context = new CreditReportContext())
            {
                var r = context.CreditApplicationHeaders.AsQueryable();
                if (customerId.HasValue)
                    r = r.Where(x => x.CustomerId == customerId.Value);
                else if (!allCustomers.GetValueOrDefault())
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "customerId or allCustomers must be specified");

                var items = r.ToList();
                foreach (var i in items)
                    context.CreditApplicationHeaders.Remove(i);

                context.SaveChanges();

                return Json2(new { deletedCount = items.Count() });
            }
        }

        public class InjectTestReportRequest
        {
            [Required]
            public string ProviderName { get; set; }
            [Required]
            public string ReasonData { get; set; }
            [Required]
            public string ReasonType { get; set; }
            [Required]
            public string CivicRegNr { get; set; }
            [Required]
            public int CustomerId { get; set; }
            [Required]
            public Dictionary<string, string> CreditReportItems { get; set; }
        }

        private class InjectTestReportService : PersonBaseCreditReportService
        {
            private ICivicRegNumber civicRegNr;

            private InjectTestReportService(InjectTestReportRequest request) : base(request.ProviderName)
            {
                this.civicRegNr = new CivicRegNumberParser(NEnv.ClientCfg.Country.BaseCountry).Parse(request.CivicRegNr);
            }

            public override string ForCountry => civicRegNr.Country;

            public static int InjectReport(InjectTestReportRequest request, CreditReportRepository repo, string informationMetadata, int userId, string username)
            {
                var s = new InjectTestReportService(request);
                var saveRequest = s.CreateResult(s.civicRegNr, request.CreditReportItems.Select(x => new SaveCreditReportRequest.Item
                {
                    Name = x.Key,
                    Value = x.Value
                }).Concat(Enumerables.Singleton(new SaveCreditReportRequest.Item { Name = "isInjectedTestReport", Value = "true" })) , new CreditReportRequestData
                {
                    InformationMetadata = informationMetadata,
                    UserId = userId,
                    Username = username,
                    ReasonData = request.ReasonData,
                    ReasonType = request.ReasonType
                });
                saveRequest.SearchTerms.Add(new SaveCreditReportRequest.Item
                {
                    Name = "isInjectedTestReport",
                    Value = "true"
                });
                return repo.Save(saveRequest, request.CustomerId);
            }

            protected override Result DoTryBuyCreditReport(ICivicRegNumber civicRegNr, CreditReportRequestData requestData) => throw new NotImplementedException();
        }

        [Route("InjectPersonTestReport")]
        [HttpPost]
        public ActionResult InjectPersonTestReport(InjectTestReportRequest request)
        {
            if (NEnv.IsProduction)
                return HttpNotFound();

            GetUserProperties(new List<string>(), out var userId, out var username, out var metadata);

            var enc = NEnv.EncryptionKeys;
            var repo = new CreditReportRepository(enc.CurrentKeyName, enc.AsDictionary());

            var reportId = InjectTestReportService.InjectReport(request, repo, metadata, userId, username);

            return Json2(new
            {
                Id = reportId
            });
        }
    }
}