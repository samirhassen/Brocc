using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.CreditStandard;
using nTest.Code;
using nTest.RandomDataSource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace nTest.Controllers
{
    [NTechApi]
    public class ApiUnsecuredLoansStandardController : NController
    {
        [Route("Api/UnsecuredLoansStandard/CreateCustomApplication")]
        [HttpGet]
        public ActionResult CreateCustomApplication(int? nrOfApplicants, bool? kreditzWasApproved, int count = 1)
        {
            var r = new RandomnessSource(null);
            var applicationNrs = new List<string>();
            foreach (var _ in Enumerable.Range(1, count))
            {
                if (TryCreateApplication(nrOfApplicants, kreditzWasApproved, r, out var result))
                {
                    applicationNrs.Add(result.ApplicationNr);
                }
                else
                {
                    return Content($"Error: {result.ErrorMessage}");
                }
            }
            if (applicationNrs.Count == 1)
                return Content($"ApplicationNr: {applicationNrs[0]}");
            else
                return Content($"ApplicationNrs: {string.Join(",", applicationNrs.Take(10))} (of {applicationNrs.Count} total)");
        }

        private class CreateApplicationResult
        {
            public string ErrorMessage { get; set; }
            public string ApplicationNr { get; set; }
            public string ProviderName { get; set; }
            public int RequestedAmount { get; internal set; }
            public int RequestedRepaymentTimeInMonths { get; internal set; }

            public Dictionary<int, StoredPerson> ApplicantsByApplicantNr;
        }

        private bool TryCreateApplication(int? nrOfApplicants, bool? kreditzWasApproved, RandomnessSource r, out CreateApplicationResult result)
        {
            var resultLocal = new CreateApplicationResult
            {
                ApplicantsByApplicantNr = new Dictionary<int, StoredPerson>(),
                ProviderName = "self"
            };

            var clientConfig = NEnv.ClientCfg;

            var repo = new TestPersonRepository(NEnv.ClientCfg.Country.BaseCountry, DbSingleton.SharedInstance.Db);

            resultLocal.RequestedAmount = r.NextIntBetween(30000, 150000);
            resultLocal.RequestedRepaymentTimeInMonths = r.OneOf(12, 24, 48, 60, 72, 96);

            var applicants = Enumerable.Range(1, nrOfApplicants ?? r.NextIntBetween(1, 2)).Select(x =>
            {
                var person = repo.GenerateNewTestPerson(true, r, TimeMachine.SharedInstance.GetCurrentTime().DateTime);
                resultLocal.ApplicantsByApplicantNr[x] = person;

                return new
                {
                    CivicRegNr = person.CivicRegNr,
                    FirstName = person.GetProperty("firstName"),
                    LastName = person.GetProperty("lastName"),
                    AddressStreet = person.GetProperty("addressStreet"),
                    AddressZipcode = person.GetProperty("addressZipcode"),
                    AddressCity = person.GetProperty("addressCity"),
                    Email = person.GetProperty("email"),
                    Phone = person.GetProperty("phone"),
                    ClaimsToBePep = "false",
                    CivilStatus = r.OneOf(CreditStandardCivilStatus.Codes.ToArray()).ToString(),
                    MonthlyIncomeAmount = r.NextIntBetween(10000, 40000),
                    NrOfChildren = r.NextIntBetween(0, 4),
                    //Problem, flag full time in the client config?
                    EmploymentStatus = r.OneOf(CreditStandardEmployment.Codes.ToArray()).ToString(),
                    EmployerName = "Företaget AB",
                    EmployerPhone = "010 111 222 333",
                    EmployedSince = "1994-12-10",
                    HousingType = r.OneOf(CreditStandardHousingType.Codes.ToArray()).ToString(),
                    HousingCostPerMonthAmount = r.NextIntBetween(500, 8000),
                    HasConsentedToCreditReport = true,
                    HasConsentedToShareBankAccountData = true,
                    ClaimsToHaveKfmDebt = false
                };
            }).ToList();

            var request = new
            {
                RequestedAmount = resultLocal.RequestedAmount,
                LoansToSettleAmount = r.NextIntBetween(0, resultLocal.RequestedAmount),
                RequestedRepaymentTimeInMonths = resultLocal.RequestedRepaymentTimeInMonths,
                Applicants = applicants,
                KreditzData = !kreditzWasApproved.HasValue ? null : new
                {
                    CaseId = Guid.NewGuid().ToString(),
                    CaseUrl = $"https://example.org/k/case/{Guid.NewGuid().ToString()}",
                    CreditDecisionCode = kreditzWasApproved.Value ? "approved" : "rejected"
                },
                Meta = new
                {
                    ProviderName = resultLocal.ProviderName,
                    CustomerExternalIpAddress = "127.0.0.1",
                }
            };

            var rawRequest = JsonConvert.SerializeObject(request);
            Console.WriteLine(rawRequest);

            var client = new CreditDriverPreCreditClient();

            if (client.TryCreateUnsecuredLoanStandardApplication(request, out var applicationNr, out var errorMessage))
            {
                resultLocal.ApplicationNr = applicationNr;
                result = resultLocal;
                return true;
            }
            else
            {
                resultLocal.ErrorMessage = errorMessage;
                result = resultLocal;
                return false;
            }
        }
    }
}