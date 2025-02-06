using Newtonsoft.Json;
using nPreCredit.Code;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.BankAccounts.Se;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    [NTechAuthorizeCreditMiddle]
    [RoutePrefix("FraudCheck")]
    public class FraudCheckController : NController
    {
        [Route("New")]
        public ActionResult New(string applicationNr, int applicantNr, bool continueExisting)
        {
            List<FraudControlItem> fraudControlItems;
            using (var context = new PreCreditContext())
            {
                FraudControl existingFraudControl = context
                       .FraudControls
                       .SingleOrDefault(x => x.ApplicationNr == applicationNr && x.ApplicantNr == applicantNr && x.IsCurrentData);

                List<string> civicRegNrFields = new List<string>(new string[] { "civicRegNr" });
                List<string> sensitiveFields = new List<string>(new string[] { "lastName", "addressStreet", "addressZipcode", "addressCity", "addressCountry" });

                var repo = DependancyInjection.Services.Resolve<IPartialCreditApplicationModelRepository>();
                var appModel = repo.Get(applicationNr, applicantFields: new List<string> { "customerId" }, applicationFields: new List<string> { NEnv.ClientCfg.Country.BaseCountry == "FI" ? "iban" : "bankAccountNr" });
                int customerId = appModel.Applicant(applicantNr).Get("customerId").IntValue.Required;

                string iban = null;
                string ibanReadable = null;
                string bankAccountNr = null;
                string bankAccountNrReadable = null;
                if (NEnv.ClientCfg.Country.BaseCountry == "FI")
                {
                    iban = appModel.Application.Get("iban").StringValue.Optional;
                    if (!string.IsNullOrWhiteSpace(iban))
                    {
                        IBANFi b;
                        if (IBANFi.TryParse(iban, out b))
                            ibanReadable = $"{b.GroupsOfFourValue} ({NEnv.IBANToBICTranslatorInstance.InferBankName(b)})";
                        else
                            ibanReadable = $"{iban} (Invalid)";
                    }
                }
                else if (NEnv.ClientCfg.Country.BaseCountry == "SE")
                {
                    bankAccountNr = appModel.Application.Get("bankAccountNr").StringValue.Optional;
                    if (!string.IsNullOrWhiteSpace(bankAccountNr))
                    {
                        BankAccountNumberSe b; string _;
                        if (BankAccountNumberSe.TryParse(bankAccountNr, out b, out _))
                            bankAccountNrReadable = $"{b.ClearingNr} {b.AccountNr} ({b.BankName})";
                        else
                            bankAccountNrReadable = $"{bankAccountNr} (Invalid)";
                    }
                }
                else
                    throw new NotImplementedException();

                var insensitive = new PreCreditCustomerClient().BulkFetchPropertiesByCustomerIdsD(new HashSet<int> { customerId }, "firstName", "phone", "email")?.Values?.FirstOrDefault();

                var latestKycAnswers = GetLatestKycAnswers(applicationNr, customerId);

                string phoneNr = insensitive["phone"];
                string email = insensitive["email"];

                if (continueExisting)
                {
                    if (existingFraudControl == null)
                        throw new Exception("Cannot continue working on a non-existing fraud control, applicationNr: " + applicationNr + ", applicantNr: " + applicantNr);
                    fraudControlItems = existingFraudControl.FraudControlItems;
                }
                else
                {
                    if (existingFraudControl != null)
                    {
                        existingFraudControl.IsCurrentData = false;
                    }

                    fraudControlItems = PopulateFraudControlItems(customerId, applicationNr, applicantNr, phoneNr, email, iban);
                    var now = Clock.Now;
                    var app = context.CreditApplicationHeaders.Single(x => x.ApplicationNr == applicationNr);

                    var coApplFraudCtrl = context.FraudControls.Where(x => x.IsCurrentData && x.ApplicationNr == applicationNr && x.ApplicantNr != applicantNr).SingleOrDefault();
                    if (coApplFraudCtrl != null)
                    {
                        if (coApplFraudCtrl.Status != FraudCheckStatusCode.Rejected)
                        {
                            app.FraudCheckStatus = CreditApplicationMarkerStatusName.Initial;
                        }
                    }
                    else
                    {
                        app.FraudCheckStatus = CreditApplicationMarkerStatusName.Initial;
                    }

                    var fraudControl = new FraudControl
                    {
                        InformationMetaData = InformationMetadata,
                        ApplicantNr = applicantNr,
                        ChangedById = CurrentUserId,
                        ChangedDate = now,
                        ApplicationNr = applicationNr,
                        Status = FraudCheckStatusCode.Unresolved,
                        FraudControlItems = fraudControlItems,
                        ReplacesFraudControl = existingFraudControl,
                        IsCurrentData = true,
                    };
                    context.FraudControlItems.AddRange(fraudControlItems);
                    context.FraudControls.Add(fraudControl);
                    context.SaveChanges();
                }

                dynamic customerModel = new ExpandoObject();
                customerModel.insensitive = insensitive;
                List<CustomerPropertyModel> customerProps = new List<CustomerPropertyModel>();
                foreach (string sf in sensitiveFields)
                {
                    customerProps.Add(new CustomerPropertyModel
                    {
                        Name = sf,
                        Group = "sensitive",
                        CustomerId = customerId,
                        Value = $"-- {sf} --"
                    });
                }
                foreach (string sf in civicRegNrFields)
                {
                    customerProps.Add(new CustomerPropertyModel
                    {
                        Name = sf,
                        Group = "civicRegNr",
                        CustomerId = customerId,
                        Value = $"-- {sf} --"
                    });
                }
                customerModel.sensitive = customerProps;

                var urlToHere = Url.Action("New", "FraudCheck", new { applicationNr = applicationNr, applicantNr = applicantNr, continueExisting = continueExisting });

                var kycQuestions = NEnv.KycQuestions?.ToString();
                var fraudControlViewItems = fraudControlItems
                                            .Select(x => new
                                            {
                                                x.Id,
                                                DecisionByName = GetUserDisplayNameByUserId(x.ChangedById.ToString()),
                                                x.Status,
                                                x.Key,
                                                x.ChangedDate,
                                                x.Value,
                                                RefUrl = GetRefUrl(x, applicationNr, true, continueExisting, applicantNr)
                                            })
                        .OrderByDescending(x => x.Id)
                        .ToList();

                SetInitialData(new
                {
                    latestKycAnswers,
                    customerModel,
                    customerId,
                    applicationNr,
                    applicantNr,
                    iban,
                    ibanReadable,
                    bankAccountNr,
                    bankAccountNrReadable,
                    fraudControlViewItems,
                    unlockSensitiveItemUrl = Url.Action("UnlockSensitiveItem"),
                    rejectItemUrl = Url.Action("RejectItem"),
                    approveItemUrl = Url.Action("ApproveItem"),
                    verifyItemUrl = Url.Action("VerifyItem")
                });
                return View();
            }
        }

        [Route("View")]
        public ActionResult View(string applicationNr, int applicantNr)
        {
            List<FraudControlItem> fraudControlItems;
            using (var context = new PreCreditContext())
            {
                FraudControl existingFraudControl = context
                       .FraudControls
                       .SingleOrDefault(x => x.ApplicationNr == applicationNr && x.ApplicantNr == applicantNr && x.IsCurrentData);

                List<string> civicRegNrFields = new List<string>(new string[] { "civicRegNr" });
                List<string> sensitiveFields = new List<string>(new string[] { "lastName", "addressStreet", "addressZipcode", "addressCity", "addressCountry" });

                var repo = DependancyInjection.Services.Resolve<IPartialCreditApplicationModelRepository>();
                var appModel = repo.Get(applicationNr, applicantFields: new List<string> { "customerId" }, applicationFields: new List<string> { NEnv.ClientCfg.Country.BaseCountry == "FI" ? "iban" : "bankAccountNr" });
                int customerId = appModel.Applicant(applicantNr).Get("customerId").IntValue.Required;

                string iban = null;
                string ibanReadable = null;
                string bankAccountNr = null;
                string bankAccountNrReadable = null;
                if (NEnv.ClientCfg.Country.BaseCountry == "FI")
                {
                    iban = appModel.Application.Get("iban").StringValue.Optional;
                    if (!string.IsNullOrWhiteSpace(iban))
                    {
                        IBANFi b;
                        if (IBANFi.TryParse(iban, out b))
                            ibanReadable = $"{b.GroupsOfFourValue} ({NEnv.IBANToBICTranslatorInstance.InferBankName(b)})";
                        else
                            ibanReadable = $"{iban} (Invalid)";
                    }
                }
                else if (NEnv.ClientCfg.Country.BaseCountry == "SE")
                {
                    bankAccountNr = appModel.Application.Get("bankAccountNr").StringValue.Optional;
                    if (!string.IsNullOrWhiteSpace(bankAccountNr))
                    {
                        BankAccountNumberSe b; string _;
                        if (BankAccountNumberSe.TryParse(bankAccountNr, out b, out _))
                            bankAccountNrReadable = $"{b.ClearingNr} {b.AccountNr} ({b.BankName})";
                        else
                            bankAccountNrReadable = $"{bankAccountNr} (Invalid)";
                    }
                }
                else
                    throw new NotImplementedException();

                var insensitive = new PreCreditCustomerClient().BulkFetchPropertiesByCustomerIdsD(new HashSet<int> { customerId }, "firstName", "phone", "email")?.Values?.FirstOrDefault();
                string phoneNr = insensitive["phone"];
                string email = insensitive["email"];

                if (existingFraudControl == null)
                    throw new Exception("Cannot view a non-existing fraud control, applicationNr: " + applicationNr + ", applicantNr: " + applicantNr);
                fraudControlItems = existingFraudControl.FraudControlItems;

                var now = Clock.Now;
                dynamic customerModel = new ExpandoObject();
                customerModel.insensitive = insensitive;
                List<CustomerPropertyModel> customerProps = new List<CustomerPropertyModel>();
                foreach (string sf in sensitiveFields)
                {
                    customerProps.Add(new CustomerPropertyModel
                    {
                        Name = sf,
                        Group = "sensitive",
                        CustomerId = customerId,
                        Value = "----"
                    });
                }
                foreach (string sf in civicRegNrFields)
                {
                    customerProps.Add(new CustomerPropertyModel
                    {
                        Name = sf,
                        Group = "civicRegNr",
                        CustomerId = customerId,
                        Value = "----"
                    });
                }
                customerModel.sensitive = customerProps;
                var kycQuestions = NEnv.KycQuestions?.ToString();
                var fraudControlViewItems = fraudControlItems
                                            .Select(x => new
                                            {
                                                x.Id,
                                                DecisionByName = GetUserDisplayNameByUserId(x.ChangedById.ToString()),
                                                x.Status,
                                                x.Key,
                                                x.ChangedDate,
                                                x.Value,
                                                RefUrl = GetRefUrl(x, applicationNr, false, false, applicantNr)
                                            })
                        .OrderByDescending(x => x.Id)
                        .ToList();

                var latestKycAnswers = GetLatestKycAnswers(applicationNr, customerId);

                SetInitialData(new
                {
                    FraudCheckStatus = existingFraudControl.Status,
                    latestKycAnswers,
                    customerModel,
                    customerId,
                    applicationNr,
                    applicantNr,
                    iban,
                    ibanReadable,
                    bankAccountNr,
                    bankAccountNrReadable,
                    fraudControlViewItems,
                    unlockSensitiveItemUrl = Url.Action("UnlockSensitiveItem"),
                });
                return View();
            }
        }

        private object GetLatestKycAnswers(string applicationNr, int customerId)
        {
            return LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry)
                 .FetchCustomerOnboardingStatuses(new HashSet<int> { customerId }, "UnsecuredLoanApplication", applicationNr, true)
                 .Opt(customerId)
                 ?.LatestKycQuestionsSet
                 ?.Items
                 ?.Select(x => new { x.QuestionText, x.AnswerText })
                 ?.ToList();
        }

        private string GetRefUrl(FraudControlItem item, string applicationNr, bool isNew, bool isContinueExisting, int applicantNr)
        {
            string refUrl = null;
            if (item.Key == FraudControlItem.CheckEmail || item.Key == FraudControlItem.CheckAccountNr || item.Key == FraudControlItem.CheckAddress)
            {
                var backTarget = NTechNavigationTarget.CreateCrossModuleNavigationTargetCode(
                    $"UnsecuredLoanFraudCheck{(isNew ? "New" : "View")}",
                    new Dictionary<string, string>
                    {
                        { "applicationNr", applicationNr },
                        { "continueExisting", isContinueExisting ? "True" : "False" },
                        { "applicantNr", applicantNr.ToString() },
                    });
                refUrl = NEnv.ServiceRegistry.External.ServiceUrl("nPreCredit", "CreditManagement/CreditApplication",
                    Tuple.Create("backTarget", backTarget),
                    Tuple.Create("applicationNr", item.Value)).ToString();
            }
            return refUrl;
        }
        public class FraudControlItemViewModel
        {
            public string Key { get; set; }
            public string Value { get; set; }
            public string Status { get; set; }
            public string RefUrl { get; set; }
        }

        private List<FraudControlItem> PopulateFraudControlItems(int customerId, string applicationNr, int applicantNr, string phoneNr, string email, string iban)
        {
            List<FraudControlItem> fraudControlItems = new List<FraudControlItem>();
            var now = Clock.Now;
            fraudControlItems.Add(new FraudControlItem
            {
                ChangedById = CurrentUserId,
                ChangedDate = now,
                InformationMetaData = InformationMetadata,
                Key = FraudControlItem.CheckPhone,
                Value = phoneNr,
                Status = FraudControlItem.Initial
            });

            var repo = DependancyInjection.Services.Resolve<IPartialCreditApplicationModelRepository>();
            var appModel = repo.Get(applicationNr, applicantFields: new List<string> { "employment" });
            string employmentForm = appModel.Applicant(applicantNr).Get("employment").StringValue.Optional;
            if (employmentForm != null && employmentForm == "employment_foretagare")
            {
                fraudControlItems.Add(new FraudControlItem
                {
                    ChangedById = CurrentUserId,
                    ChangedDate = now,
                    InformationMetaData = InformationMetadata,
                    Key = FraudControlItem.CheckEmployment,
                    Value = "Yes",
                    Status = FraudControlItem.Initial
                });
            }

            var customerClient = new PreCreditCustomerClient();
            List<int> customerIdsWithSameEmail = customerClient.GetCustomerIdsWithSameData("email", email);
            List<string> applicationNrsWithSameIban = GetApplicationNrsWithSameData("iban", iban);
            List<int> customerIdsWithSameAddress = customerClient.GetCustomerIdsWithSameAdress(customerId, false);
            fraudControlItems.AddRange(PopulateItems(customerIdsWithSameEmail, applicationNr, FraudControlItem.CheckEmail, customerId));
            fraudControlItems.AddRange(CreateFraudControlItems(applicationNrsWithSameIban, applicationNr, FraudControlItem.CheckAccountNr));
            fraudControlItems.AddRange(PopulateItems(customerIdsWithSameAddress, applicationNr, FraudControlItem.CheckAddress, customerId));

            var creditClient = new CreditClient();
            var applicantCredits = creditClient.GetCustomerCreditHistory(new List<int>() { customerId });

            var otherRecentLoans = applicantCredits
                .OrderByDescending(x => x.StartDate.AddMonths(24) >= Clock.Now)
                .ToList();

            if (otherRecentLoans.Count > 0)
            {
                fraudControlItems.Add(new FraudControlItem
                {
                    ChangedById = CurrentUserId,
                    ChangedDate = now,
                    InformationMetaData = InformationMetadata,
                    Key = FraudControlItem.CheckOtherApprovedLoanRecently,
                    Value = "Yes",
                    Status = FraudControlItem.Initial
                });
            }
            return fraudControlItems;
        }

        private List<string> GetApplicationNrsWithSameData(string name, string value)
        {
            using (var db = new PreCreditContext())
            {
                return db
                .CreditApplicationItems
                .Where(x =>
                    x.Name == name &&
                    x.Value == value
                 )
                .ToList()
                .Select(x => x.ApplicationNr)
                .ToList();
            }
        }

        private static IEnumerable<IEnumerable<T>> SplitIntoGroupsOfN<T>(T[] array, int n)
        {
            for (var i = 0; i < (float)array.Length / n; i++)
            {
                yield return array.Skip(i * n).Take(n);
            }
        }

        private List<FraudControlItem> PopulateItems(List<int> customerIds, string applicationNr, string key, int customerId)
        {
            var allApplicationNrs = new List<string>();
            customerIds.RemoveAll(x => x == customerId);
            using (var context = new PreCreditContext())
            {
                var cidStrings = customerIds.Distinct().Select(x => x.ToString()).ToArray();
                foreach (var customerIdGroup in SplitIntoGroupsOfN(cidStrings, 500))
                {
                    allApplicationNrs.AddRange(context
                            .CreditApplicationItems
                            .Where(x => x.Name == "customerId" && cidStrings.Contains(x.Value))
                            .Select(x => x.ApplicationNr)
                            .ToList());
                }

                return CreateFraudControlItems(allApplicationNrs.Distinct().ToList(), applicationNr, key);
            }
        }

        private List<FraudControlItem> CreateFraudControlItems(List<string> applicationNrsWithSameData, string applicationNr, string key)
        {
            List<FraudControlItem> fraudControlItems = new List<FraudControlItem>();
            var now = Clock.Now;
            foreach (var applNr in applicationNrsWithSameData)
            {
                if (applNr != applicationNr)
                {
                    fraudControlItems.Add(new FraudControlItem
                    {
                        ChangedById = CurrentUserId,
                        ChangedDate = now,
                        InformationMetaData = InformationMetadata,
                        Key = key,
                        Value = applNr,
                        Status = FraudControlItem.Initial
                    });
                }
            }
            return fraudControlItems;
        }

        public class CustomerPropertyModel
        {
            public string Name { get; set; }
            public string Group { get; set; }
            public int CustomerId { get; set; }
            public string Value { get; set; }
        }

        [Route("UnlockSensitiveItem")]
        [HttpPost]
        public string UnlockSensitiveItem(CustomerPropertyModel item)
        {
            var c = new PreCreditCustomerClient();
            return c.UnlockSensitiveItem(item.CustomerId, item.Name);
        }

        [Route("RejectItem")]
        [HttpPost]
        public string RejectItem(int fraudControlItemId, string applicationNr, int applicantNr)
        {
            using (var context = new PreCreditContext())
            {
                FraudControlItem fraudControlItem = context
                       .FraudControlItems
                       .SingleOrDefault(x => x.Id == fraudControlItemId);
                fraudControlItem.Status = FraudControlItem.Rejected;

                var app = context.CreditApplicationHeaders.Single(x => x.ApplicationNr == applicationNr);
                app.FraudCheckStatus = CreditApplicationMarkerStatusName.Rejected;

                var fraudControl = context.FraudControls.Single(x => x.ApplicationNr == applicationNr && x.ApplicantNr == applicantNr && x.IsCurrentData);
                if (fraudControl == null)
                {
                    throw new Exception("FraudControl missing for applicationNr " + applicationNr);
                }
                List<string> fraudRejectionReasons;
                if (fraudControl.RejectionReasons != null)
                {
                    fraudRejectionReasons = JsonConvert.DeserializeAnonymousType(fraudControl.RejectionReasons, new List<string>());
                }
                else
                {
                    fraudRejectionReasons = new List<string>();
                }
                var unhandledItems = fraudControl.FraudControlItems.Where(x => x.Status == FraudControlItem.Initial);
                if (unhandledItems.Count() == 0)
                {
                    fraudControl.Status = FraudCheckStatusCode.Rejected;
                }
                fraudRejectionReasons.Add("Fraudcontrol rejected: " + fraudControlItem.Key);
                fraudControl.RejectionReasons = JsonConvert.SerializeObject(fraudRejectionReasons);
                context.SaveChanges();
                return GetUserDisplayNameByUserId(CurrentUserId.ToString());
            }
        }

        [Route("ApproveItem")]
        [HttpPost]
        public string ApproveItem(int fraudControlItemId, string applicationNr, int applicantNr)
        {
            return ApproveOrVerify(fraudControlItemId, applicationNr, applicantNr, FraudControlItem.Approved);
        }

        [Route("VerifyItem")]
        [HttpPost]
        public string VerifyItem(int fraudControlItemId, string applicationNr, int applicantNr)
        {
            return ApproveOrVerify(fraudControlItemId, applicationNr, applicantNr, FraudControlItem.Verified);
        }

        private string ApproveOrVerify(int fraudControlItemId, string applicationNr, int applicantNr, string status)
        {
            using (var context = new PreCreditContext())
            {
                FraudControlItem itemToUpdate = context
                       .FraudControlItems
                       .SingleOrDefault(x => x.Id == fraudControlItemId);
                itemToUpdate.Status = status;

                var fraudControl = context.FraudControls.Where(x => x.IsCurrentData && x.ApplicationNr == applicationNr && x.ApplicantNr == applicantNr).SingleOrDefault();
                if (fraudControl == null)
                {
                    throw new Exception("FraudControl missing for applicationNr " + applicationNr);
                }
                var unapprovedItems = fraudControl.FraudControlItems.Where(x => x.Status == FraudControlItem.Initial || x.Status == FraudControlItem.Rejected);
                var unhandledItems = fraudControl.FraudControlItems.Where(x => x.Status == FraudControlItem.Initial);

                if (unapprovedItems.Count() == 0) //all items are approved
                {
                    fraudControl.Status = FraudCheckStatusCode.Approved;
                    var app = context.CreditApplicationHeaders.Single(x => x.ApplicationNr == applicationNr);
                    var repo = DependancyInjection.Services.Resolve<IPartialCreditApplicationModelRepository>();
                    var appModel = repo.Get(applicationNr, new PartialCreditApplicationModelRequest());
                    var coApplFraudCtrl = context.FraudControls.Where(x => x.IsCurrentData && x.ApplicationNr == applicationNr && x.ApplicantNr != applicantNr).SingleOrDefault();
                    if (appModel.NrOfApplicants == 1)
                    {
                        app.FraudCheckStatus = CreditApplicationMarkerStatusName.Accepted;
                    }
                    else if (coApplFraudCtrl != null && coApplFraudCtrl.Status == FraudCheckStatusCode.Approved)
                    {
                        app.FraudCheckStatus = CreditApplicationMarkerStatusName.Accepted;
                    }
                }
                else if (unhandledItems.Count() == 0) //all items are handled but some are rejected
                {
                    fraudControl.Status = FraudCheckStatusCode.Rejected;
                }
                context.SaveChanges();
            }
            return GetUserDisplayNameByUserId(CurrentUserId.ToString());
        }
    }
}