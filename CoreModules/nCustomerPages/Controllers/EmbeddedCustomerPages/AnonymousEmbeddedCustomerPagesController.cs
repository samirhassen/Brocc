using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;
using Newtonsoft.Json.Serialization;
using NTech;
using NTech.Banking.CivicRegNumbers;
using NTech.Core.Module.Shared;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;

namespace nCustomerPages.Controllers;

public class AnonymousEmbeddedCustomerPagesController : EmbeddedCustomerPagesControllerBase
{
    private static bool IsApiWhiteListedForProxying(string moduleName, string localPath)
    {
        /*
         * When whitelisting things here think about the fact that the end user can manipulate everything except the customer id.
         * Make sure the apis exposed here are safe to be called (in the sense of only affecting that user) under these premises.
         */
        if (moduleName.EqualsIgnoreCase("NTechHost"))
        {
            return localPath.IsOneOfIgnoreCase(
                "Api/Customer/KycQuestionSession/LoadCustomerPagesSession",
                "Api/Customer/KycQuestionSession/HandleAnswers",
                "Api/PreCredit/PolicyFilters/PreScore-WebApplication");
        }

        if (!NEnv.IsProduction && moduleName.EqualsIgnoreCase("nTest"))
        {
            return localPath.IsOneOfIgnoreCase(
                "Api/TestPerson/GetOrGenerate",
                "Api/Company/TestCompany/GetOrGenerateBulk");
        }

        return false;
    }

    [NTechApi]
    [HttpPost]
    [Route("api/embedded-customerpages/iso-countries")]
    [AllowAnonymous]
    public ActionResult IsoCountries()
    {
        var result = new JsonNetActionResult
        {
            Data = IsoCountry.LoadEmbedded(),
            SerializerSettings =
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }
        };
        return result;
    }

    [NTechApi]
    [HttpPost]
    [Route("api/embedded-customerpages/ul-web-application-settings")]
    [AllowAnonymous]
    public ActionResult LoanObjectives()
    {
        var clientCfg = NEnv.ClientCfgCore;
        if (!NEnv.IsStandardUnsecuredLoansEnabled ||
            !clientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans.webapplication"))
            return HttpNotFound();

        var applicationSettings = LegacyServiceClientFactory
            .CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry)
            .LoadSettings("unsecuredLoanExternalApplication");

        object result;
        try
        {
            var exampleCalculatorValues = GetExampleCalculatorValues();

            result = new
            {
                IsEnabled = true,
                Settings = new
                {
                    LoanObjectives = clientCfg.GetRepeatedCustomValue("LoanObjectives", "LoanObjective"),
                    RepaymentTimes = exampleCalculatorValues.RepaymentTimes,
                    LoanAmounts = exampleCalculatorValues.LoanAmounts,
                    ExampleMarginInterestRatePercent =
                        Numbers.ParseDecimalOrNull(applicationSettings.Req("exampleInterestRatePercent")),
                    ExampleInitialFeeWithheldAmount = clientCfg.GetSingleCustomInt(false,
                        "UlStandardWebApplication", "ExampleInitialFeeWithheldAmount"),
                    ExampleInitialFeeCapitalizedAmount = clientCfg.GetSingleCustomInt(false,
                        "UlStandardWebApplication", "ExampleInitialFeeCapitalizedAmount"),
                    ExampleInitialFeeOnFirstNotificationAmount =
                        int.Parse(applicationSettings.Req("exampleInitialFeeOnFirstNotificationAmount")),
                    ExampleNotificationFee = int.Parse(applicationSettings.Req("exampleNotificationFee")),
                    PersonalDataPolicyUrl = new Uri(applicationSettings.Req("personalDataPolicyUrl")).ToString(),
                    DataSharing = GetDataSharingSettings(clientCfg, NTechEnvironmentLegacy.SharedInstance)
                }
            };
        }
        catch (NTechCoreWebserviceException ex)
        {
            if (ex.ErrorCode == "applicationDisabled")
            {
                result = new
                {
                    IsEnabled = false
                };
            }
            else
                throw;
        }

        var actionResult = new JsonNetActionResult
        {
            Data = result,
            SerializerSettings =
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }
        };
        return actionResult;

        (List<string> RepaymentTimes, List<int> LoanAmounts) GetExampleCalculatorValues()
        {
            var exampleLoanAmounts = GetSteppedItems(GetInt("exampleLoanAmountMin"), GetInt("exampleLoanAmountMax"),
                GetInt("exampleLoanAmountStep"));
            var exampleRepaymentMonths = GetSteppedItems(GetInt("exampleRepaymentMonthsMin"),
                GetInt("exampleRepaymentMonthsMax"), 1);
            var exampleRepaymentDays = applicationSettings.Req("exampleRepaymentDaysIsEnabled") == "true"
                ? GetSteppedItems(GetInt("exampleRepaymentDaysMin"), GetInt("exampleRepaymentDaysMax"), 1,
                    global: (Min: 10, Max: 30))
                : [];
            return (
                RepaymentTimes: exampleRepaymentDays.Select(x => $"{x}d")
                    .Concat(exampleRepaymentMonths.Select(y => $"{y}m")).ToList(),
                LoanAmounts: exampleLoanAmounts);

            List<int> GetSteppedItems(int min, int max, int step, (int Min, int Max)? global = null)
            {
                if (min > max)
                    throw new Exception("Web application calculator min > max");

                if (min <= 0 || max <= 0 || step <= 0)
                    throw CreateDisabledException();

                if (global.HasValue)
                {
                    min = Math.Max(min, global.Value.Min);
                    max = Math.Min(max, global.Value.Max);
                }

                var items = new List<int>();
                var value = min;
                do
                {
                    items.Add(value);
                    if (items.Count > 1000) throw new Exception("Calculator step too small. Caused > 1000 items");
                    value += step;
                } while (value <= max);

                return items;
            }

            int GetInt(string name) => applicationSettings.ReqParse(name, int.Parse);
        }

        NTechCoreWebserviceException CreateDisabledException() => new("Disabled")
            { ErrorCode = "applicationDisabled" };
    }

    [NTechApi]
    [HttpPost]
    [Route("api/embedded-customerpages/custom-costs")]
    [AllowAnonymous]
    public async Task<ActionResult> CustomCosts()
    {
        var creditClient = LegacyServiceClientFactory.CreateCreditClient(LegacyHttpServiceSystemUser.SharedInstance,
            NEnv.ServiceRegistry);
        var result = new JsonNetActionResult
        {
            Data = (await creditClient.GetCustomCostsAsync()).Select(x => new
            {
                x.Text,
                x.Code
            }).ToList(),
            SerializerSettings =
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }
        };
        return result;
    }

    private static object GetDataSharingSettings(IClientConfigurationCore clientCfg, INTechEnvironment env)
    {
        var isDataSharingEnabled = clientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans.datasharing");
        if (!isDataSharingEnabled)
            return null;
        var kreditzSettings = KreditzApiClient.GetSettings(env);
        return new
        {
            ProviderName = KreditzApiClient.DataSharingProviderName,
            kreditzSettings.UseMock,
            kreditzSettings.IFrameClientId,
            kreditzSettings.TestCivicRegNr,
            kreditzSettings.FetchMonthCount
        };
    }

    [NTechApi]
    [HttpPost]
    [Route("api/embedded-customerpages/parse-civicregnr")]
    [AllowAnonymous]
    public ActionResult ParseCivicRegNr(string civicRegNr, string countryCode)
    {
        if (string.IsNullOrWhiteSpace(civicRegNr)) return Json2(new { isValid = false });
        if (string.IsNullOrWhiteSpace(countryCode))
            countryCode = NEnv.ClientCfg.Country.BaseCountry;
        var parser = new CivicRegNumberParser(countryCode);
        if (parser.TryParse(civicRegNr, out var validCivicRegNr))
        {
            return Json2(new
            {
                isValid = true,
                normalizedValue = validCivicRegNr.NormalizedValue,
                ageInYears = validCivicRegNr.BirthDate.HasValue
                    ? (int)Math.Floor(
                        (decimal)Dates.GetAbsoluteNrOfMonthsBetweenDates(Clock.Today,
                            validCivicRegNr.BirthDate.Value) / 12m)
                    : new int?(),
                isMale = validCivicRegNr.IsMale
            });
        }

        return Json2(new { isValid = false });
    }

    [NTechApi]
    [AllowAnonymous]
    [HttpPost]
    public ActionResult ForwardedAnonymousApiCall()
    {
        var moduleName = NTechServiceRegistry.NormalizePath(RouteData.Values["module"] as string);
        var localPath = NTechServiceRegistry.NormalizePath(RouteData.Values["path"] as string);

        if (!IsApiWhiteListedForProxying(moduleName, localPath))
            return NTechWebserviceMethod.ToFrameworkErrorActionResult(
                NTechWebserviceMethod.CreateErrorResponse("That api either doesnt exist or is not whitelisted",
                    errorCode: "notFoundOrNotWhitelisted", httpStatusCode: 400));

        return SendForwardApiCall(request => null, moduleName, localPath);
    }

    public static void RegisterRoutes(RouteCollection routes)
    {
        routes.MapRoute(
            name: "AnonymousEmbeddedCustomerPagesControllerForwardedApiCalls",
            url: "api/embedded-customerpages/anonymous-proxy/{module}/{*path}",
            defaults: new { controller = "AnonymousEmbeddedCustomerPages", action = "ForwardedAnonymousApiCall" }
        );
    }
}