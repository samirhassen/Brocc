using nCredit.Code;
using nCredit.Code.EInvoiceFi;
using nCredit.Code.Services;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.BankAccounts.Se;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.Conversion;
using NTech.Banking.OrganisationNumbers;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using static nCredit.Code.Fileformats.OutgoingPaymentFileFormat_SUS_SE;

namespace nCredit
{
    public static class NEnv
    {
        private static Lazy<LegacyCreditEnvSettings> envSettings = new Lazy<LegacyCreditEnvSettings>(() => new LegacyCreditEnvSettings());
        private static Lazy<INotificationProcessSettingsFactory> notificationProcessSettingsFactory = new Lazy<INotificationProcessSettingsFactory>(
            () =>
            {
                var customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
                return new NotificationProcessSettingsFactory2(ClientCfgCore, new CachedSettingsService(customerClient), envSettings.Value);
            });

        public static INotificationProcessSettingsFactory NotificationProcessSettings => notificationProcessSettingsFactory.Value;

        public static ICreditEnvSettings EnvSettings => envSettings.Value;

        public static string CurrentServiceName => "nCredit";

        public static bool IsBundlingEnabled
        {
            get
            {
                var s = Opt("ntech.isbundlingenabled") ?? "true";
                return s.Trim().ToLower() == "true";
            }
        }

        public static string NTechCdnUrl => Opt("ntech.cdn.rooturl");

        public static string BookKeepingRuleFileName => envSettings.Value.BookKeepingRuleFileName;
        public static NTech.Banking.BookKeeping.NtechAccountPlanFile BookKeepingAccountPlan => envSettings.Value.BookKeepingAccountPlan;

        public static bool HasPerLoanDueDay => envSettings.Value.HasPerLoanDueDay;

        public static InterestModelCode ClientInterestModel => envSettings.Value.ClientInterestModel;

        public static bool CreditsUse360DayInterestYear => envSettings.Value.CreditsUse360DayInterestYear;
                
        public static bool IsProduction => envSettings.Value.IsProduction;
        public static ClientConfiguration ClientCfg => envSettings.Value.ClientCfg;

        public static IClientConfigurationCore ClientCfgCore =>
            NTechCache.WithCache("nCredit.ClientCfgCore", TimeSpan.FromMinutes(15), () => ClientConfigurationCoreFactory.CreateUsingNTechEnvironment(E));

        // Currently only used by Balanzia FI. No limits (on changeterms) for other clients. 
        public static Tuple<decimal?, decimal?> MinAndMaxAllowedMarginInterestRate => envSettings.Value.MinAndMaxAllowedMarginInterestRate;

        public static BankAccountNumberParser BaseBankAccountNumberParser
        {
            get
            {
                return NTechCache.WithCache("BaseBankAccountNumberParser", TimeSpan.FromMinutes(5), () => new BankAccountNumberParser(ClientCfg.Country.BaseCountry));
            }
        }

        public static CivicRegNumberParser BaseCivicRegNumberParser
        {
            get
            {
                return NTechCache.WithCache("BaseCivicRegNumberParser", TimeSpan.FromMinutes(5), () => new CivicRegNumberParser(ClientCfg.Country.BaseCountry));
            }
        }

        public static OcrNumberParser BaseOcrNumberParser
        {
            get
            {
                return NTechCache.WithCache("BaseOcrNumberParser", TimeSpan.FromMinutes(5), () => new OcrNumberParser(ClientCfg.Country.BaseCountry));
            }
        }

        public static bool IsManualPaymentDualityRequired
        {
            get
            {
                var v = Opt("ntech.credit.manualpayment.dualitydisabled");
                return !((v ?? ClientCfg.OptionalSetting("ntech.credit.manualpayment.dualitydisabled") ?? "false") == "true");
            }
        }

        public static bool IsMortgageLoanBKIClient => ClientCfg.OptionalSetting("ntech.mortgageloan.clienttype")?.ToLowerInvariant() == "bki";

        public static DirectoryInfo SkinningRootFolder => E.ClientResourceDirectory("ntech.skinning.rootfolder", "Skinning", false);

        public static FileInfo SkinningCssFile => E.ClientResourceFile("ntech.skinning.cssfile", Path.Combine(SkinningRootFolder.FullName, "css\\skinning.css"), false);

        public static UcCreditRegistrySettingsModel UcCreditRegistrySettings
        {
            get
            {
                var f = NTechEnvironment.Instance.StaticResourceFile("ntech.credit.uccreditregistrysettingsfile", "uc-creditregistry-settings.txt", true);
                var settings = NTechSimpleSettings.ParseSimpleSettingsFile(f.FullName);

                return new UcCreditRegistrySettingsModel
                {
                    SharkEndpoint = new Uri(settings.Req("shark.endpoint")),
                    SharkUsername = settings.Req("shark.username"),
                    SharkPassword = settings.Req("shark.password"),
                    SharkCreditorId = settings.Req("shark.creditorid"),
                    SharkSourceSystemId = settings.Req("shark.sourcesystemid"),
                    SharkDeliveryUniqueId = settings.Req("shark.deliveryuniqueid"),
                    SharkGroupId = settings.Opt("shark.groupid"),
                    LogFolder = settings.Opt("shark.logfolder")
                };
            }
        }

        public static bool IsDirectDebitPaymentsEnabled => envSettings.Value.IsDirectDebitPaymentsEnabled;

        public static AutogiroSettingsModel AutogiroSettings => envSettings.Value.AutogiroSettings;
        public static PositiveCreditRegisterSettingsModel PositiveCreditRegisterSettings => envSettings.Value.PositiveCreditRegisterSettings;
        public static bool IsSkinningEnabled => NTechCache.WithCacheS($"ntech.cache.skinningenabled", TimeSpan.FromMinutes(5), () => NEnv.SkinningRootFolder?.Exists ?? false);

        public static EInvoiceFiSettingsFile EInvoiceFiSettingsFile => envSettings.Value.EInvoiceFiSettingsFile;

        public static FileInfo GetOptionalExcelTemplateFilePath(string filename)
        {
            var d = E.ClientResourceDirectory("ntech.excel.templatefolder", "ExcelTemplates", false);
            if (d == null)
                return null;
            return new FileInfo(Path.Combine(d.FullName, filename));
        }

        public static List<string> PluginSourceFolders
        {
            get
            {
                var v = Opt("ntech.credit.plugins.sourcefolders");
                if (v != null)
                    return v.Split(';').Select(x => x).ToList();
                else
                    return null;
            }
        }

        public static List<string> EnabledPluginNames
        {
            get
            {
                var v = Opt("ntech.credit.plugins.enablednames");
                if (v != null)
                    return v.Split(';').ToList();
                else
                    return new List<string>();
            }
        }

        public static DirectoryInfo LogFolder
        {
            get
            {
                var v = Opt("ntech.logfolder");
                if (v == null)
                    return null;
                return new DirectoryInfo(v);
            }
        }

        public static NTechServiceRegistry ServiceRegistryNormal
        {
            get
            {
                return NTechCache.WithCache(
                   "138819bd-fe35-4b25-b104-801150e2dcf601",
                    TimeSpan.FromMinutes(5),
                    () =>
                    {
                        return NTechEnvironment.Instance.ServiceRegistry;
                    });
            }
        }        

        public static NTech.Core.Module.AffiliateModel GetAffiliateModel(string providerName, bool allowMissing = false) => 
            envSettings.Value.GetAffiliateModel(providerName, allowMissing: allowMissing);

        public static List<NTech.Core.Module.AffiliateModel> GetAffiliateModels() => envSettings.Value.GetAffiliateModels();

        public static Tuple<string, string> ApplicationAutomationUsernameAndPassword => Tuple.Create(Req("ntech.automationuser.username"), Req("ntech.automationuser.password"));

        public static DirectoryInfo OutgoingCreditNotificationDeliveryFolder => envSettings.Value.OutgoingCreditNotificationDeliveryFolder;

        public static string MorsExportProfileName => Opt("ntech.credit.morsfile.exportprofile");
        public static string AnnualStatementsExportProfileName => Opt("ntech.credit.annualstatements.exportprofile");
        public static string CreditDataExportProfileName => Opt("ntech.credit.creditdataexport.exportprofile");

        public static IBANToBICTranslator IBANToBICTranslatorInstance => BankAccountValidationService.IBANToBICTranslatorInstance;

        public static NTechServiceRegistry ServiceRegistry
        {
            get
            {
                return NTechCache.WithCache(
                    "4a379ae9-ede3-46fe-ad54-19cf8c164d58",
                    TimeSpan.FromMinutes(5),
                    () => E.ServiceRegistry);
            }
        }

        public static bool IsVerboseLoggingEnabled => (Opt("ntech.isverboseloggingenabled") ?? "false") == "true";

        public static bool IsMortgageLoansEnabled => envSettings.Value.IsMortgageLoansEnabled;
        public static bool IsStandardMortgageLoansEnabled => envSettings.Value.IsStandardMortgageLoansEnabled;

        public static void ThrowIfBothCompanyLoanAndUnsecuredLoanEnabled()
        {
            if (IsCompanyLoansEnabled && IsUnsecuredLoansEnabled)
                throw new Exception("Both company loans and unsecured loans cannot be enabled at the same time");
        }

        public static CreditType ClientCreditType => envSettings.Value.ClientCreditType;

        public static bool IsCompanyLoansEnabled => envSettings.Value.IsCompanyLoansEnabled;
        public static bool IsUnsecuredLoansEnabled => envSettings.Value.IsUnsecuredLoansEnabled;
        public static bool IsStandardUnsecuredLoansEnabled => envSettings.Value.IsStandardUnsecuredLoansEnabled;
        public static bool IsSkinningCssEnabled => NTechCache.WithCacheS($"ntech.cache.skinningcssenabled", TimeSpan.FromMinutes(5), () => NEnv.SkinningCssFile?.Exists ?? false);

        public static void ThrowIfIsStandardUnsecuredLoansEnabled()
        {
            if (IsStandardUnsecuredLoansEnabled)
                throw new Exception("Not allowed when using standard unsecured loans");
        }

        public static bool IsEInvoiceFiEnabled => ClientCfg.IsFeatureEnabled("ntech.feature.einvoicefi.v1");

        public static List<string> KycScreenReportEmails => Opt("ntech.kycscreen.reportemail")?.Split(';')?.ToList();

        public class EncryptionKeySet
        {
            public string CurrentKeyName { get; set; }

            public List<KeyItem> AllKeys { get; set; }

            public string GetKey(string name)
            {
                return AllKeys.Single(x => x.Name == name).Key;
            }

            public Dictionary<string, string> AsDictionary()
            {
                return AllKeys.ToDictionary(x => x.Name, x => x.Key);
            }

            public class KeyItem
            {
                public string Name { get; set; }
                public string Key { get; set; }
            }
        }

        public static EncryptionKeySet EncryptionKeys =>
            JsonConvert.DeserializeObject<EncryptionKeySet>(File.ReadAllText(
                E.StaticResourceFile("ntech.encryption.keysfile", "encryptionkeys.txt", true).FullName));

        public static string TestingOverrideDateFile
        {
            get
            {
                if (IsProduction)
                    return null;
                return Opt("ntech.credit.testing.overridedatefile");
            }
        }

        public static string SieFileEnding => envSettings.Value.SieFileEnding;

        public static string SatExportProfileName => Opt("ntech.satreporting.exportprofile");

        public static string BookKeepingFileExportProfileName => Opt("ntech.credit.bookkeepingfile.exportprofile");

        public static string CreditRemindersExportProfileName => envSettings.Value.CreditRemindersExportProfileName;

        public static string TrapetsAmlExportProfileName => Opt("ntech.trapetsamlreporting.exportprofile");

        public static Code.Trapets.TrapetsKycConfiguration TrapetsAmlConfig => Code.Trapets.TrapetsKycConfiguration.FromXElement(ClientCfg.GetCustomSection("CreditTrapetsAmlConfiguration"));

        public static Tuple<string, string> SatReportingCreditorIdAndName =>
            //Creditorid seems to be the same thing as webbservice userid
            Tuple.Create(Req("ntech.satreporting.creditorid"), Req("ntech.satreporting.creditorname"));


        public static bool IsTemplateCacheDisabled => string.Equals((Opt("ntech.document.disabletemplatecache") ?? "false"), "true", StringComparison.InvariantCultureIgnoreCase);

        public static FileInfo BookKeepingReconciliationReportFormatFile => envSettings.Value.BookKeepingReconciliationReportFormatFile;

        private static NTechEnvironment E => NTechEnvironment.Instance;

        private static string Opt(string n)
        {
            return E.Setting(n, false);
        }

        private static string Req(string n)
        {
            return E.Setting(n, true);
        }
    }

    public class LegacyCreditEnvSettings : ICreditEnvSettings
    {
        public ClientConfiguration ClientCfg
        {
            get
            {
                return NTechCache.WithCache("nCredit.ClientCfg", TimeSpan.FromMinutes(15), () => ClientConfiguration.CreateUsingNTechEnvironment());
            }
        }

        public string OutgoingPaymentFilesBankName => Req("ntech.credit.outgoingpayments.bank");
        public bool IsMortgageLoansEnabled => ClientCfg.IsFeatureEnabled("ntech.feature.mortgageloans");
        public bool IsStandardMortgageLoansEnabled => IsMortgageLoansEnabled && ClientCfg.IsFeatureEnabled("ntech.feature.mortgageloans.standard");

        public bool IsUnsecuredLoansEnabled => ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans");
        public bool IsStandardUnsecuredLoansEnabled => IsUnsecuredLoansEnabled && ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans.standard");

        public bool IsCompanyLoansEnabled
        {
            get
            {
                var v = Opt("ntech.feature.companyloans");
                if (!string.IsNullOrWhiteSpace(v))
                    return v?.ToLowerInvariant() == "true";
                return ClientCfg.IsFeatureEnabled("ntech.feature.companyloans");
            }
        }

        public bool IsProduction
        {
            get
            {
                var s = Req("ntech.isproduction");
                return s.Trim().ToLower() == "true";
            }
        }

        public SignatureProviderCode EidSignatureProviderCode
        {
            get
            {
                var s = Req("ntech.eidsignatureprovider")?.Trim()?.ToLowerInvariant();
                return (SignatureProviderCode)Enum.Parse(typeof(SignatureProviderCode), s, true);
            }
        }

        public InterestModelCode ClientInterestModel => Enums.Parse<InterestModelCode>(ClientCfg.OptionalSetting("ntech.credit.interestmodel")) ?? InterestModelCode.Actual_365_25;

        public bool CreditsUse360DayInterestYear => ClientInterestModel == InterestModelCode.Actual_360;

        public bool IsDirectDebitPaymentsEnabled
        {
            get
            {
                var isEnabled = Opt("ntech.feature.directdebitpaymentsenabled");
                if (!string.IsNullOrWhiteSpace(isEnabled))
                    return isEnabled?.ToLowerInvariant() == "true";
                return ClientCfg.IsFeatureEnabled("ntech.feature.directdebitpaymentsenabled");
            }
        }

        public AutogiroSettingsModel AutogiroSettings
        {
            get
            {
                var f = NTechEnvironment.Instance.StaticResourceFile("ntech.credit.autogirosettingsfile", "autogiro-settings.txt", true);

                var settings = NTechSimpleSettings.ParseSimpleSettingsFile(f.FullName);
                return new AutogiroSettingsModel
                {
                    HmacFileSealKey = settings.Opt("hmacfileseal.key"),
                    IsHmacFileSealEnabled = settings.OptBool("hmacfileseal.enabled"),
                    CustomerNr = settings.Opt("customernr"),
                    OutgoingStatusFileExportProfileName = settings.Opt("exportprofile.outgoing.statusfile"),
                    OutgoingPaymentFileExportProfileName = settings.Opt("exportprofile.outgoing.paymentfile"),
                    BankGiroNr = !string.IsNullOrEmpty(settings.Opt("bankgironr")) ? BankGiroNumberSe.Parse(settings.Opt("bankgironr")) : null,
                    IncomingStatusFileImportFolder = settings.Opt("import.statusfile.sourcefolder")
                };
            }
        }

        public PositiveCreditRegisterSettingsModel PositiveCreditRegisterSettings
        {
            get
            {
                var f = NTechEnvironment.Instance.StaticResourceFile("ntech.credit.positivecreditregistersettingsfile", "positive-credit-register-settings.txt", true);
                var settings = NTechSimpleSettings.ParseSimpleSettingsFile(f.FullName);

                return new NTech.Core.Credit.Shared.DomainModel.PositiveCreditRegisterSettingsModel
                {
                    IsMock = settings.ReqBool("IsMock"),
                    IsLogRequestToFileEnabled = settings.OptBool("IsLogRequestToFileEnabled"),
                    IsLogResponseToFileEnabled = settings.OptBool("IsLogResponseToFileEnabled"),
                    IsLogBatchStatusToFileEnabled = settings.OptBool("IsLogBatchStatusToFileEnabled"),
                    LogFilePath = settings.Opt("LogFilePath"),
                    AddLoansEndpointUrl = settings.Req("AddLoansEndpointUrl"),
                    ChangeLoansEndpointUrl = settings.Req("ChangeLoansEndpointUrl"),
                    RepaymentsEndpointUrl = settings.Req("RepaymentsEndpointUrl"),
                    DelayedRepaymentsEndpointUrl = settings.Req("DelayedRepaymentsEndpointUrl"),
                    TerminatedLoansEndpointUrl = settings.Req("TerminatedLoansEndpointUrl"),
                    CheckBatchStatusEndpointUrl = settings.Req("CheckBatchStatusEndpointUrl"),
                    GetLoanEndpointUrl = settings.Req("GetLoanEndpointUrl"),
                    CertificateThumbPrint = settings.Req("CertificateThumbPrint"),
                    LenderMarketingName = settings.Req("LenderMarketingName"),
                    OwnerIdCode = settings.Req("OwnerIdCode"),
                    IsTargetProduction = settings.ReqBool("IsTargetProduction"),
                    ForceFirstTimeExportToTriggerLoanChanges = settings.OptBool("ForceFirstTimeExportToTriggerLoanChanges")
                };
            }
        }


        public string OutgoingPaymentFileCustomerMessagePattern => Opt("ntech.credit.outgoingpayments.customermessagepattern") ?? "{eventName} {contextNumber}";

        public IBankAccountNumber OutgoingPaymentBankAccountNr
        {
            get
            {
                string settingName;
                Func<string, IBankAccountNumber> parse;

                var country = ClientCfg.Country.BaseCountry;
                if (country == "FI")
                {
                    settingName = "ntech.credit.outgoingpaymentiban";
                    parse = x => IBANFi.Parse(x);
                }
                else if (country == "SE")
                {
                    settingName = "ntech.credit.outgoingpaymentbankaccountnr";
                    parse = x => BankAccountNumberSe.Parse(x);
                }
                else
                    throw new NotImplementedException();

                var settingValue = Opt(settingName);
                return settingValue == null ? null : parse(settingValue);
            }
        }

        public IBankAccountNumber IncomingPaymentBankAccountNr
        {
            get
            {
                string settingName;
                Func<string, IBankAccountNumber> parse;

                var country = ClientCfg.Country.BaseCountry;
                if (country == "FI")
                {
                    settingName = "ntech.credit.notificationpaymentiban";
                    parse = x => IBANFi.Parse(x);
                }
                else if (country == "SE")
                {
                    settingName = "ntech.credit.notificationpaymentbankgiroaccountnr";
                    parse = x => BankGiroNumberSe.Parse(x);
                }
                else
                    throw new NotImplementedException();

                var settingValue = Opt(settingName);
                return settingValue == null ? null : parse(settingValue);
            }
        }

        public bool OptBool(string name)
        {
            return (Opt(name) ?? "false").Trim().ToLowerInvariant() == "true";
        }

        public string Opt(string n)
        {
            return E.Setting(n, false);
        }

        public string Req(string n)
        {
            return E.Setting(n, true);
        }

        public NTechEnvironment E => NTechEnvironment.Instance;

        public bool IsTemplateCacheDisabled => string.Equals((Opt("ntech.document.disabletemplatecache") ?? "false"), "true", StringComparison.InvariantCultureIgnoreCase);

        public decimal CreditSettlementBalanceLimit
        {
            get
            {
                var v = Opt("ntech.credit.settlementbalancelimit") ?? ClientCfg.OptionalSetting("ntech.credit.settlementbalancelimit");
                if (v == null)
                {
                    if (IsStandardUnsecuredLoansEnabled)
                        throw new Exception("Not allowed when using standard unsecured loans");
                    return 30m;
                }
                if (v.Contains(".") && v.Contains(","))
                    throw new Exception("Invalid appsetting ntech.credit.settlementbalancelimit. Should be a decimal with . or , as decimal separator and no 1k separator.");
                return decimal.Parse(v.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public bool HasPerLoanDueDay => ClientCfg.IsFeatureEnabled("ntech.feature.perloandueday");
        public int? MortgageLoanInterestBindingMonths => ClientCfg.Country.BaseCountry == "SE" ? new int?(3) : new int?();
        public bool IsPromiseToPayAmortizationFreedomEnabled => NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.amortizationFreeWhilePromiseToPay");

        public int? CreditSettlementOfferGraceDays
        {
            get
            {
                var v = Opt("ntech.credit.settlementoffer.gracedays");
                if (v == null)
                    return null;
                else
                    return int.Parse(v);
            }
        }

        public Code.Fileformats.OutgoingPaymentFileFormat_SUS_SE.SwedbankSeSettings OutgoingPaymentFilesSwedbankSeSettings
        {
            get
            {
                var file = NTechSimpleSettings.ParseSimpleSettingsFile(E.StaticResourceFile("ntech.credit.outgoingpayments.swedbankse.settingsfile", "swedbank-se-settings.txt", true).FullName);

                return new SwedbankSeSettings
                {
                    FileFormat = file.Req("fileFormat"),
                    ClientOrgNr = OrganisationNumberSe.Parse(file.Req("clientOrgnr")),
                    CustomerPaymentTransactionMessage = file.Req("customerPaymentTransactionMessage"),
                    AgreementNumber = file.Req("agreementNumber"),
                    AgreementPaymentType = file.Req("agreementPaymentType")
                };
            }
        }

        public string TemplatePrintContextLogFolder => Opt("ntech.pdf.contextlogfolder");

        public int NrOfDaysUntilCreditTermsChangeOfferExpires
        {
            get
            {
                var v = Opt("ntech.credit.termschange.nrofdaysuntilofferexpires");
                if (v == null)
                {
                    return 10;
                }
                else
                    return int.Parse(v);
            }
        }

        public DirectoryInfo OutgoingCreditNotificationDeliveryFolder
        {
            get
            {
                var o = Opt("ntech.credit.notificationdeliveryfolder");
                if (o == null)
                    return null;
                return new DirectoryInfo(o);
            }
        }

        public string CreditRemindersExportProfileName => Opt("ntech.credit.reminders.exportprofile");

        public CreditType ClientCreditType
        {
            get
            {
                if (IsUnsecuredLoansEnabled)
                    return CreditType.UnsecuredLoan;

                if (IsMortgageLoansEnabled || IsStandardMortgageLoansEnabled)
                    return CreditType.MortgageLoan;

                if (IsCompanyLoansEnabled)
                    return CreditType.CompanyLoan;

                throw new NotImplementedException();
            }
        }

        public string DebtCollectionPartnerName => Opt("ntech.credit.debtcollection.partnername");
        public int LindorffFileDebtCollectionClientNumber => int.Parse(Req("ntech.credit.debtcollection.lindorfffi.clientnumber"));
        public bool ShouldRecalculateAnnuityOnInterestChange => ClientCfg.IsFeatureEnabled("ntech.feature.recalculateannuityoninterestchange");
        public Tuple<decimal?, decimal?> MinAndMaxAllowedMarginInterestRate
        {
            get
            {
                var json = GetBalanziaFiScoringMatricesJson();
                if (json != null)
                {
                    var parsed = JObject.Parse(json);
                    var minAllowed = parsed.GetDecimalPropertyValue("MinAllowedMarginInterestRate", true, true).Value;
                    var maxAllowed = parsed.GetDecimalPropertyValue("MaxAllowedMarginInterestRate", true, true).Value;
                    return Tuple.Create((decimal?)minAllowed, (decimal?)maxAllowed);
                }
                else
                    return null;
            }
        }

        public decimal? LegalInterestCeilingPercent
        {
            get
            {
                var c = Opt("ntech.legalinterestceilingpercent") ?? ClientCfg.OptionalSetting("ntech.legalinterestceilingpercent");
                if (c != null)
                    return decimal.Parse(c, CultureInfo.InvariantCulture);
                else
                    return null;
            }
        }

        private string GetBalanziaFiScoringMatricesJson()
        {
            var file = NTechEnvironment.Instance.StaticResourceFile("ntech.precredit.balanziafistandardpricingmatrices", "balanzia-fi-scoring-matrices.json", false);

            return file.Exists ? File.ReadAllText(file.FullName) : null;
        }

        public string BookKeepingRuleFileName => E.ClientResourceFile("ntech.credit.bookkeeping.rulefile", "Credit-BookkeepingRules.xml", true).FullName;
        public string SieFileEnding
        {
            get
            {
                string sieFileEnding = Opt("ntech.credit.bookkeeping.siefileending");
                if (sieFileEnding != null && sieFileEnding.StartsWith("."))
                {
                    sieFileEnding = sieFileEnding.Substring(1);
                }
                return sieFileEnding ?? "si";
            }
        }

        public DirectoryInfo AffiliatesFolder => E.ClientResourceDirectory("ntech.credit.affiliatesfolder", "Affiliates", true);

        public NTech.Core.Module.AffiliateModel GetAffiliateModel(string providerName, bool allowMissing = false)
        {
            return NTechCache.WithCache($"527332db-fb47-4dab-bd69-93088f3c9e95.{providerName}", TimeSpan.FromMinutes(15), () =>
            {
                var path = AffiliatesFolder;
                //TODO: If this model of affiliates works out, make this a table instead and provide a UI.
                var affilateFile = System.IO.Path.Combine(path.FullName, providerName + ".json");

                if (!File.Exists(affilateFile))
                {
                    if (allowMissing)
                        return null;
                    else
                        throw new Exception("Missing affiliate file: " + affilateFile);
                }

                return JsonConvert.DeserializeObject<NTech.Core.Module.AffiliateModel>(System.IO.File.ReadAllText(affilateFile));
            });
        }

        public List<NTech.Core.Module.AffiliateModel> GetAffiliateModels()
        {
            return NTechCache.WithCache(
                "8ac65e0e-4b3f-4c2c-8fef-c089af7cb4f8",
                TimeSpan.FromMinutes(5),
                () => Directory
                    .GetFiles(AffiliatesFolder.FullName, "*.json")
                    .Select(x => JsonConvert.DeserializeObject<AffiliateModel>(File.ReadAllText(x)))
                    .ToList());
        }

        public HandelsbankenSeSettings OutgoingPaymentFilesHandelsbankenSeSettings
        {
            get
            {
                var f = NTechSimpleSettings.ParseSimpleSettingsFile(E.StaticResourceFile("ntech.credit.outgoingpayments.handelsbankense.settingsfile", "handelsbanken-se-settings.txt", true).FullName);

                return new HandelsbankenSeSettings
                {
                    FileFormat = f.Req("fileFormat"),
                    BankMmbId = f.Req("bankMmbId"),
                    ClientOrgnr = OrganisationNumberSe.Parse(f.Req("clientOrgnr")),
                    SenderCompanyName = f.Req("senderCompanyName")
                };
            }
        }

        public DanskeBankFiSettings OutgoingPaymentFilesDanskeBankSettings
        {
            get
            {
                var f = E.StaticResourceFile("ntech.outgoingpayments.danskebankfi.settingsfile", "danskebank-fi-settings.txt", false);
                if (f.Exists)
                {
                    var s = NTechSimpleSettings.ParseSimpleSettingsFile(f.FullName, forceFileExistance: true);
                    return new DanskeBankFiSettings
                    {
                        FileFormat = s.Req("fileformat"),
                        SendingCompanyId = s.Req("sendingcompanyid"),
                        SendingCompanyName = s.Req("sendingcompanyname"),
                        SendingBankBic = s.Req("sendingbankbic"),
                        SendingBankName = s.Req("sendingbankname")
                    };
                }
                else
                {
                    return new DanskeBankFiSettings
                    {
                        FileFormat = Req("ntech.credit.outgoingpayments.danskebankfi.fileformat"),
                        SendingCompanyId = Req("ntech.credit.outgoingpayments.danskebankfi.sendingcompanyid"),
                        SendingCompanyName = Req("ntech.credit.outgoingpayments.danskebankfi.sendingcompanyname"),
                        SendingBankBic = Req("ntech.credit.outgoingpayments.danskebankfi.sendingbankbic"),
                        SendingBankName = Req("ntech.credit.outgoingpayments.danskebankfi.sendingbankname")
                    };
                }
            }
        }

        public EInvoiceFiSettingsFile EInvoiceFiSettingsFile => new EInvoiceFiSettingsFile(NTechSimpleSettingsCore.ParseSimpleSettingsFile(E.StaticResourceFile("ntech.einvoicefi.settingsfile", "einvoicefisettings.txt", true).FullName, forceFileExistance: true));
        public NTech.Banking.BookKeeping.NtechAccountPlanFile BookKeepingAccountPlan
        {
            get
            {
                var fileName = E.ClientResourceFile("ntech.bookkeeping.accountplanfile", "BookKeepingAccountPlan.xml", false).FullName;
                if (File.Exists(fileName))
                {
                    return NTech.Banking.BookKeeping.NtechAccountPlanFile.Parse(XDocument.Load(fileName));
                }
                return null;
            }
        }
        public FileInfo BookKeepingReconciliationReportFormatFile => E.ClientResourceFile("ntech.report.bookKeepingReconciliationReportFormatFile", "bookKeepingReconciliationReportFormat.json", false);
    }
}