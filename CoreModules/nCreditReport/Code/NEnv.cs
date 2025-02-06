using nCreditReport.Code;
using nCreditReport.Code.CreditSafeSe;
using nCreditReport.Code.TestOnly;
using Newtonsoft.Json.Linq;
using NTech.Banking.OrganisationNumbers;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace nCreditReport
{
    public static class NEnv
    {
        public static bool IsProduction => Req("ntech.isproduction") == "true";

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

        public static ClientConfiguration ClientCfg
        {
            get
            {
                return NTechCache.WithCache("nCreditReport.ClientCfg", TimeSpan.FromMinutes(15), () => ClientConfiguration.CreateUsingNTechEnvironment());
            }
        }

        public static NTechSimpleSettings UcbvSeSettings
        {
            get
            {
                return NTechSimpleSettings.ParseSimpleSettingsFile(
                    NTechEnvironment.Instance.StaticResourceFile("ntech.creditreport.ucbvse.settingsfile", "uc-bv-se-settings.txt", true).FullName,
                    forceFileExistance: true);
            }
        }

        public static bool IsMortgageLoansEnabled => ClientCfg.IsFeatureEnabled("ntech.feature.mortgageloans");

        public class EncryptionSettings
        {
            public string CurrentKeyName { get; set; }

            public IDictionary<string, string> KeysByName { get; set; }
        }

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

        public static EncryptionKeySet EncryptionKeys
        {
            get
            {
                return NTechCache.WithCache("nCreditReport.EncryptionKeys", TimeSpan.FromMinutes(15), () =>
                {
                    return NHttp
                        .Begin(new Uri(NEnv.ServiceRegistry.Internal["nUser"]), NHttp.GetCurrentAccessToken())
                        .PostJson("Encryption/KeySet", new { })
                        .ParseJsonAs<EncryptionKeySet>();
                });
            }
        }

        public static string CreditReportLogFolder => Opt("ntech.creditreport.logfolder");

        public class BisnodeFiSettings
        {
            /// <summary>
            /// balanzia
            /// </summary>
            public string CustomerCode { get; set; }

            /// <summary>
            /// BALANZIA
            /// </summary>
            public string UserId { get; set; }

            /// <summary>
            /// 100620
            /// </summary>
            public string CustomerId { get; set; }

            public string EndpointUrl { get; set; }

            public bool RequestBricVariables { get; set; }

            public bool DisableSslCheck { get; set; }
        }

        public static BisnodeFiSettings BisnodeFi =>
            new BisnodeFiSettings
            {
                CustomerCode = Req("ntech.soliditetfi.customercode"),
                CustomerId = Req("ntech.soliditetfi.customerid"),
                UserId = Req("ntech.soliditetfi.userid"),
                EndpointUrl = Req("ntech.soliditetfi.endpointurl"),
                RequestBricVariables = (Opt("ntech.soliditetfi.requestbricvariables") ?? "false").Trim() == "true",
                DisableSslCheck = (Opt("ntech.soliditetfi.disablesslcheck") ?? "false") == "true"
            };

        public class UcBusinessSeSettings : UcSeSettings
        {
            public IOrganisationNumber TestReplacementOrgnr { get; set; }
        }

        public class UcSeSettings : Code.TestOnly.ICreditReportCommonTestSettings
        {
            public string UserId { get; set; }
            public string Password { get; set; }
            public string EndpointUrl { get; set; }
            public string Template { get; set; }
            public string Product { get; set; }
            public bool SaveHtmlReplyInArchive { get; set; }
            public bool SaveXmlReplyInArchive { get; set; }
            public bool SavePdfReplyInArchive { get; set; } //Warning: These are huge. Like 300k. It might slow down the system alot to use these in production.
            public string TestModuleMode { get; set; }
        }

        public static UcSeSettings UcSe
        {
            get
            {
                var file = NTechEnvironment.Instance.StaticResourceFile("ntech.creditreport.uc.settingsfile", "uc-creditreport-settings.txt", false);
                if (!file.Exists)
                    return null;
                var f = NTechSimpleSettings.ParseSimpleSettingsFile(file.FullName, forceFileExistance: true);
                return new UcSeSettings
                {
                    UserId = f.Req("userid"),
                    Password = f.Req("password"),
                    EndpointUrl = f.Req("endpointurl"),
                    SavePdfReplyInArchive = f.OptBool("savepdfreplyinarchive"),
                    SaveHtmlReplyInArchive = f.OptBool("savehtmlreplyinarchive", defaultValue: true),
                    SaveXmlReplyInArchive = f.OptBool("savexmlreplyinarchive", defaultValue: true),
                    Template = f.Opt("template"),
                    Product = f.Opt("product"),
                    TestModuleMode = f.Opt("testmodulemode")
                };
            }
        }

        public static CreditSafeSeSettings CreditSafeSe 
        {
            get
            {
                var file = NTechEnvironment.Instance.StaticResourceFile("ntech.creditreport.creditsafese.settingsfile", "creditsafe-creditreport-settings.txt", false);
                if (!file.Exists)
                    return null;
                var f = NTechSimpleSettings.ParseSimpleSettingsFile(file.FullName, forceFileExistance: true);
                return new CreditSafeSeSettings
                {
                    UserName = f.Req("username"),
                    Password = f.Req("password"),
                    DataBlock = f.Req("datablock"),
                    Template = f.Req("template"),
                    TestModuleMode = f.Opt("testmodulemode"),
                    TestCivicRegNr = f.Opt("testoverride.civicregnr")
                };
            }
        }

        public static UcBusinessSeSettings UcBusinessSe
        {
            get
            {
                var file = NTechEnvironment.Instance.StaticResourceFile("ntech.creditreport.ucbusiness.settingsfile", "uc-business-creditreport-settings.txt", false);
                if (!file.Exists)
                    return null;
                var f = NTechSimpleSettings.ParseSimpleSettingsFile(file.FullName, forceFileExistance: true);
                return new UcBusinessSeSettings
                {
                    UserId = f.Req("userid"),
                    Password = f.Req("password"),
                    EndpointUrl = f.Req("endpointurl"),
                    SavePdfReplyInArchive = f.OptBool("savepdfreplyinarchive"),
                    SaveHtmlReplyInArchive = f.OptBool("savehtmlreplyinarchive", defaultValue: true),
                    SaveXmlReplyInArchive = f.OptBool("savexmlreplyinarchive", defaultValue: true),
                    Template = f.Opt("template"),
                    TestReplacementOrgnr = f.Opt("testreplacementorgnr") == null
                        ? null
                        : new OrganisationNumberParser(NEnv.ClientCfg.Country.BaseCountry).Parse(f.Req("testreplacementorgnr")),
                    TestModuleMode = f.Opt("testmodulemode")
                };
            }
        }

        public static NTechServiceRegistry ServiceRegistry
        {
            get
            {
                return NTechCache.WithCache(
                    "48e31939-c3ff-4d24-803c-f0ba69143f380",
                    TimeSpan.FromMinutes(5),
                    () => NTechEnvironment.Instance.ServiceRegistry);
            }
        }

        public static Tuple<string, string> AutomationUsernameAndPassword => Tuple.Create(Req("ntech.automationuser.username"), Req("ntech.automationuser.password"));

        private static string Opt(string n)
        {
            return NTechEnvironment.Instance.Setting(n, false);
        }

        private static string Req(string n)
        {
            return NTechEnvironment.Instance.Setting(n, true);
        }

        public static bool IsVerboseLoggingEnabled => (Opt("ntech.isverboseloggingenabled") ?? "false") == "true";

        public static string CreditReportTestPersonsFile => Opt("ntech.creditreport.testpersonsfile");

        public static string CreditReportTestPersonsFileFormat => Opt("ntech.creditreport.testpersonsfileformat") ?? "txt.v1";

        public static string CreditReportExchangeToCivicNr => Opt("ntech.creditreport.exchangetocivicnr");

        public static Code.SatFi.SatAccountInfo SatAccount
        {
            get
            {
                var hashKey = Opt("ntech.satfi.hashkey");
                if (hashKey == null)
                    return null;

                var clockDrag = Opt("ntech.satfi.clockdrag");

                return new Code.SatFi.SatAccountInfo
                {
                    EndpointUrl = Opt("ntech.satfi.endpointurl"),
                    HashKey = hashKey,
                    Password = Req("ntech.satfi.password"),
                    UserId = Req("ntech.satfi.userid"),
                    ClockDrag = clockDrag == null ? new TimeSpan?() : TimeSpan.Parse(clockDrag, System.Globalization.CultureInfo.InvariantCulture),
                    OverrideTarget = Opt("ntech.satfi.overridetarget")
                };
            }
        }

        /// <summary>
        /// Potentially other information for this account than the one above, so we split them up. 
        /// </summary>
        public static Code.SatFi.SatAccountInfo SatFiCreditReportAccount
        {
            get
            {
                var hashKey = Opt("ntech.satficreditreport.hashkey");
                if (hashKey == null)
                    return null;

                return new Code.SatFi.SatAccountInfo
                {
                    EndpointUrl = Opt("ntech.satficreditreport.endpointurl"),
                    HashKey = hashKey,
                    Password = Req("ntech.satficreditreport.password"),
                    UserId = Req("ntech.satficreditreport.userid")
                };
            }
        }

        /// <summary>
        /// CreditReport-Fields.json
        /// Joins two sections in the file, SharedBetweenAllProviders and 
        /// </summary>
        /// <param name="providerName"></param>
        /// <returns></returns>
        public static IEnumerable<CreditReportField> CreditReportFields(string providerName)
        {
            var file = NTechEnvironment.Instance.ClientResourceFile("ntech.creditreport.settingsoverridefile", "CreditReport-Fields.json", true);
            var parsedJson = JObject.Parse(File.ReadAllText(file.FullName));

            var sharedFieldsForAllProviders = parsedJson["SharedBetweenAllProviders"]?.ToObject<List<CreditReportField>>() ?? Enumerable.Empty<CreditReportField>();
            var providerFields = providerName != null ?
                parsedJson[providerName]?.ToObject<List<CreditReportField>>() ?? Enumerable.Empty<CreditReportField>() :
                Enumerable.Empty<CreditReportField>();

            return sharedFieldsForAllProviders.Concat(providerFields);
        }

        public static TimeSpan MaxCreditReportArchiveJobRuntime =>
            TimeSpan.FromMinutes(int.Parse(Opt("ntech.creditreport.archivejob.maxtimeinminutes") ?? "5"));

        public static int CreditReportArchiveJobInactivNrOfDaysCutoff =>
            int.Parse(Opt("ntech.creditreport.archivejob.inactivenrofdayscutoff") ?? "90");

        public static string CurrentServiceName => "nCreditReport";
    }
}