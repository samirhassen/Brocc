﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using nCreditReport.Code;
using nCreditReport.Code.BisnodeFi;
using NTech.Banking.CivicRegNumbers;

namespace nCreditReport.Controllers
{
    [RoutePrefix("PersonInfo")]
    public class PersonInfoController : NController
    {
        private static class AddressProviderFactory
        {
            public const string BisnodeFiProviderName = "bisnodefi";
            public const string TestFiProviderName = "testfi";
            public const string TestSeProviderName = "testse";
            public const string TestOnlySeProviderName = "testonlyse";
            public const string TestOnlyFiProviderName = "testonlyfi";

            public class AddressResult
            {
                public List<Tuple<string, string>> SuccessItems { get; set; }
                public bool IsError { get; set; }
                public bool IsInvalidCredentialsError { get; set; }
                public bool IsTimeoutError { get; set; }
                public string ErrorMessage { get; set; }
            }

            public static Func<ICivicRegNumber, string, AddressResult> GetAddressProvider(string providerName)
            {

                switch (providerName.ToLower())
                {
                    case BisnodeFiProviderName:
                        return (c, username) =>
                        {
                            return BisnodeService.RequestCreditReport(c, username, true, r =>
                            {
                                if (r.IsError)
                                {
                                    return new AddressResult
                                    {
                                        IsError = true,
                                        ErrorMessage = r.ErrorMessage,
                                        IsInvalidCredentialsError = r.IsInvalidCredentialsError
                                    };
                                }

                                return new AddressResult
                                {
                                    SuccessItems = r
                                        .SuccessItems
                                        .Select(x => Tuple.Create(x.Name, x.Value))
                                        .ToList()
                                };
                            });
                        };
                    case TestOnlyFiProviderName:
                    case TestOnlySeProviderName:

                        return (c, _) =>
                        {
                            var cli = new nTestClient();
                            var addressFields = new List<string>
                                { "addressStreet", "addressCity", "addressZipcode", "addressCountry" };
                            var properties = new List<string>
                            {
                                "firstName", "lastName",
                                "creditreport_personStatus", "creditreport_hasDomesticAddress"
                            };
                            properties.AddRange(addressFields);
                            var result = cli.GetTestPerson(null, c, properties.ToArray()) ??
                                         new Dictionary<string, string>();

                            if (result.ContainsKey("creditreport_personStatus"))
                                result["personStatus"] = result["creditreport_personStatus"];

                            if (result.Opt("creditreport_hasDomesticAddress") == "false")
                            {
                                foreach (var k in addressFields)
                                    result.Remove(k);
                            }

                            if (result.Opt("personStatus") != "nodata")
                            {
                                return new AddressResult
                                {
                                    IsError = false,
                                    IsInvalidCredentialsError = false,
                                    ErrorMessage = null,
                                    SuccessItems = result.Select(x => Tuple.Create(x.Key, x.Value)).ToList()
                                };
                            }

                            return new AddressResult
                            {
                                IsError = false,
                                IsInvalidCredentialsError = false,
                                ErrorMessage = null,
                                SuccessItems = new[] { Tuple.Create("personStatus", "nodata") }.ToList()
                            };
                        };
                    default:
                        return null;
                }
            }
        }

        [Route("FetchNameAndAddress")]
        [HttpPost]
        public ActionResult FetchNameAndAddress(string providerName, string civicRegNr, List<string> requestedItemNames,
            int? customerId)
        {
            var errors = new List<string>();

            GetUserProperties(errors, out _, out var username, out _);

            if (string.IsNullOrWhiteSpace(civicRegNr) || !customerId.HasValue)
                errors.Add("Missing civicRegNr or customerId");

            if (errors.Count > 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, string.Join(";", errors));

            var getAddress = AddressProviderFactory.GetAddressProvider(providerName);
            if (getAddress == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid providerName");
            var p = new CivicRegNumberParser(NEnv.ClientCfg.Country.BaseCountry);
            if (!p.TryParse(civicRegNr, out var c))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid civicRegNr");
            }

            var k = NEnv.EncryptionKeys;
            var repo = new AddressLookupCacheRepository(k.CurrentKeyName, k.AsDictionary());
            var cachedItems = repo.GetCachedResult(customerId.Value, providerName, TimeSpan.FromDays(1));
            if (cachedItems != null)
            {
                return Json2(new
                {
                    Success = true,
                    IsFromCache = true,
                    Items = cachedItems
                        .Select(x => new
                        {
                            Name = x.Key,
                            Value = x.Value
                        })
                        .ToList()
                });
            }

            var r = getAddress(c, username);
            if (r.IsError)
            {
                if (r.IsInvalidCredentialsError || r.IsTimeoutError)
                    return Json2(new
                    {
                        Success = false, IsInvalidCredentialsError = r.IsInvalidCredentialsError,
                        IsTimeoutError = false
                    });
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, r.ErrorMessage);
            }

            var resultItems = r
                .SuccessItems
                .Where(x => !x.Item1.Equals("civicRegNr") &&
                            requestedItemNames.Contains(x.Item1, StringComparer.OrdinalIgnoreCase))
                .Select(x => new
                {
                    Name = x.Item1,
                    Value = x.Item2
                })
                .ToList();

            repo.StoreCachedResult(customerId.Value, providerName,
                resultItems.ToDictionary(x => x.Name, x => x.Value), TimeSpan.FromDays(1));

            return Json2(new
            {
                Success = true,
                IsFromCache = false,
                Items = resultItems
            });
        }

        [Route("ClearCache")]
        [HttpPost]
        public ActionResult ClearCache(int? customerId, bool? allCustomers)
        {
            //NOTE: We have allCustomers = true explicitly instead of doing that for missing customerId to avoid deleting everthing by accident when for instance misspelling customerId
            using (var context = new CreditReportContext())
            {
                var r = context.AddressLookupCachedResults.AsQueryable();
                if (customerId.HasValue)
                    r = r.Where(x => x.CustomerId == customerId.Value);
                else if (!allCustomers.GetValueOrDefault())
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest,
                        "customerId or allCustomers must be specified");

                var items = r.ToList();
                foreach (var i in items)
                    context.AddressLookupCachedResults.Remove(i);

                context.SaveChanges();

                return Json2(new { deletedCount = items.Count() });
            }
        }
    }
}