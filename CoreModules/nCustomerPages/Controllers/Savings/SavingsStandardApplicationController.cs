using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using nCustomerPages.Code;
using nCustomerPages.Code.Clients.Savings.Contract;
using nCustomerPages.Code.ElectronicIdSignature;
using nCustomerPages.Models;
using Newtonsoft.Json;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.Shared.BankAccounts.Fi;
using NTech.Core.Module.Shared.Clients;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using Serilog;

namespace nCustomerPages.Controllers.Savings;

[RoutePrefix("savings")]
[CustomerPagesAuthorize(AllowEmptyRole = true)]
[PreventBackButton]
public class SavingsStandardApplicationController : SavingsBaseController
{
    private readonly Lazy<ISavingsAgreementElectronicIdSignatureProvider> _electronicIdSignatureProvider;
    private const string SavingsApplicationQuestionType = "SavingsAccount_StandardAccount";

    public SavingsStandardApplicationController()
    {
        _electronicIdSignatureProvider = new Lazy<ISavingsAgreementElectronicIdSignatureProvider>(() =>
            SavingsAgreementElectronicIdSignatureProviderFactory.Create(GetExternalLink, Url));
    }

    protected override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        if (!NEnv.IsSavingsApplicationActive)
        {
            filterContext.Result = HttpNotFound();
        }

        base.OnActionExecuting(filterContext);
    }

    [Route("standard-application")]
    public ActionResult Index(string extVarKey)
    {
        ViewBag.HideUserHeader = true;

        var sc = new SystemUserSavingsClient();
        if (!sc.TryGetCurrentInterestRateForStandardAccount(out var interestRatePercent))
        {
            return Content("There is no interest rate defined for standard accounts.");
        }

        List<FixedRateProduct> fixedInterestProducts;
        try
        {
            fixedInterestProducts = sc.GetRatesForFixedInterestAccounts();
        }
        catch (Exception ex)
        {
            NLog.Error("Could not load fixed interest products", ex);
            // Ui will just hide account type choice if no fixed rate products are available
            fixedInterestProducts = [];
        }

        var civicRegNr = CustomerCivicRegNumber;
        if (!IsStrongIdentity || civicRegNr == null)
        {
            Log.Warning("Savings application failed due to missing strong identity");
            return RedirectToAction("Failed");
        }

        var birthDate = civicRegNr.BirthDate;
        if (!birthDate.HasValue)
        {
            Log.Warning("Savings application failed due to missing birthdate");
            return RedirectToAction("Failed");
        }

        List<AffiliateTrackingModel.ExternalApplicationVariable> externalApplicationVariables = null;
        if (extVarKey != null)
        {
            if (sc.TryGetTemporarilyEncryptedData(extVarKey, out var data))
            {
                externalApplicationVariables =
                    AffiliateTrackingModel.GetExternalApplicationVariablesFromString(data);
            }
        }

        CustomerSavingsApplicationStatus? customerApplicationStatus = null;
        object existingCustomer = null;
        object trustedSourceLookupCustomer = null;
        if (GetFullYearsSince(birthDate.Value) < 18)
        {
            customerApplicationStatus = CustomerSavingsApplicationStatus.CustomerIsAMinor;
        }
        else
        {
            var statuses = sc.GetSavingsAccountStatus(new HashSet<int> { CustomerId }).SingleOrDefault().Value;
            // This should no longer happen as it should be possible to open multiple accounts
            //if (statuses != null && statuses.Any(x => x.AccountStatus == "Active"))
            //{
            //    return Redirect(MakeSavingsOverviewMessageUrl(MessageTypeCode.alreadyhaveaccount));
            //}

            //if (statuses != null && statuses.Any(x => x.AccountStatus != "Closed"))
            //{
            //    return Redirect(MakeSavingsOverviewMessageUrl(MessageTypeCode.accountbeingprocessed));
            //}

            var hasOldClosedSavingsAccount = statuses != null && statuses.All(x => x.AccountStatus == "Closed");
            HandleContactInfo(customerApplicationStatus, x => existingCustomer = x,
                x => trustedSourceLookupCustomer = x, civicRegNr, sc, hasOldClosedSavingsAccount);

            customerApplicationStatus = CustomerSavingsApplicationStatus.CustomerHasAnActiveAccount;
        }

        var actualStatus = customerApplicationStatus ?? CustomerSavingsApplicationStatus.NoActiveApplication;

        ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
        {
            isProduction = NEnv.IsProduction,
            civicRegNr = civicRegNr.NormalizedValue,
            interestRatePercent = interestRatePercent,
            fixedInterestProducts = fixedInterestProducts.Select(p =>
                new
                {
                    id = p.Id,
                    name = p.Name,
                    interestRatePercent = p.InterestRate,
                    termInMonths = p.TermInMonths,
                }).ToList(),
            existingCustomer = existingCustomer,
            trustedSourceLookupCustomer = trustedSourceLookupCustomer,
            kycQuestions = KycQuestions.FetchJsonResource(NEnv.KycQuestions.ToString()),
            translation = GetTranslations(),
            customerApplicationStatus = actualStatus.ToString(),
            savingsAccountOverviewUrl =
                Url.Action("Navigate", "CustomerPortal", new { targetName = "SavingsOverview" }),
            cancelUrl = Url.Action("Logout", "Common"),
            externalApplicationVariables
        })));
        var vm = new SavingsAccountApplicationViewModel
        {
            Status = actualStatus
        };
        return View(vm);
    }

    private void HandleContactInfo(CustomerSavingsApplicationStatus? customerApplicationStatus,
        Action<object> setExistingCustomer, Action<object> setTrustedSourceLookupCustomer,
        ICivicRegNumber civicRegNr, SystemUserSavingsClient sc, bool hasOldClosedSavingsAccount)
    {
        var cc = new SystemUserCustomerClient();
        var result = cc.GetCustomerCardItems(CustomerId, "addressCity", "addressStreet", "addressZipcode",
            "addressCountry", "firstName", "lastName", "email", "phone");

        if (result.ContainsKey("addressZipcode") && !hasOldClosedSavingsAccount)
        {
            if (!customerApplicationStatus.HasValue)
            {
                //TODO: It's unclear what to do if there is an existing customer with missing some but not all info
                setExistingCustomer(new
                {
                    contact = new
                    {
                        customerAddressCity = result.SingleOrDefault(x => x.Key == "addressCity").Value,
                        customerAddressStreet = result.SingleOrDefault(x => x.Key == "addressStreet").Value,
                        customerAddressZipcode = result.SingleOrDefault(x => x.Key == "addressZipcode").Value,
                        customerAddressCountry = result.SingleOrDefault(x => x.Key == "addressCountry").Value,
                        customerFirstName = result.SingleOrDefault(x => x.Key == "firstName").Value,
                        customerLastName = result.SingleOrDefault(x => x.Key == "lastName").Value,
                        customerEmail = result.SingleOrDefault(x => x.Key == "email").Value,
                        customerPhone = result.SingleOrDefault(x => x.Key == "phone").Value
                    }
                });
            }
        }
        else
        {
            //New customer or existing customer missing address or existing customer that closed all their accounts, try to lookup the address from a trusted source
            var lc = new CustomerLockedSavingsClient(this.CustomerId);
            var trustedSourceResult = lc.FetchCustomerAddressFromTrustedSource(civicRegNr,
                "firstName", "lastName", "addressStreet", "addressZipcode", "addressCity", "addressCountry",
                "personStatus");

            var lookupApplicationItems = new Dictionary<string, string>();
            var trustedSourceHadContactInfo = false;
            if (!trustedSourceResult.IsSuccess)
            {
                lookupApplicationItems["customerContactInfoSourceWarningCode"] = "ProviderDown";
            }
            else
            {
                var personStatusCode = trustedSourceResult.Items.Opt("personStatus");
                if (personStatusCode != "normal")
                {
                    lookupApplicationItems["customerContactInfoSourceWarningCode"] = "RequiresManualAttention";
                    lookupApplicationItems["customerContactInfoSourceWarningMessage"] =
                        $"Person status is '{personStatusCode ?? "unknown"}'";
                }

                bool HasNonEmptyValueFor(string n) => trustedSourceResult.Items.ContainsKey(n) &&
                                                      !string.IsNullOrWhiteSpace(trustedSourceResult.Items[n]);

                trustedSourceHadContactInfo =
                    HasNonEmptyValueFor("firstName") && HasNonEmptyValueFor("addressZipcode");

                if (trustedSourceHadContactInfo)
                {
                    lookupApplicationItems[nameof(SavingsApplicationItemName.customerFirstName)] =
                        trustedSourceResult.Items.Opt("firstName");
                    lookupApplicationItems[nameof(SavingsApplicationItemName.customerLastName)] =
                        trustedSourceResult.Items.Opt("lastName");
                    lookupApplicationItems[nameof(SavingsApplicationItemName.customerNameSourceTypeCode)] =
                        "TrustedParty";

                    lookupApplicationItems[nameof(SavingsApplicationItemName.customerAddressStreet)] =
                        trustedSourceResult.Items.Opt("addressStreet");
                    lookupApplicationItems[nameof(SavingsApplicationItemName.customerAddressZipcode)] =
                        trustedSourceResult.Items.Opt("addressZipcode");
                    lookupApplicationItems[nameof(SavingsApplicationItemName.customerAddressCity)] =
                        trustedSourceResult.Items.Opt("addressCity");
                    lookupApplicationItems[nameof(SavingsApplicationItemName.customerAddressCountry)] =
                        trustedSourceResult.Items.Opt("addressCountry");
                    lookupApplicationItems[nameof(SavingsApplicationItemName.customerAddressSourceTypeCode)] =
                        "TrustedParty";
                }
                else if (!lookupApplicationItems.ContainsKey("customerContactInfoSourceWarningCode"))
                {
                    lookupApplicationItems["customerContactInfoSourceWarningCode"] = "InfoMissing";
                }
            }

            var contactInfoLookupResultEncryptionKey = sc.StoreTemporarilyEncryptedData(JsonConvert.SerializeObject(
                new
                {
                    appItems = lookupApplicationItems
                }), expireAfterHours: 4);

            setTrustedSourceLookupCustomer(new
            {
                contactInfoLookupResultEncryptionKey = contactInfoLookupResultEncryptionKey,
                contact = trustedSourceHadContactInfo
                    ? new
                    {
                        customerAddressCity =
                            lookupApplicationItems.Opt(nameof(SavingsApplicationItemName.customerAddressCity)),
                        customerAddressStreet =
                            lookupApplicationItems.Opt(nameof(SavingsApplicationItemName.customerAddressStreet)),
                        customerAddressZipcode =
                            lookupApplicationItems.Opt(nameof(SavingsApplicationItemName.customerAddressZipcode)),
                        customerAddressCountry =
                            lookupApplicationItems.Opt(nameof(SavingsApplicationItemName.customerAddressCountry)),
                        customerFirstName =
                            lookupApplicationItems.Opt(nameof(SavingsApplicationItemName.customerFirstName)),
                        customerLastName =
                            lookupApplicationItems.Opt(nameof(SavingsApplicationItemName.customerLastName)),
                    }
                    : null
            });
        }
    }

    [Route("standard-application-failed")]
    public ActionResult Failed()
    {
        ViewBag.HideUserHeader = true;
        ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
        {
            isProduction = NEnv.IsProduction,
            translation = GetTranslations(),
            logoutUrl = Url.Action("Logout", "Common"),
        })));
        return View();
    }


    private static int GetFullYearsSince(DateTimeOffset d)
    {
        var t = Clock.Today;
        if (t < d)
            return 0;

        var age = t.Year - d.Year;

        return d.AddYears(age + 1) <= t ? age + 1 : age;
    }


    public enum SavingsApplicationItemName
    {
        savingsAccountTypeCode,
        customerCivicRegNr,
        customerAddressCity,
        customerAddressStreet,
        customerAddressZipcode,
        customerAddressCountry,
        customerFirstName,
        customerLastName,
        customerEmail,
        customerPhone,
        signedAgreementDocumentArchiveKey,
        savingsAccountNr,
        withdrawalIban,
        customerAddressSourceTypeCode, //Unknown|Customer|TrustedParty
        customerNameSourceTypeCode, //Unknown|Customer|TrustedParty
        customerContactInfoSourceWarningCode, //ProviderDown|InfoMissing|StatusRequiresAttention
        customerContactInfoSourceWarningMessage,
        fixedInterestProduct
    }

    public class ApplicationItem
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class ApplicationApplyModel
    {
        public string UserLanguage { get; set; }
        public string ContactInfoLookupResultEncryptionKey { get; set; }
        public IList<ApplicationItem> ApplicationItems { get; set; }
        public List<AffiliateTrackingModel.ExternalApplicationVariable> ExternalApplicationVariables { get; set; }
    }

    [Route("standard-application-apply")]
    [HttpPost]
    public ActionResult Apply(ApplicationApplyModel application)
    {
        if (application?.ApplicationItems == null)
        {
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing application data");
        }

        var sc = new SystemUserSavingsClient();

        var appItems = new Dictionary<string, string>();
        foreach (var a in application.ApplicationItems)
        {
            if (Enum.TryParse(a.Name, out SavingsApplicationItemName _))
                appItems[a.Name] = a.Value;
            else
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Item '{a.Name}' is not allowed");
        }

        //Generate an account nr since it's needed for the agreement
        if (appItems.ContainsKey(nameof(SavingsApplicationItemName.savingsAccountNr)))
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "savingsAccountNr cannot be included");

        if (appItems.ContainsKey(nameof(SavingsApplicationItemName.customerCivicRegNr)))
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "customerCivicRegNr cannot be included");

        if (appItems.ContainsKey(nameof(SavingsApplicationItemName.signedAgreementDocumentArchiveKey)))
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest,
                "signedAgreementDocumentArchiveKey cannot be included");

        if (!appItems.TryGetValue(nameof(SavingsApplicationItemName.savingsAccountTypeCode), out var item))
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "savingsAccountTypeCode must be included");

        if (item is "FixedInterestAccount" &&
            !appItems.ContainsKey(nameof(SavingsApplicationItemName.fixedInterestProduct)))
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest,
                "fixedInterestProduct must be included for account type FixedInterestAccount");

        if (appItems.ContainsKey(nameof(SavingsApplicationItemName.customerContactInfoSourceWarningCode)))
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest,
                "customerContactInfoSourceWarningCode cannot be included");

        if (appItems.ContainsKey(nameof(SavingsApplicationItemName.customerContactInfoSourceWarningMessage)))
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest,
                "customerContactInfoSourceWarningMessage cannot be included");

        if (appItems.ContainsKey(nameof(SavingsApplicationItemName.customerAddressSourceTypeCode)))
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest,
                "customerAddressSourceTypeCode cannot be included");

        if (appItems.ContainsKey(nameof(SavingsApplicationItemName.customerNameSourceTypeCode)))
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest,
                "customerNameSourceTypeCode cannot be included");

        if (appItems.ContainsKey(nameof(SavingsApplicationItemName.customerFirstName)))
            appItems[nameof(SavingsApplicationItemName.customerNameSourceTypeCode)] =
                IsStrongIdentity ? "Customer" : "Unknown";

        if (appItems.ContainsKey(nameof(SavingsApplicationItemName.customerAddressZipcode)) && IsStrongIdentity)
            appItems[nameof(SavingsApplicationItemName.customerAddressSourceTypeCode)] =
                IsStrongIdentity ? "Customer" : "Unknown";

        var civicRegNr = CustomerCivicRegNumber;
        if (civicRegNr == null || !IsStrongIdentity)
        {
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest,
                "Savings application requires login using strong identity");
        }

        var withdrawalIbanRaw = appItems
            .SingleOrDefault(x => x.Key == nameof(SavingsApplicationItemName.withdrawalIban)).Value;
        if (withdrawalIbanRaw == null || !IBANFi.TryParse(withdrawalIbanRaw, out var withdrawalIbanParsed))
        {
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "withdrawalIban missing or invalid");
        }

        appItems[nameof(SavingsApplicationItemName.withdrawalIban)] = withdrawalIbanParsed.NormalizedValue;

        appItems[nameof(SavingsApplicationItemName.customerCivicRegNr)] = civicRegNr.NormalizedValue;
        var savingsAccountNr = sc.CreateNewSavingsAccountNumber();
        appItems[nameof(SavingsApplicationItemName.savingsAccountNr)] = savingsAccountNr;

        if (!string.IsNullOrWhiteSpace(application.ContactInfoLookupResultEncryptionKey))
        {
            if (!sc.TryGetTemporarilyEncryptedData(application.ContactInfoLookupResultEncryptionKey, out var data))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest,
                    "Contact info lookup result is no longer available (1)");
            }

            var contactInfoItems = JsonConvert
                .DeserializeAnonymousType(data, new { appItems = (Dictionary<string, string>)null })?.appItems;
            if (contactInfoItems == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest,
                    "Contact info lookup result is no longer available (2)");
            foreach (var i in contactInfoItems)
                appItems[i.Key] = i.Value;
        }

        var cc = new CustomerClient(LegacyHttpServiceSystemUser.SharedInstance,
            LegacyServiceClientFactory.CreateClientFactory(NEnv.ServiceRegistry));

        var tempDataKey = sc.StoreTemporarilyEncryptedData(JsonConvert.SerializeObject(new
        {
            applicationItems = appItems,
            externalApplicationVariables = application.ExternalApplicationVariables
        }), expireAfterHours: 2);

        //The tempdata key can contain chars that dont work well in urls so we change the format
        var urlSafeTempDataKey = Urls.ToUrlSafeBase64String(Encoding.UTF8.GetBytes(tempDataKey));

        var redirectUrl = NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages",
            "savings/standard-application-afterquestions",
            Tuple.Create("tempDataKey", urlSafeTempDataKey));

        var questionsSession = cc.CreateKycQuestionSession(new CreateKycQuestionSessionRequest
        {
            CustomerIds = [CustomerId],
            Language = application.UserLanguage ?? NEnv.ClientCfg.Country.GetBaseLanguage(),
            QuestionsRelationType = SavingsApplicationQuestionType,
            SourceType = SavingsApplicationQuestionType,
            SourceId = savingsAccountNr,
            SourceDescription = "Savings account application",
            RedirectUrl = redirectUrl.ToString(),
            SlidingExpirationHours = 2
        });

        var questionsUrl = NEnv.ServiceRegistry.External
            .ServiceUrl("nCustomerPages", $"n/public-kyc/questions-session/{questionsSession.SessionId}")
            .ToString();

        return Json2(new { questionsUrl });
    }

    [Route("standard-application-afterquestions")]
    public ActionResult AfterQuestions(string tempDataKey)
    {
        var decodedTempDataKey = Encoding.UTF8.GetString(Urls.FromUrlSafeBase64String(tempDataKey));
        var sc = new SystemUserSavingsClient();
        if (!sc.TryGetTemporarilyEncryptedData(decodedTempDataKey, out var plainData))
        {
            return RedirectToAction("Failed");
        }

        var d = JsonConvert.DeserializeAnonymousType(plainData, new
        {
            applicationItems = (Dictionary<string, string>)null,
            externalApplicationVariables = (List<AffiliateTrackingModel.ExternalApplicationVariable>)null
        });

        var savingsAccountNr = d.applicationItems[nameof(SavingsApplicationItemName.savingsAccountNr)];
        var cc = new CustomerClient(LegacyHttpServiceSystemUser.SharedInstance,
            LegacyServiceClientFactory.CreateClientFactory(NEnv.ServiceRegistry));
        var customerStatus = cc.FetchCustomerOnboardingStatuses(new HashSet<int> { CustomerId },
            SavingsApplicationQuestionType, savingsAccountNr, false).Opt(CustomerId);

        if (!customerStatus.LatestKycQuestionsAnswerDate.HasValue)
        {
            return RedirectToAction("Failed");
        }

        var agreementService = new SavingsAccountAgreementService();
        if (!SavingsAccountAgreementService.TryGetAgreementPdf(d.applicationItems, Clock, out var pdfBytes,
                out var failedMessage))
        {
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
        }

        var signatureTempDataKey =
            sc.StoreTemporarilyEncryptedData(JsonConvert.SerializeObject(d), expireAfterHours: 2);

        var signatureUrl = this._electronicIdSignatureProvider.Value.StartSignatureSessionReturningSignatureUrl(
            signatureTempDataKey, pdfBytes, CustomerCivicRegNumber, "SavingsAgreement.pdf", null, null, null);

        return Redirect(signatureUrl);
    }

    [Route("standard-application-aftersign")]
    public ActionResult AfterSign() =>
        AfterSign(x => Request.Params.AllKeys.Contains(x) ? this.Request.Params[x] : null);

    [Route("{localSessionId}/standard-application-aftersign")]
    public ActionResult AfterSign(string localSessionId) =>
        AfterSign(x => x == "localSessionId" ? localSessionId : null);

    public ActionResult AfterSign(Func<string, string> getParams)
    {
        var s = new SavingsAccountSignatureService();
        var result =
            SavingsAccountSignatureService.CreateSavingsAccountAfterAgreementSigned(getParams,
                _electronicIdSignatureProvider);
        if (!result.IsSuccess)
        {
            return RedirectToAction("Failed");
        }

        var p = new LoginProvider();
        p.ReloginUserToPickUpRoleChanges(HttpContext.GetOwinContext());
        return Redirect(MakeSavingsOverviewMessageUrl(result.SuccessCode.Value,
            newAccountNr: result.CreatedSavingsAccountNr));
    }
}