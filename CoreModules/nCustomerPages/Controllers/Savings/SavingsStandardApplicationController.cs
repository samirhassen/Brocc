using nCustomerPages.Code;
using nCustomerPages.Code.ElectronicIdSignature;
using Newtonsoft.Json;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.CivicRegNumbers;
using NTech.Core.Module.Shared.Clients;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace nCustomerPages.Controllers.Savings
{
    [RoutePrefix("savings")]
    [CustomerPagesAuthorize(AllowEmptyRole = true)]
    [PreventBackButton]
    public class SavingsStandardApplicationController : SavingsBaseController
    {
        private Lazy<ISavingsAgreementElectronicIdSignatureProvider> electronicIdSignatureProvider;
        private const string SavingsApplicationQuestionType = "SavingsAccount_StandardAccount";

        public SavingsStandardApplicationController()
        {
            this.electronicIdSignatureProvider = new Lazy<ISavingsAgreementElectronicIdSignatureProvider>(() =>
                SavingsAgreementElectronicIdSignatureProviderFactory.Create(this.GetExternalLink, this.Url));
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

            decimal interestRatePercent;
            if (!sc.TryGetCurrentInterestRateForStandardAccount(out interestRatePercent))
            {
                return Content("There is no interest rate defined for standard accounts.");
            }

            var civicRegNr = this.CustomerCivicRegNumber;
            if (!this.IsStrongIdentity || civicRegNr == null)
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
                string data;
                if (sc.TryGetTemporarilyEncryptedData(extVarKey, out data))
                {
                    externalApplicationVariables = AffiliateTrackingModel.GetExternalApplicationVariablesFromString(data);
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
                if (statuses != null && statuses.Any(x => x.AccountStatus == "Active"))
                {
                    return Redirect(MakeSavingsOverviewMessageUrl(MessageTypeCode.alreadyhaveaccount));
                }
                else if (statuses != null && statuses.Any(x => x.AccountStatus != "Closed"))
                {
                    return Redirect(MakeSavingsOverviewMessageUrl(MessageTypeCode.accountbeingprocessed));
                }
                else
                {
                    var hasOldClosedSavingsAccount = statuses != null && statuses.All(x => x.AccountStatus == "Closed");
                    HandleContactInfo(customerApplicationStatus, x => existingCustomer = x, x => trustedSourceLookupCustomer = x, civicRegNr, sc, hasOldClosedSavingsAccount);
                }
            }

            var actualStatus = (customerApplicationStatus ?? CustomerSavingsApplicationStatus.NoActiveApplication);

            ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
            {
                isProduction = NEnv.IsProduction,
                civicRegNr = civicRegNr.NormalizedValue,
                interestRatePercent = interestRatePercent,
                existingCustomer = existingCustomer,
                trustedSourceLookupCustomer = trustedSourceLookupCustomer,
                kycQuestions = KycQuestions.FetchJsonResource(NEnv.KycQuestions.ToString()),
                translation = GetTranslations(),
                customerApplicationStatus = actualStatus.ToString(),
                savingsAccountOverviewUrl = Url.Action("Navigate", "CustomerPortal", new { targetName = "SavingsOverview" }),
                cancelUrl = Url.Action("Logout", "Common"),
                externalApplicationVariables
            })));
            return View();
        }

        private void HandleContactInfo(CustomerSavingsApplicationStatus? customerApplicationStatus, Action<object> setExistingCustomer, Action<object> setTrustedSourceLookupCustomer, ICivicRegNumber civicRegNr, SystemUserSavingsClient sc, bool hasOldClosedSavingsAccount)
        {
            var cc = new SystemUserCustomerClient();
            var result = cc.GetCustomerCardItems(CustomerId, "addressCity", "addressStreet", "addressZipcode", "addressCountry", "firstName", "lastName", "email", "phone");

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
                    "firstName", "lastName", "addressStreet", "addressZipcode", "addressCity", "addressCountry", "personStatus");

                var lookupApplicationItems = new Dictionary<string, string>();
                bool trustedSourceHadContactInfo = false;
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
                        lookupApplicationItems["customerContactInfoSourceWarningMessage"] = $"Person status is '{personStatusCode ?? "unknown"}'";
                    }

                    Func<string, bool> hasNonEmptyValueFor =
                        n => trustedSourceResult.Items.ContainsKey(n) && !string.IsNullOrWhiteSpace(trustedSourceResult.Items[n]);

                    trustedSourceHadContactInfo = hasNonEmptyValueFor("firstName") && hasNonEmptyValueFor("addressZipcode");

                    if (trustedSourceHadContactInfo)
                    {
                        lookupApplicationItems[SavingsApplicationItemName.customerFirstName.ToString()] = trustedSourceResult.Items.Opt("firstName");
                        lookupApplicationItems[SavingsApplicationItemName.customerLastName.ToString()] = trustedSourceResult.Items.Opt("lastName");
                        lookupApplicationItems[SavingsApplicationItemName.customerNameSourceTypeCode.ToString()] = "TrustedParty";

                        lookupApplicationItems[SavingsApplicationItemName.customerAddressStreet.ToString()] = trustedSourceResult.Items.Opt("addressStreet");
                        lookupApplicationItems[SavingsApplicationItemName.customerAddressZipcode.ToString()] = trustedSourceResult.Items.Opt("addressZipcode");
                        lookupApplicationItems[SavingsApplicationItemName.customerAddressCity.ToString()] = trustedSourceResult.Items.Opt("addressCity");
                        lookupApplicationItems[SavingsApplicationItemName.customerAddressCountry.ToString()] = trustedSourceResult.Items.Opt("addressCountry");
                        lookupApplicationItems[SavingsApplicationItemName.customerAddressSourceTypeCode.ToString()] = "TrustedParty";
                    }
                    else if (!lookupApplicationItems.ContainsKey("customerContactInfoSourceWarningCode"))
                    {
                        lookupApplicationItems["customerContactInfoSourceWarningCode"] = "InfoMissing";
                    }
                }

                var contactInfoLookupResultEncryptionKey = sc.StoreTemporarilyEncryptedData(JsonConvert.SerializeObject(new
                {
                    appItems = lookupApplicationItems
                }), expireAfterHours: 4);

                setTrustedSourceLookupCustomer(new
                {
                    contactInfoLookupResultEncryptionKey = contactInfoLookupResultEncryptionKey,
                    contact = trustedSourceHadContactInfo ? new
                    {
                        customerAddressCity = lookupApplicationItems.Opt(SavingsApplicationItemName.customerAddressCity.ToString()),
                        customerAddressStreet = lookupApplicationItems.Opt(SavingsApplicationItemName.customerAddressStreet.ToString()),
                        customerAddressZipcode = lookupApplicationItems.Opt(SavingsApplicationItemName.customerAddressZipcode.ToString()),
                        customerAddressCountry = lookupApplicationItems.Opt(SavingsApplicationItemName.customerAddressCountry.ToString()),
                        customerFirstName = lookupApplicationItems.Opt(SavingsApplicationItemName.customerFirstName.ToString()),
                        customerLastName = lookupApplicationItems.Opt(SavingsApplicationItemName.customerLastName.ToString()),
                    } : null
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

        private enum CustomerSavingsApplicationStatus
        {
            NoActiveApplication,
            WaitingForClient,
            CustomerIsAMinor,
            CustomerHasAnActiveAccount
        }

        private int GetFullYearsSince(DateTimeOffset d)
        {
            var t = Clock.Today;
            if (t < d)
                return 0;

            var age = t.Year - d.Year;

            return (d.AddYears(age + 1) <= t) ? (age + 1) : age;
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
            customerContactInfoSourceWarningMessage
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
            if (application == null || application.ApplicationItems == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing application data");
            }

            var sc = new SystemUserSavingsClient();

            var appItems = new Dictionary<string, string>();
            SavingsApplicationItemName _;
            foreach (var a in application.ApplicationItems)
            {
                if (Enum.TryParse(a.Name, out _))
                    appItems[a.Name] = a.Value;
                else
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Item '{a.Name}' is not allowed");
            }

            //Generate an account nr since it's needed for the agreement
            if (appItems.ContainsKey(SavingsApplicationItemName.savingsAccountNr.ToString()))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "savingsAccountNr cannot be included");

            if (appItems.ContainsKey(SavingsApplicationItemName.customerCivicRegNr.ToString()))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "customerCivicRegNr cannot be included");

            if (appItems.ContainsKey(SavingsApplicationItemName.signedAgreementDocumentArchiveKey.ToString()))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "signedAgreementDocumentArchiveKey cannot be included");

            if (appItems.ContainsKey(SavingsApplicationItemName.savingsAccountTypeCode.ToString()))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "savingsAccountTypeCode cannot be included");

            if (appItems.ContainsKey(SavingsApplicationItemName.customerContactInfoSourceWarningCode.ToString()))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "customerContactInfoSourceWarningCode cannot be included");

            if (appItems.ContainsKey(SavingsApplicationItemName.customerContactInfoSourceWarningMessage.ToString()))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "customerContactInfoSourceWarningMessage cannot be included");

            if (appItems.ContainsKey(SavingsApplicationItemName.customerAddressSourceTypeCode.ToString()))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "customerAddressSourceTypeCode cannot be included");

            if (appItems.ContainsKey(SavingsApplicationItemName.customerNameSourceTypeCode.ToString()))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "customerNameSourceTypeCode cannot be included");

            if (appItems.ContainsKey(SavingsApplicationItemName.customerFirstName.ToString()))
                appItems[SavingsApplicationItemName.customerNameSourceTypeCode.ToString()] = IsStrongIdentity ? "Customer" : "Unknown";

            if (appItems.ContainsKey(SavingsApplicationItemName.customerAddressZipcode.ToString()) && IsStrongIdentity)
                appItems[SavingsApplicationItemName.customerAddressSourceTypeCode.ToString()] = IsStrongIdentity ? "Customer" : "Unknown";

            var civicRegNr = CustomerCivicRegNumber;

            if (civicRegNr == null || !IsStrongIdentity)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Savings application requires login using strong identity");
            }

            var withdrawalIbanRaw = appItems.SingleOrDefault(x => x.Key == SavingsApplicationItemName.withdrawalIban.ToString()).Value;
            IBANFi withdrawalIbanParsed;
            if (withdrawalIbanRaw == null || !IBANFi.TryParse(withdrawalIbanRaw, out withdrawalIbanParsed))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "withdrawalIban missing or invalid");
            }
            appItems[SavingsApplicationItemName.withdrawalIban.ToString()] = withdrawalIbanParsed.NormalizedValue;

            appItems[SavingsApplicationItemName.customerCivicRegNr.ToString()] = civicRegNr.NormalizedValue;
            var savingsAccountNr = sc.CreateNewSavingsAccountNumber();
            appItems[SavingsApplicationItemName.savingsAccountNr.ToString()] = savingsAccountNr;

            if (!string.IsNullOrWhiteSpace(application.ContactInfoLookupResultEncryptionKey))
            {
                string data;
                if (!sc.TryGetTemporarilyEncryptedData(application.ContactInfoLookupResultEncryptionKey, out data))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Contact info lookup result is no longer available (1)");
                }
                var contactInfoItems = JsonConvert.DeserializeAnonymousType(data, new { appItems = (Dictionary<string, string>)null })?.appItems;
                if (contactInfoItems == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Contact info lookup result is no longer available (2)");
                foreach (var i in contactInfoItems)
                    appItems[i.Key] = i.Value;
            }

            var cc = new CustomerClient(LegacyHttpServiceSystemUser.SharedInstance, LegacyServiceClientFactory.CreateClientFactory(NEnv.ServiceRegistry));

            var tempDataKey = sc.StoreTemporarilyEncryptedData(JsonConvert.SerializeObject(new
            {
                applicationItems = appItems,
                externalApplicationVariables = application?.ExternalApplicationVariables
            }), expireAfterHours: 2);

            //The tempdata key can contain chars that dont work well in urls so we change the format
            var urlSafeTempDataKey = Urls.ToUrlSafeBase64String(Encoding.UTF8.GetBytes(tempDataKey));

            var redirectUrl = NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages", $"savings/standard-application-afterquestions",
                Tuple.Create("tempDataKey", urlSafeTempDataKey));

            var questionsSession = cc.CreateKycQuestionSession(new CreateKycQuestionSessionRequest
            {
                CustomerIds = new List<int> { CustomerId },
                Language = application.UserLanguage ?? NEnv.ClientCfg.Country.GetBaseLanguage(),
                QuestionsRelationType = SavingsApplicationQuestionType,
                SourceType = SavingsApplicationQuestionType,
                SourceId = savingsAccountNr,
                SourceDescription = "Savings account application",
                RedirectUrl = redirectUrl.ToString(),
                SlidingExpirationHours = 2
            });

            var questionsUrl = NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages", $"n/public-kyc/questions-session/{questionsSession.SessionId}").ToString();

            return Json2(new
            {
                questionsUrl = questionsUrl
            });
        }

        [Route("standard-application-afterquestions")]
        public ActionResult AfterQuestions(string tempDataKey)
        {
            var decodedTempDataKey = Encoding.UTF8.GetString(Urls.FromUrlSafeBase64String(tempDataKey));
            var sc = new SystemUserSavingsClient();
            string plainData;
            if (!sc.TryGetTemporarilyEncryptedData(decodedTempDataKey, out plainData))
            {
                return RedirectToAction("Failed");
            }
            var d = JsonConvert.DeserializeAnonymousType(plainData, new
            {
                applicationItems = (Dictionary<string, string>)null,
                externalApplicationVariables = (List<AffiliateTrackingModel.ExternalApplicationVariable>)null
            });

            var savingsAccountNr = d.applicationItems[SavingsApplicationItemName.savingsAccountNr.ToString()];
            var cc = new CustomerClient(LegacyHttpServiceSystemUser.SharedInstance, LegacyServiceClientFactory.CreateClientFactory(NEnv.ServiceRegistry));
            var customerStatus = cc.FetchCustomerOnboardingStatuses(new HashSet<int> { CustomerId }, SavingsApplicationQuestionType, savingsAccountNr, false).Opt(CustomerId);

            if (!customerStatus.LatestKycQuestionsAnswerDate.HasValue)
            {
                return RedirectToAction("Failed");
            }

            string failedMessage;
            byte[] pdfBytes;
            var agreementService = new SavingsAccountAgreementService();
            if (!agreementService.TryGetAgreementPdf(d.applicationItems, Clock, out pdfBytes, out failedMessage))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            }

            var signatureTempDataKey = sc.StoreTemporarilyEncryptedData(JsonConvert.SerializeObject(d), expireAfterHours: 2);

            var signatureUrl = this.electronicIdSignatureProvider.Value.StartSignatureSessionReturningSignatureUrl(signatureTempDataKey, pdfBytes, CustomerCivicRegNumber, "SavingsAgreement.pdf", null, null, null);

            return Redirect(signatureUrl);
        }

        [Route("standard-application-aftersign")]
        public ActionResult AfterSign() => AfterSign(x => Request.Params.AllKeys.Contains(x) ? this.Request.Params[x] : null);

        [Route("{localSessionId}/standard-application-aftersign")]
        public ActionResult AfterSign(string localSessionId) => AfterSign(x => x == "localSessionId" ? localSessionId : null);

        public ActionResult AfterSign(Func<string, string> getParams)
        {
            var s = new SavingsAccountSignatureService();
            var result = s.CreateSavingsAccountAfterAgreementSigned(getParams, electronicIdSignatureProvider);
            if (!result.IsSuccess)
            {
                return RedirectToAction("Failed");
            }
            else
            {
                var p = new LoginProvider();
                p.ReloginUserToPickUpRoleChanges(this.HttpContext.GetOwinContext());
                return Redirect(MakeSavingsOverviewMessageUrl(result.SuccessCode.Value, newAccountNr: result.CreatedSavingsAccountNr));
            }
        }
    }
}