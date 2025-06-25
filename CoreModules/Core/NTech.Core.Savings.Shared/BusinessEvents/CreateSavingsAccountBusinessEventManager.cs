using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.Conversion;
using NTech.Banking.Shared.BankAccounts.Fi;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Savings.Shared.Database;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFixed;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;
using NTech.Core.Savings.Shared.Services;
using NTech.Core.Savings.Shared.Services.Utilities;

namespace NTech.Core.Savings.Shared.BusinessEvents
{
    public enum SavingsApplicationItemName
    {
        savingsAccountTypeCode,
        customerCivicRegNr,
        customerAddressCity,
        customerAddressStreet,
        customerAddressZipcode,
        customerAddressCountry,
        customerAddressSourceTypeCode, //One of SavingsApplicationCustomerContactInfoSourceType
        customerFirstName,
        customerLastName,
        customerNameSourceTypeCode, //One of SavingsApplicationCustomerContactInfoSourceType
        customerEmail,
        customerPhone,
        customerContactInfoSourceWarningCode, //One of SavingsApplicationCustomerContactInfoSourceWarningCode
        customerContactInfoSourceWarningMessage, //Free text description of the warning. This needs to be encrypted for storage
        signedAgreementDocumentArchiveKey,
        customerId,
        savingsAccountNr,
        withdrawalIban,
        fixedInterestProduct
    }

    public enum SavingsApplicationCustomerContactInfoSourceWarningCode
    {
        ProviderDown, //No answer or error from the address provider
        InfoMissing, //Got an answer but adress or name was missing or incomplete
        RequiresManualAttention //Customer is flagged as dead for instance which could indicate a stolen e-id. Will also include customerContactInfoSourceWarningStatus
    }

    public enum SavingsApplicationCustomerContactInfoSourceType
    {
        Unknown,
        Customer, //Customer entered it
        TrustedParty //Like folkbokföring
    }

    public class CreateSavingsAccountBusinessEventManager : BusinessEventManagerBaseCore
    {
        private readonly IKeyValueStoreService keyValueStoreService;
        private readonly ISavingsEnvSettings envSettings;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly ICustomerClient customerClient;
        private readonly SavingsContextFactory contextFactory;

        public CreateSavingsAccountBusinessEventManager(
            INTechCurrentUserMetadata currentUser, ICoreClock clock,
            IKeyValueStoreService keyValueStoreService,
            ISavingsEnvSettings envSettings, IClientConfigurationCore clientConfiguration,
            ICustomerClient customerClient, SavingsContextFactory contextFactory) : base(currentUser, clock,
            clientConfiguration)
        {
            this.keyValueStoreService = keyValueStoreService;
            this.envSettings = envSettings;
            this.clientConfiguration = clientConfiguration;
            this.customerClient = customerClient;
            this.contextFactory = contextFactory;
        }

        public class CreationOptions
        {
            public bool AllowCreateWithoutSignedAgreement { get; set; }
            public bool AllowAccountNrGeneration { get; set; }
        }

        public bool TryCreateSavingsAccount(
            ISavingsContext context,
            IList<Tuple<string, string>> applicationItems,
            IList<Tuple<string, string>> externalVariables,
            out string failedMessage,
            out SavingsAccountHeader savingsAccount,
            out IOcrNumber ocrPaymentReference,
            CreationOptions creationOptions = null)
        {
            creationOptions = creationOptions ?? new CreationOptions();

            var civicRegNrItem = applicationItems.SingleOrDefault(x =>
                x.Item1 == nameof(SavingsApplicationItemName.customerCivicRegNr));

            if (civicRegNrItem == null)
            {
                failedMessage = "customerCivicRegNr missing";
                savingsAccount = null;
                ocrPaymentReference = null;
                return false;
            }

            if (!new CivicRegNumberParser(clientConfiguration.Country.BaseCountry).TryParse(civicRegNrItem.Item2,
                    out var civicRegNr))
            {
                failedMessage = "customerCivicRegNr invalid";
                savingsAccount = null;
                ocrPaymentReference = null;
                return false;
            }

            var savingsAccountTypeCodeRaw = applicationItems.FirstOrDefault(x
                    => x.Item1 == nameof(SavingsApplicationItemName.savingsAccountTypeCode))
                ?.Item2;
            var savingsAccountTypeCode = Enums.Parse<SavingsAccountTypeCode>(savingsAccountTypeCodeRaw);
            if (!savingsAccountTypeCode.HasValue)
            {
                failedMessage = "Missing or invalid savingsAccountTypeCode";
                savingsAccount = null;
                ocrPaymentReference = null;
                return false;
            }

            var withdrawalIbanRaw = applicationItems
                .FirstOrDefault(x => x.Item1 == nameof(SavingsApplicationItemName.withdrawalIban))
                ?.Item2;
            IBANFi withdrawalIban;
            if (string.IsNullOrWhiteSpace(withdrawalIbanRaw))
            {
                withdrawalIban = null;
            }
            else if (!IBANFi.TryParse(withdrawalIbanRaw, out withdrawalIban))
            {
                failedMessage = "Invalid withdrawalIban";
                savingsAccount = null;
                ocrPaymentReference = null;
                return false;
            }

            var customerId = customerClient.GetCustomerId(civicRegNr);

            if (savingsAccountTypeCode is SavingsAccountTypeCode.StandardAccount &&
                context.SavingsAccountHeadersQueryable.Any(x =>
                    x.MainCustomerId == customerId && x.Status != nameof(SavingsAccountStatusCode.Closed)))
            {
                failedMessage = "Customer already has a standard account that is not closed";
                savingsAccount = null;
                ocrPaymentReference = null;
                return false;
            }

            var savingsAccountNr = applicationItems
                .SingleOrDefault(x => x.Item1 == nameof(SavingsApplicationItemName.savingsAccountNr))?.Item2;
            if (string.IsNullOrWhiteSpace(savingsAccountNr))
            {
                if (creationOptions.AllowAccountNrGeneration)
                {
                    savingsAccountNr = GenerateNewSavingsAccountNumber(contextFactory);
                }
                else
                {
                    //NOTE: Alternatively we might support generating a new nr here if it does not exist already
                    failedMessage = "Missing savingsAccountNr. Supply one or opt in to generation.";
                    savingsAccount = null;
                    ocrPaymentReference = null;
                    return false;
                }
            }

            if (context.SavingsAccountHeadersQueryable.Any(x => x.SavingsAccountNr == savingsAccountNr))
            {
                failedMessage = "An account with this savingsAccountNr already exists";
                savingsAccount = null;
                ocrPaymentReference = null;
                return false;
            }

            var signedAgreementDocumentArchiveKey = applicationItems.SingleOrDefault(x =>
                x.Item1 == nameof(SavingsApplicationItemName.signedAgreementDocumentArchiveKey))?.Item2;
            if (string.IsNullOrWhiteSpace(signedAgreementDocumentArchiveKey) &&
                !creationOptions.AllowCreateWithoutSignedAgreement)
            {
                failedMessage = "Missing signedAgreementDocumentArchiveKey";
                savingsAccount = null;
                ocrPaymentReference = null;
                return false;
            }

            string productId = null;
            FixedAccountProduct product = null;
            if (savingsAccountTypeCode == SavingsAccountTypeCode.FixedInterestAccount)
            {
                productId =
                    applicationItems.Single(s => s.Item1 == nameof(SavingsApplicationItemName.fixedInterestProduct))
                        .Item2;
                product = context.FixedAccountProductQueryable.Single(p => p.Id == productId);
            }

            var evt = AddBusinessEvent(BusinessEventType.AccountCreation, context);
            var h = new SavingsAccountHeader
            {
                SavingsAccountNr = savingsAccountNr,
                AccountTypeCode = savingsAccountTypeCode.Value.ToString(),
                CreatedByEvent = evt,
                MainCustomerId = customerId,
                FixedInterestProduct = productId,
                MaturesAt = savingsAccountTypeCode == SavingsAccountTypeCode.FixedInterestAccount
                    ? GetMaturityDate(product)
                    : null
            };
            FillInInfrastructureFields(h);
            context.AddSavingsAccountHeaders(h);

            var frozenRemarks = new List<SavingsAccountCreationRemark>();

            //Validera kunduppgifter med om kunden inte har det sedan tidigare
            var createCustomerRequest = new CreateOrUpdatePersonRequest
            {
                CivicRegNr = civicRegNr.NormalizedValue,
                ExpectedCustomerId = customerId,
                Properties = new List<CreateOrUpdatePersonRequest.Property>()
            };

            var customerContactInfoSourceWarningCode =
                Enums.Parse<SavingsApplicationCustomerContactInfoSourceWarningCode>(
                    applicationItems.SingleOrDefault(x =>
                        x.Item1 == nameof(SavingsApplicationItemName.customerContactInfoSourceWarningCode))?.Item2);

            if (customerContactInfoSourceWarningCode.HasValue)
            {
                var customerContactInfoSourceWarningMessage = applicationItems.SingleOrDefault(x =>
                    x.Item1 == nameof(SavingsApplicationItemName.customerContactInfoSourceWarningMessage))?.Item2;

                AddFrozenAttention(SavingsAccountCreationRemarkCode.ContactInfoLookupIssue, JsonConvert.SerializeObject(
                    new
                    {
                        customerId = customerId,
                        customerContactInfoSourceWarningCode = customerContactInfoSourceWarningCode?.ToString(),
                        customerContactInfoSourceWarningMessage =
                            customerContactInfoSourceWarningMessage //TODO: Switch this to an id of an encrypted item if there is time
                    }));
            }

            //Merge in any included customer info            
            foreach (var i in applicationItems)
            {
                var name = GetCustomerPropertyFromApplicationItem(i.Item1);
                if (name != null)
                {
                    createCustomerRequest.Properties.Add(new CreateOrUpdatePersonRequest.Property
                    {
                        Name = name,
                        Value = i.Item2
                    });
                }
            }

            customerClient.CreateOrUpdatePerson(createCustomerRequest);

            CheckForOtherCustomersWithSameData("email", "email", SavingsAccountCreationRemarkCode.FraudCheckSameEmail);
            CheckForOtherCustomersWithSameData("phone", "phone", SavingsAccountCreationRemarkCode.FraudCheckSamePhone);

            var isKycScreenDone = TryKycScreen(customerId);
            if (!isKycScreenDone)
            {
                AddFrozenAttention(SavingsAccountCreationRemarkCode.KycScreenFailed,
                    JsonConvert.SerializeObject(new { customerId = customerId }));
            }
            else
            {
                customerClient.SetupCustomerKycDefaults(new SetupCustomerKycDefaultsRequest
                {
                    CustomerIds = new List<int> { customerId },
                    OnlyThisSourceType = $"SavingsAccount_{savingsAccountTypeCode}",
                    OnlyThisSourceId = savingsAccountNr
                });
            }

            var kycScreenProperties = new HashSet<string>
            {
                "localIsPep",
                "localIsSanction",
            };
            var taxOrCitizenProperties = new HashSet<string>
            {
                "includeInFatcaExport",
                "taxcountries",
                "citizencountries"
            };
            var propertyStatus = customerClient.CheckPropertyStatus(customerId,
                kycScreenProperties.Concat(taxOrCitizenProperties).ToHashSetShared());

            if (isKycScreenDone && propertyStatus.MissingPropertyNames.Any(x => kycScreenProperties.Contains(x)))
            {
                //No need to add both this and KycScreenFailed when isKycScreenDone = false but this could be changed with no issues. It's just duplicate info for the handler.
                AddFrozenAttention(SavingsAccountCreationRemarkCode.KycAttentionNeeded,
                    JsonConvert.SerializeObject(new { customerId = customerId }));
            }

            if (propertyStatus.MissingPropertyNames.Any(x => taxOrCitizenProperties.Contains(x)))
            {
                AddFrozenAttention(SavingsAccountCreationRemarkCode.UnknownTaxOrCitizenCountry,
                    JsonConvert.SerializeObject(new { customerId = customerId }));
            }

            if (withdrawalIban != null)
            {
                var ibanLookupValue = withdrawalIban.NormalizedValue;
                //Intentionally also looking at historical values. Upside: More frauds can be found. Dowside: More false positives also. May need to be changed to only look at the current on each
                var otherSavingsAccountsWithSameWithdrawalIban = context
                    .SavingsAccountHeadersQueryable
                    .Where(x =>
                        x.MainCustomerId != customerId &&
                        x.DatedStrings.Any(y =>
                            y.Name == nameof(DatedSavingsAccountStringCode.WithdrawalIban) &&
                            y.Value == ibanLookupValue))
                    .Select(x => x.SavingsAccountNr)
                    .Distinct()
                    .ToList();
                foreach (var otherSavingsAccountNr in otherSavingsAccountsWithSameWithdrawalIban)
                {
                    AddFrozenAttention(SavingsAccountCreationRemarkCode.FraudCheckSameWithdrawalIban,
                        JsonConvert.SerializeObject(new { savingsAccountNr = otherSavingsAccountNr }));
                }
            }

            if (HasAccountFreezeCheckpoint(customerId))
            {
                AddFrozenAttention(SavingsAccountCreationRemarkCode.CustomerCheckpoint,
                    JsonConvert.SerializeObject(new { customerId = customerId }));
            }

            var externalVariablesComment = "";
            if (externalVariables != null && externalVariables.Count > 0)
            {
                var key = Guid.NewGuid().ToString();
                keyValueStoreService.SetValue(key,
                    nameof(KeyValueStoreKeySpaceCode.SavingsExternalVariablesV1),
                    JsonConvert.SerializeObject(externalVariables.Select(x =>
                        new { Name = x.Item1, Value = x.Item2 })));
                AddDatedSavingsAccountString(nameof(DatedSavingsAccountStringCode.ExternalVariablesKey), key,
                    context, savingsAccount: h, businessEvent: evt);
                externalVariablesComment =
                    $". External variables: {string.Join(", ", externalVariables.Select(x => $"{x.Item1}={x.Item2}"))}";
            }

            SetStatus(h,
                frozenRemarks.Any() ? SavingsAccountStatusCode.FrozenBeforeActive : SavingsAccountStatusCode.Active,
                evt, context);

            var attachmentArchiveKeys = signedAgreementDocumentArchiveKey == null
                ? null
                : new List<string> { signedAgreementDocumentArchiveKey };
            AddComment(
                frozenRemarks.Any()
                    ? $"Account created as frozen pending control due to remark: {string.Join(", ", frozenRemarks.Select(x => x.RemarkCategoryCode).Distinct().ToList())}{externalVariablesComment}"
                    : $"Account created as open{externalVariablesComment}",
                BusinessEventType.AccountCreation, context, savingsAccount: h,
                attachmentArchiveKeys: attachmentArchiveKeys);

            //Deposit ocr
            var g = new SavingsOcrPaymentReferenceGenerator(contextFactory, clientConfiguration.Country.BaseCountry,
                sequenceNrShift: OcrSequenceNrShift);
            var ocrNr = g.GenerateNew();
            AddDatedSavingsAccountString(nameof(DatedSavingsAccountStringCode.OcrDepositReference), ocrNr.NormalForm,
                context, businessEvent: evt, savingsAccount: h);

            if (!string.IsNullOrWhiteSpace(signedAgreementDocumentArchiveKey))
            {
                AddDatedSavingsAccountString(nameof(DatedSavingsAccountStringCode.SignedInitialAgreementArchiveKey),
                    signedAgreementDocumentArchiveKey, context, savingsAccount: h, businessEvent: evt);
                AddSavingsAccountDocument(SavingsAccountDocumentTypeCode.InitialAgreement,
                    signedAgreementDocumentArchiveKey, context, savingsAccount: h, businessEvent: evt);
            }

            if (withdrawalIban != null)
                AddDatedSavingsAccountString(nameof(DatedSavingsAccountStringCode.WithdrawalIban),
                    withdrawalIban.NormalizedValue, context, savingsAccount: h, businessEvent: evt);

            failedMessage = null;
            savingsAccount = h;
            ocrPaymentReference = ocrNr;
            return true;

            void CheckForOtherCustomersWithSameData(string searchTermCode, string customerPropertyName,
                SavingsAccountCreationRemarkCode freezeCode)
            {
                var propertyValue = createCustomerRequest.Properties.FirstOrDefault(x => x.Name == customerPropertyName)
                    ?.Value;
                if (propertyValue == null) return;
                var customerIdsWithSameValue = customerClient.FindCustomerIdsMatchingAllSearchTerms(
                    new List<CustomerSearchTermModel>
                    {
                        new CustomerSearchTermModel
                        {
                            TermCode = searchTermCode,
                            TermValue = propertyValue
                        }
                    });
                foreach (var customerIdWithSameEmail in (customerIdsWithSameValue ?? new List<int>()).Except(new[]
                             { customerId }))
                {
                    AddFrozenAttention(freezeCode,
                        JsonConvert.SerializeObject(new { customerId = customerIdWithSameEmail }));
                }
            }

            void AddFrozenAttention(SavingsAccountCreationRemarkCode code, string data)
            {
                var f = new SavingsAccountCreationRemark
                {
                    SavingsAccount = h, CreatedByEvent = evt, RemarkCategoryCode = code.ToString(), RemarkData = data
                };
                FillInInfrastructureFields(f);
                if (frozenRemarks.Any(x => x.RemarkCategoryCode == code.ToString() && x.RemarkData == data)) return;
                //Dont add dupes it just clutters up the ui for no reason
                context.AddSavingsAccountCreationRemarks(f);
                frozenRemarks.Add(f);
            }
        }

        private DateTime? GetMaturityDate(FixedAccountProduct product)
        {
            return Clock.Today.AddMonths(product.TermInMonths);
        }

        private bool TryKycScreen(int customerId)
        {
            try
            {
                var result = customerClient.KycScreenNew(new HashSet<int> { customerId }, Clock.Today, true);
                return result.Opt(customerId) == null;
            }
            catch
            {
                return false;
            }
        }

        private bool HasAccountFreezeCheckpoint(int customerId)
        {
            return customerClient.GetActiveCheckpointIdsOnCustomerIds(
                new HashSet<int> { customerId },
                new List<string> { "SavingsAccountCreationRemark" })?.CheckPointByCustomerId?.Any() == true;
        }

        //To ensure that ocr nrs from test dont overlap with production and that the left most nr is higher so its easy to see its from test
        //This is to make sure that accidenatal deposits made if someone thinks they are using production when in fact they are on test end up unplaced instead of one some elses account
        private long OcrSequenceNrShift =>
            !envSettings.IsProduction && clientConfiguration.Country.BaseCountry == "FI" ? 11111111L : 0L;

        private static string GetCustomerPropertyFromApplicationItem(string i)
        {
            if (!Enum.TryParse(i, out SavingsApplicationItemName itemName)) return null;

            switch (itemName)
            {
                case SavingsApplicationItemName.customerFirstName:
                    return "firstName";
                case SavingsApplicationItemName.customerLastName:
                    return "lastName";
                case SavingsApplicationItemName.customerEmail:
                    return "email";
                case SavingsApplicationItemName.customerPhone:
                    return "phone";
                case SavingsApplicationItemName.customerAddressZipcode:
                    return "addressZipcode";
                case SavingsApplicationItemName.customerAddressStreet:
                    return "addressStreet";
                case SavingsApplicationItemName.customerAddressCity:
                    return "addressCity";
                case SavingsApplicationItemName.customerAddressCountry:
                    return "addressCountry";
                default:
                    return null;
            }
        }

        public static string GenerateNewSavingsAccountNumber(SavingsContextFactory contextFactory)
        {
            return new SavingsAccountNrGenerator(contextFactory).GenerateNewSavingsAccountNr();
        }
    }
}