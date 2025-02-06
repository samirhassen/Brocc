using NTech.Banking.BankAccounts;
using NTech.Legacy.Module.Shared;
using NTech.Services.Infrastructure.Email;
using nTest.Code;
using nTest.RandomDataSource;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

namespace nTest.Controllers
{
    [RoutePrefix("Ui")]
    public class MainController : NController
    {
        [Route("Main")]
        public ActionResult Index()
        {
            Func<string, string> getLoginPage = (country) =>
            {
                if (country == "FI")
                    return "login/eid-signature";
                else if (country == "SE")
                    return "login/eid-signature";
                else
                    throw new NotImplementedException();
            };
            var loginPage = getLoginPage(NEnv.ClientCfg.Country.BaseCountry);
            var resetService = new EnvironmentResetService(NTechEnvironmentLegacy.SharedInstance);
            var environmentRestoreJobs = resetService.GetEnvironmentRestoreJobs();

            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                customApplicationUrl = Url.Action("CustomApplication", new { }),
                newBackofficeTestBaseUrl = NEnv.ServiceRegistry.External.ServiceUrl("nBackoffice", $"/s/test/").ToString(),
                customUnsecuredLoansStandardApplicationUrl = NEnv.ServiceRegistry.External.ServiceUrl("nBackoffice", $"/s/test/unsecured-standard/createapplication").ToString(),
                customMortgageLoansStandardApplicationUrl = NEnv.ServiceRegistry.External.ServiceUrl("nBackoffice", $"/s/test/mortgage-standard/createapplication").ToString(),
                apiDocumentationUrl = Url.Action("Index", "ApiDocumentation", new { }),
                createPaymentFileUrl = Url.Action("CreatePaymentFile", new { }),
                urlLoginToCustomerPages = !NEnv.ServiceRegistry.ContainsService("nCustomerPages") ? null : NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages", $"{loginPage}?targetName=Overview").ToString(),
                urlLoginToCustomerPagesApplications = !NEnv.ServiceRegistry.ContainsService("nCustomerPages") ? null : NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages", $"{loginPage}?targetName=ApplicationsOverview").ToString(),
                urlLoginToCustomerPagesOverview = !NEnv.ServiceRegistry.ContainsService("nCustomerPages") ? null : NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages", $"{loginPage}?targetName=StandardOverview").ToString(),
                urlLoginToCustomerPagesMortgageLoanApplication = !NEnv.ServiceRegistry.ContainsService("nCustomerPages") ? null : NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages", $"{loginPage}?targetName=MortgageLoanApplication").ToString(),
                urlApplyForSavingsAccountInCustomerPages = !NEnv.ServiceRegistry.ContainsService("nCustomerPages") ? null : NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages", $"{loginPage}?targetName=SavingsStandardApplication").ToString(),
                urlToBackoffice = (new Uri(NEnv.ServiceRegistry.External["nBackoffice"])).ToString(),
                urlToGccCustomerApplication = NEnv.IsUnsecuredLoansEnabled && NEnv.ServiceRegistry.ContainsService("nGccCustomerApplication") ? NEnv.ServiceRegistry.External.ServiceRootUri("nGccCustomerApplication").ToString() : null,
                urlToCustomerPagesMortageLoanCalculator = NEnv.IsStandardMortgageLoansEnabled ? NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages", "n/mortgage-loan-applications/open/calculator").ToString() : null,
                currentTime = TimeMachine.SharedInstance.GetCurrentTime(),
                urlToHere = Url.Action("Index"),
                isCompanyLoansEnabled = NEnv.IsCompanyLoansEnabled,
                isMortgageLoansEnabled = NEnv.IsMortgageLoansEnabled,
                isUnsecuredLoansEnabled = NEnv.IsUnsecuredLoansEnabled,
                environmentRestoreJobs,
                hasEmailProvider = NTechEmailServiceFactory.HasEmailProvider,
                isEmailProviderDown = NTechEmailServiceFactory.OfflineSimulatingTestEmailService.IsSimulatedDownNow(),
                clientCountry = NEnv.ClientCfg.Country.BaseCountry,
                //TODO: Longterm this should be the calculator
                urlToUlStandardWebApplication = !NEnv.ServiceRegistry.ContainsService("nCustomerPages") 
                    ? null 
                    : NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages", "n/ul-webapplications/application-calculator").ToString(),
            });
            ViewBag.IsPerLoanDueDatesEnabled = NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.perloandueday");
            return View();
        }

        [Route("EditTestEntity")]
        public ActionResult EditTestEntity()
        {
            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                currentTime = TimeMachine.SharedInstance.GetCurrentTime(),
                baseCountry = NEnv.ClientCfg.Country.BaseCountry
            });
            return View();
        }

        [Route("CreatePaymentFile")]
        public ActionResult CreatePaymentFile()
        {
            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                currentTime = TimeMachine.SharedInstance.GetCurrentTime(),
                baseCountry = NEnv.ClientCfg.Country.BaseCountry
            });
            return View();
        }

        [Route("PhoneNrs")]
        [HttpGet()]
        public ActionResult PhoneNrs()
        {
            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                currentTime = TimeMachine.SharedInstance.GetCurrentTime(),
            });
            return View();
        }

        [Route("BuyCreditReport")]
        [HttpGet()]
        public ActionResult BuyCreditReport()
        {
            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                currentTime = TimeMachine.SharedInstance.GetCurrentTime(),
                baseCountry = NEnv.ClientCfg.Country.BaseCountry,
                isMortgageLoansEnabled = NEnv.IsMortgageLoansEnabled,
                isUnsecuredLoansEnabled = NEnv.IsUnsecuredLoansEnabled
            });
            return View();
        }

        [Route("GenerateTestData")]
        [HttpGet()]
        public ActionResult GenerateTestData(int? seed, string birthDate = null)
        {
            seed = seed ?? Environment.TickCount;
            var random = new RandomnessSource(seed);
            var now = TimeMachine.SharedInstance.GetCurrentTime().Date;
            DateTime? customBirthDate = null;
            if (!string.IsNullOrWhiteSpace(birthDate))
                customBirthDate = DateTime.ParseExact(birthDate, "yyyyMMdd", CultureInfo.InvariantCulture);

            var baseCountry = NEnv.ClientCfg.Country.BaseCountry;
            var repo = new TestPersonRepository(baseCountry, DbSingleton.SharedInstance.Db, baseCountry == "FI");
            var p = repo.GenerateNewTestPerson(true, random, now, reuseExisting: true, birthDate: customBirthDate);

            var gs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "civicregnr", "Person" },
                { "iban", "Person" },
                { "bankaccountnr", "Person" },
                { "firstname", "Contact" },
                { "lastname", "Contact" },
                { "email", "Contact" },
                { "phone", "Contact" },
                { "addressStreet", "Address" },
                { "addressZipcode", "Address" },
                { "addressCity", "Address" },
                { "addressCountry", "Address" }
            };

            var groupOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "Person", 1 },
                { "Contact", 3 },
                { "Address", 4 },
                { "Other", 5 },
            };

            var properties = new Dictionary<string, string>();
            p.Properties.ToList().ForEach(x => properties[x.Key] = x.Value);

            if (NEnv.IsCompanyLoansEnabled)
            {
                var companyRepo = new TestCompanyRepository(NEnv.ClientCfg.Country.BaseCountry, DbSingleton.SharedInstance.Db, ApiCompanyLoanController.CreateStoreHtmlDocumentInArchiveFunc());
                var c = companyRepo.GenerateNewTestCompany(true, random, now, reuseExisting: true,
                    bankAccountNrType: (NEnv.ClientCfg.Country.BaseCountry == "SE" ? (BankAccountNumberTypeCode?)BankAccountNumberTypeCode.BankGiroSe : null));

                groupOrder["Company"] = 2;

                gs["orgnr"] = "Company";
                properties["orgnr"] = c.Properties["orgnr"];

                gs["companyName"] = "Company";
                properties["companyName"] = c.Properties["companyName"];

                if (NEnv.ClientCfg.Country.BaseCountry == "SE")
                {
                    gs["bankGiroNr"] = "Company";
                    properties["bankGiroNr"] = c.Properties["bankAccountNr"];
                }
            }

            var items = properties.Select(x => new
            {
                Group = gs.ContainsKey(x.Key) ? gs[x.Key] : "Other",
                Name = x.Key,
                Value = x.Value
            })
            .GroupBy(x => x.Group)
            .OrderBy(x => groupOrder[x.Key])
            .SelectMany(x =>
                new[] { new { Group = x.Key, Name = (string)null, Value = (string)null } }
                .Concat(x.Select(y => new { Group = (string)null, Name = y.Name, Value = y.Value })))
            .ToList();

            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                items = items,
                seed = seed,
                currentTime = TimeMachine.SharedInstance.GetCurrentTime(),
            });
            return View();
        }

        [Route("CustomApplication")]
        public ActionResult CustomApplication()
        {
            var applicationUrlPattern = new Uri(new Uri(NEnv.ServiceRegistry.External["nPreCredit"]), "CreditManagement/CreditApplication?applicationNr=NNNNN");
            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                currentTime = TimeMachine.SharedInstance.GetCurrentTime(),
                applicationUrlPrefix = applicationUrlPattern.ToString().Replace("NNNNN", ""),
                defaultProviderName = NEnv.DefaultProviderName,
                providerNames = NEnv.GetProviderNames(false)
            });
            return View();
        }

        [Route("CustomSavingsAccountApplication")]
        public ActionResult CustomSavingsAccountApplication()
        {
            var savingsAccountUrlPattern = new Uri(new Uri(NEnv.ServiceRegistry.External["nSavings"]), "Ui/SavingsAccount/#!/Details/NNNNN");
            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                currentTime = TimeMachine.SharedInstance.GetCurrentTime(),
                savingsAccountUrlPrefix = savingsAccountUrlPattern.ToString().Replace("NNNNN", ""),
            });
            return View();
        }

        [Route("PaymentPlanCalculation")]
        public ActionResult PaymentPlanCalculation()
        {
            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                urlCreditAnnuityCalculation = !NEnv.ServiceRegistry.ContainsService("nCredit") ? null : NEnv.ServiceRegistry.External.ServiceUrl("nCredit", "Api/Credit/AnnuityCalculationDetailsExcel").ToString(),
                urlCreditPaymentPlanCalculation = !NEnv.ServiceRegistry.ContainsService("nCredit") ? null : NEnv.ServiceRegistry.External.ServiceUrl("nCredit", "Api/Credit/ClientPaymentPlanDetailsExcel").ToString(),
                currentTime = TimeMachine.SharedInstance.GetCurrentTime()
            });
            return View();
        }

        [Route("EditCreditCollateral")]
        public ActionResult EditCreditCollateral()
        {
            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
                currentTime = TimeMachine.SharedInstance.GetCurrentTime()
            });
            return View();
        }
    }
}