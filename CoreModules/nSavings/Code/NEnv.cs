using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using nSavings.Code.Riksgalden;
using nSavings.Code.Trapets;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.Shared.BankAccounts.Fi;
using NTech.Core.Module;
using NTech.Core.Module.Shared;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Savings.Shared;
using NTech.Core.Savings.Shared.Services.Utilities;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;

namespace nSavings.Code
{
    public static class NEnv
    {
        public static string CurrentServiceName => "nSavings";

        public static string NTechCdnUrl => Opt("ntech.cdn.rooturl");

        public static bool IsBundlingEnabled
        {
            get
            {
                var s = Opt("ntech.isbundlingenabled") ?? "true";
                return s.Trim().ToLower() == "true";
            }
        }

        public static string BookKeepingRuleFileName =>
            E.ClientResourceFile("ntech.savings.bookkeeping.rulefile", "Savings-BookkeepingRules.xml", true)
                .FullName;

        public static bool IsProduction
        {
            get
            {
                var s = Req("ntech.isproduction");
                return s.Trim().ToLower() == "true";
            }
        }

        public static ClientConfiguration ClientCfg
        {
            get
            {
                return NTechCache.WithCache("nSavings.ClientCfg", TimeSpan.FromMinutes(15),
                    () => ClientConfiguration.CreateUsingNTechEnvironment());
            }
        }

        public static CivicRegNumberParser BaseCivicRegNumberParser
        {
            get
            {
                return NTechCache.WithCache("BaseCivicRegNumberParser", TimeSpan.FromMinutes(5),
                    () => new CivicRegNumberParser(ClientCfg.Country.BaseCountry));
            }
        }


        public static OcrNumberParser BaseOcrNumberParser
        {
            get
            {
                return NTechCache.WithCache("BaseOcrNumberParser", TimeSpan.FromMinutes(5),
                    () => new OcrNumberParser(ClientCfg.Country.BaseCountry));
            }
        }

        public static DirectoryInfo SkinningRootFolder =>
            E.ClientResourceDirectory("ntech.skinning.rootfolder", "Skinning", false);

        public static FileInfo SkinningCssFile => E.ClientResourceFile("ntech.skinning.cssfile",
            Path.Combine(SkinningRootFolder.FullName, "css\\skinning.css"), false);

        public static bool IsSkinningEnabled => NTechCache.WithCacheS($"ntech.cache.skinningenabled",
            TimeSpan.FromMinutes(5), () => SkinningRootFolder?.Exists ?? false);

        public static bool IsSkinningCssEnabled => NTechCache.WithCacheS($"ntech.cache.skinningcssenabled",
            TimeSpan.FromMinutes(5), () => SkinningCssFile?.Exists ?? false);

        public static string OutgoingPaymentFilesBankName => Req("ntech.savings.outgoingpayments.bank");

        public static DirectoryInfo LogFolder
        {
            get
            {
                var v = Opt("ntech.logfolder");
                return v == null ? null : new DirectoryInfo(v);
            }
        }

        public static string BookkeepingFileExportProfileName => Opt("ntech.savings.bookkeepingfile.exportprofile");

        public static string FatcaFileExportProfileName => Opt("ntech.savings.factaexportfile.exportprofile");

        public static string OutgoingPaymentFileCustomerMessagePattern =>
            Opt("ntech.savings.outgoingpayments.customermessagepattern");

        public static DanskeBankFiSettings OutgoingPaymentFilesDanskeBankSettings =>
            new DanskeBankFiSettings
            {
                FileFormat = Req("ntech.savings.outgoingpayments.danskebankfi.fileformat"),
                SendingCompanyId = Req("ntech.savings.outgoingpayments.danskebankfi.sendingcompanyid"),
                SendingCompanyName = Req("ntech.savings.outgoingpayments.danskebankfi.sendingcompanyname"),
                SendingBankBic = Req("ntech.savings.outgoingpayments.danskebankfi.sendingbankbic"),
                SendingBankName = Req("ntech.savings.outgoingpayments.danskebankfi.sendingbankname")
            };

        public class DanskeBankFiSettings
        {
            public string FileFormat { get; set; }
            public string SendingCompanyId { get; set; }
            public string SendingCompanyName { get; set; }
            public string SendingBankBic { get; set; }
            public string SendingBankName { get; set; }
        }

        public static Tuple<string, string> ApplicationAutomationUsernameAndPassword =>
            Tuple.Create(Req("ntech.automationuser.username"), Req("ntech.automationuser.password"));

        public static IBANFi DepositsIban => IBANFi.Parse(Req("ntech.savings.depositsiban"));

        public static IBANFi OutgoingPaymentIban => IBANFi.Parse(Req("ntech.savings.outgoingpaymentiban"));

        private static Lazy<IBANToBICTranslator> iBANToBICTranslatorInstance =
            new Lazy<IBANToBICTranslator>(() => new IBANToBICTranslator());

        public static IBANToBICTranslator IBANToBICTranslatorInstance => iBANToBICTranslatorInstance.Value;

        public static NTechServiceRegistry ServiceRegistry
        {
            get
            {
                return NTechCache.WithCache(
                    "cfa425cf-3b7e-4c13-829a-c858e885d9790",
                    TimeSpan.FromMinutes(5),
                    () => NTechEnvironment.Instance.ServiceRegistry);
            }
        }

        public static DirectoryInfo PdfTemplateFolder =>
            E.ClientResourceDirectory("ntech.pdf.templatefolder", "PdfTemplates", true);

        public static string AddressProviderName => Req("ntech.savings.addressprovider");

        public static bool IsVerboseLoggingEnabled => (Opt("ntech.isverboseloggingenabled") ?? "false") == "true";

        public static bool IsTemplateCacheDisabled =>
            string.Equals(Opt("ntech.savings.disabletemplatecache") ?? "false", "true",
                StringComparison.InvariantCultureIgnoreCase);

        public static FileInfo GetOptionalExcelTemplateFilePath(string filename)
        {
            var d = E.ClientResourceDirectory("ntech.excel.templatefolder", "ExcelTemplates", false);
            return d == null ? null : new FileInfo(Path.Combine(d.FullName, filename));
        }

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

        public static string TestingOverrideDateFile =>
            IsProduction ? null : Opt("ntech.savings.testing.overridedatefile");

        public static string SieFileEnding
        {
            get
            {
                var sieFileEnding = Opt("ntech.savings.bookkeeping.siefileending");
                if (sieFileEnding != null && sieFileEnding.StartsWith("."))
                {
                    sieFileEnding = sieFileEnding.Substring(1);
                }

                return sieFileEnding ?? "si";
            }
        }

        public static string TrapetsAmlExportProfileName => Opt("ntech.trapetsamlreporting.savingsexportprofile");

        public static decimal MaxAllowedSavingsCustomerBalance
        {
            get
            {
                var v = Opt("ntech.savings.maxallowedsavingscustomerbalance");
                if (v != null)
                    return decimal.Parse(v, CultureInfo.InvariantCulture);
                if (ClientCfg.Country.BaseCurrency == "EUR")
                    return 100000m;
                throw new NotImplementedException();
            }
        }

        public static int IncomingPaymentDepositeGracePeriodIndays => Convert.ToInt32(Req("ntech.savings.incomingpayments.depositsgraceperiodindays"));

            
        public static string LedgerReportStandardAccountProductName =>
            ClientCfg.OptionalSetting("ntech.savings.ledgerreport.standardaccountproductname");

        public static TrapetsKycConfiguration TrapetsAmlConfig =>
            TrapetsKycConfiguration.FromXElement(
                ClientCfg.GetCustomSection("SavingsTrapetsAmlConfiguration"));

        public static RiksgaldenConfiguration RiksgaldenConfig => RiksgaldenConfiguration.Create(ClientCfg);

        public static NTechSimpleSettingsCore FinnishCustomsAccountsSettings =>
            NTechSimpleSettingsCore.ParseSimpleSettingsFile(
                E.StaticResourceFile("ntech.savings.finnishcustomsaccounts.settingsfile",
                    "finnishCustomsAccountsSettings.txt", true).FullName);

        private static NTechEnvironment E => NTechEnvironment.Instance;

        public static NTechServiceRegistry ServiceRegistryNormal
        {
            get
            {
                return NTechCache.WithCache(
                    "138819bd-fe35-4b25-b104-801150e2dcf601",
                    TimeSpan.FromMinutes(5),
                    () => NTechEnvironment.Instance.ServiceRegistry);
            }
        }

        public static ISavingsEnvSettings EnvSettings => SavingsEnvSettings.Instance;

        public static IClientConfigurationCore ClientCfgCore =>
            NTechCache.WithCache("nSavings.ClientCfgCore", TimeSpan.FromMinutes(15),
                () => ClientConfigurationCoreFactory.CreateUsingNTechEnvironment(E));

        private static string Opt(string n)
        {
            return E.Setting(n, false);
        }

        private static string Req(string n)
        {
            return E.Setting(n, true);
        }

        private static T? OptT<T>(string n, Func<string, T> parse) where T : struct
        {
            var v = Opt(n);
            return v == null ? new T?() : parse(v);
        }
    }

    public class SavingsEnvSettings : ISharedEnvSettings, ISavingsEnvSettings
    {
        private SavingsEnvSettings()
        {
        }

        public static ISavingsEnvSettings Instance { get; private set; } = new SavingsEnvSettings();

        public bool IsProduction => NEnv.IsProduction;

        public bool IsTemplateCacheDisabled => NEnv.IsTemplateCacheDisabled;

        public decimal MaxAllowedSavingsCustomerBalance => NEnv.MaxAllowedSavingsCustomerBalance;
        public string OutgoingPaymentFileCustomerMessagePattern => NEnv.OutgoingPaymentFileCustomerMessagePattern;
        public IBANFi OutgoingPaymentIban => NEnv.OutgoingPaymentIban;

        public int IncomingPaymentDepositeGracePeriodIndays => NEnv.IncomingPaymentDepositeGracePeriodIndays;
    }
}