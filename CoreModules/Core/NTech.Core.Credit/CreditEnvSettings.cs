using nCredit;
using nCredit.Code.EInvoiceFi;
using nCredit.Code.Fileformats;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using Newtonsoft.Json;
using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.BankAccounts.Se;
using NTech.Banking.Conversion;
using NTech.Banking.OrganisationNumbers;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Infrastructure;
using System.Globalization;
using System.Xml.Linq;
using NTech.Banking.Shared.BankAccounts.Fi;

namespace NTech.Core.Credit
{
    //TODO: Share more of this with LegacyCreditEnvSettings
    public class CreditEnvSettings : ICreditEnvSettings
    {
        private readonly NEnv env;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly FewItemsCache cache;

        public CreditEnvSettings(NEnv env, IClientConfigurationCore clientConfiguration)
        {
            this.env = env;
            this.clientConfiguration = clientConfiguration;
            cache = new FewItemsCache();
        }

        private IClientConfigurationCore ClientCfg => clientConfiguration;
        private string Opt(string name) => env.OptionalSetting(name);
        private string Req(string name) => env.RequiredSetting(name);

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
                var f = env.StaticResourceFile("ntech.credit.autogirosettingsfile", "autogiro-settings.txt", true);

                var settings = NTechSimpleSettingsCore.ParseSimpleSettingsFile(f.FullName);
                return new AutogiroSettingsModel
                {
                    HmacFileSealKey = settings.Opt("hmacfileseal.key"),
                    IsHmacFileSealEnabled = settings.OptBool("hmacfileseal.enabled"),
                    CustomerNr = settings.Req("customernr"),
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
                var f = env.StaticResourceFile("ntech.credit.positivecreditregistersettingsfile", "positive-credit-register-settings.txt", true);
                var settings = NTechSimpleSettingsCore.ParseSimpleSettingsFile(f.FullName);

                return new PositiveCreditRegisterSettingsModel
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
                    ForceFirstTimeExportToTriggerLoanChanges = settings.OptBool("ForceFirstTimeExportToTriggerLoanChanges"),
                    LenderMarketingName = settings.Req("LenderMarketingName"),
                    OwnerIdCode = settings.Req("OwnerIdCode"),
                    IsTargetProduction = settings.ReqBool("IsTargetProduction"),
                    MockPcrBatchStatusFailureCode = settings.Opt("MockPcrBatchStatusFailureCode"),
                    BatchFailedReportEmail = settings.Opt("BatchFailedReportEmail"),
                    UsePcrTestCivicRegNrs = settings.OptBool("UsePcrTestCivicRegNrs"),
                    CreditNrTestSuffix = settings.Opt("CreditNrTestSuffix")
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

        public bool IsPromiseToPayAmortizationFreedomEnabled => ClientCfg.IsFeatureEnabled("ntech.feature.amortizationFreeWhilePromiseToPay");

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

        public OutgoingPaymentFileFormat_SUS_SE.SwedbankSeSettings OutgoingPaymentFilesSwedbankSeSettings
        {
            get
            {
                var file = NTechSimpleSettingsCore.ParseSimpleSettingsFile(env.StaticResourceFile("ntech.credit.outgoingpayments.swedbankse.settingsfile", "swedbank-se-settings.txt", true).FullName);

                return new OutgoingPaymentFileFormat_SUS_SE.SwedbankSeSettings
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
                    var parsed = JsonConvert.DeserializeAnonymousType(json, new { MinAllowedMarginInterestRate = (decimal?)null, MaxAllowedMarginInterestRate = (decimal?)null });
                    if (!parsed.MaxAllowedMarginInterestRate.HasValue || !parsed.MaxAllowedMarginInterestRate.HasValue)
                        throw new Exception("Missing MinAllowedMarginInterestRate or MaxAllowedMarginInterestRate");
                    return Tuple.Create(parsed.MinAllowedMarginInterestRate, parsed.MaxAllowedMarginInterestRate);
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
            var file = env.StaticResourceFile("ntech.precredit.balanziafistandardpricingmatrices", "balanzia-fi-scoring-matrices.json", false);

            return file.Exists ? File.ReadAllText(file.FullName) : null;
        }
        public string BookKeepingRuleFileName => env.ClientResourceFile("ntech.credit.bookkeeping.rulefile", "Credit-BookkeepingRules.xml", true).FullName;
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

        public DirectoryInfo AffiliatesFolder => env.ClientResourceDirectory("ntech.credit.affiliatesfolder", "Affiliates", true);
        public List<AffiliateModel> GetAffiliateModels()
        {
            return cache.WithCache(
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
                var f = NTechSimpleSettingsCore.ParseSimpleSettingsFile(env.StaticResourceFile("ntech.credit.outgoingpayments.handelsbankense.settingsfile", "handelsbanken-se-settings.txt", true).FullName);

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
                var f = env.StaticResourceFile("ntech.outgoingpayments.danskebankfi.settingsfile", "danskebank-fi-settings.txt", false);
                if (f.Exists)
                {
                    var s = NTechSimpleSettingsCore.ParseSimpleSettingsFile(f.FullName, forceFileExistance: true);
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

        public SignatureProviderCode EidSignatureProviderCode
        {
            get
            {
                var s = Req("ntech.eidsignatureprovider")?.Trim()?.ToLowerInvariant();
                return (SignatureProviderCode)Enum.Parse(typeof(SignatureProviderCode), s, true);
            }
        }

        public EInvoiceFiSettingsFile EInvoiceFiSettingsFile => new EInvoiceFiSettingsFile(NTechSimpleSettingsCore.ParseSimpleSettingsFile(env.StaticResourceFile("ntech.einvoicefi.settingsfile", "einvoicefisettings.txt", true).FullName, forceFileExistance: true));

        public NTech.Banking.BookKeeping.NtechAccountPlanFile BookKeepingAccountPlan
        {
            get
            {
                var fileName = env.ClientResourceFile("ntech.bookkeeping.accountplanfile", "BookKeepingAccountPlan.xml", false).FullName;
                if (File.Exists(fileName))
                {
                    return NTech.Banking.BookKeeping.NtechAccountPlanFile.Parse(XDocument.Load(fileName));
                }
                return null;
            }
        }

        public FileInfo BookKeepingReconciliationReportFormatFile => env.ClientResourceFile("ntech.report.bookKeepingReconciliationReportFormatFile", "bookKeepingReconciliationReportFormat.json", false);
    }
}
