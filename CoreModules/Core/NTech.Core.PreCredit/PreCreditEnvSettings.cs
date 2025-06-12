using System.Xml.Linq;
using Newtonsoft.Json;
using nPreCredit;
using nPreCredit.Code;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Core.PreCredit
{
    public class PreCreditEnvSettings : IPreCreditEnvSettings
    {
        private readonly NEnv env;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly FewItemsCache cache;

        public PreCreditEnvSettings(NEnv env, IClientConfigurationCore clientConfiguration)
        {
            this.env = env;
            this.clientConfiguration = clientConfiguration;
            cache = new FewItemsCache();
        }

        private IClientConfigurationCore ClientCfg => clientConfiguration;
        private string Opt(string name) => env.OptionalSetting(name);
        private string Req(string name) => env.RequiredSetting(name);

        public int CreditApplicationWorkListIsNewMinutes =>
            int.Parse(Opt("ntech.precredit.worklist.isnewminutes") ?? "3");

        public string DefaultScoringVersion => Opt("ntech.precredit.scoringversion") ??
                                               ClientCfg.OptionalSetting("ntech.precredit.scoringversion");

        public bool IsMortgageLoansEnabled => ClientCfg.IsFeatureEnabled("ntech.feature.mortgageloans");

        public bool IsStandardMortgageLoansEnabled => IsMortgageLoansEnabled &&
                                                      ClientCfg.IsFeatureEnabled(
                                                          "ntech.feature.mortgageloans.standard");

        public bool IsOnlyNonStandardMortgageLoansEnabled => IsMortgageLoansEnabled && !IsStandardMortgageLoansEnabled;

        public bool IsUnsecuredLoansEnabled => ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans");

        public bool IsStandardUnsecuredLoansEnabled => IsUnsecuredLoansEnabled &&
                                                       ClientCfg.IsFeatureEnabled(
                                                           "ntech.feature.unsecuredloans.standard");

        public bool IsCompanyLoansEnabled
        {
            get
            {
                var v = Opt("ntech.feature.companyloans");
                if (!string.IsNullOrWhiteSpace(v))
                    return v.ToLowerInvariant() == "true";
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

        private AffiliateModel GetAffiliateModelNonCached(string providerName, bool allowMissing)
        {
            var path = AffiliatesFolder;
            var affilateFile = Path.Combine(path.FullName, providerName + ".json");

            if (File.Exists(affilateFile))
                return JsonConvert.DeserializeObject<AffiliateModel>(File.ReadAllText(affilateFile));
            if (allowMissing)
                return null;
            throw new Exception("Missing affiliate file: " + affilateFile);
        }

        public DirectoryInfo AffiliatesFolder =>
            env.ClientResourceDirectory("ntech.credit.affiliatesfolder", "Affiliates", true);

        public AffiliateModel GetAffiliateModel(string providerName, bool allowMissing = false)
        {
            return cache.WithCache($"527332db-fb47-4dab-bd69-93088f3c9e95.{providerName}", TimeSpan.FromMinutes(15),
                () => new
                {
                    Affilate = GetAffiliateModelNonCached(providerName, allowMissing: allowMissing)
                })?.Affilate;
        }

        public List<AffiliateModel> GetAffiliateModels()
        {
            return cache.WithCache(
                "842a117c-99e9-4afd-95e5-454e573ec9ce",
                TimeSpan.FromMinutes(5),
                () => Directory
                    .GetFiles(AffiliatesFolder.FullName, "*.json")
                    .Select(x => JsonConvert.DeserializeObject<AffiliateModel>(File.ReadAllText(x)))
                    .ToList());
        }

        public bool OptBool(string name)
        {
            return (Opt(name) ?? "false").Trim().ToLowerInvariant() == "true";
        }

        public bool IsTemplateCacheDisabled => string.Equals((Opt("ntech.document.disabletemplatecache") ?? "false"),
            "true", StringComparison.InvariantCultureIgnoreCase);

        public string CreditReportProviderName => Opt("ntech.credit.creditreportprovider") ?? "bisnodefi";

        /// <summary>
        /// Which creditreportproviders that we should list creditreports from in the system. 
        /// Comma-separated string, or will return the creditreportprovider if not set. 
        /// </summary>
        public string[] ListCreditReportProviders =>
            Opt("ntech.precredit.listcreditreportproviders")?.Split(',').Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x)).ToArray()
            ?? new[] { CreditReportProviderName };

        public bool CreditsUse360DayInterestYear =>
            (ClientCfg.OptionalSetting("ntech.credit.interestmodel") ?? "") == "Actual_360";

        public string CurrentServiceName => "nPreCredit";

        public CampaignCodeSettingsModel CampaignCodeSettings =>
            new()
            {
                DisableForceManualControl =
                    (Opt("ntech.campaigncode.forcemanualcontrol.disable") ?? "").Trim().ToLowerInvariant() ==
                    "true",
                DisableRemoveInitialFee =
                    (Opt("ntech.campaigncode.removeinitialfee.disable") ?? "").Trim().ToLowerInvariant() == "true",
            };

        public ScoringSetupModel ScoringSetup
        {
            get
            {
                return cache.WithCache("b57cb07c-c0fe-4a22-9502-5107fab8a5d3", TimeSpan.FromMinutes(15), () =>
                {
                    var f = env.ClientResourceFile("ntech.precredit.scoringsetupfile", "PreCredit-ScoringSetup.xml",
                        true);

                    return ScoringSetupModel.Parse(
                        XDocuments.Load(f.FullName));
                });
            }
        }

        public int PersonCreditReportReuseDays => int.Parse(Opt("ntech.precredit.personcreditreportreusedays") ?? "1");

        public int CompanyCreditReportReuseDays =>
            int.Parse(Opt("ntech.precredit.companycreditreportreusedays") ?? "7");

        public string AdditionalQuestionsUrlPattern =>
            //Something like: http://localhost:32730/additional-questions?id={token}
            Req("ntech.credit.additionalquestions.urlpattern");

        public string ApplicationWrapperUrlPattern => Opt("ntech.credit.applicationwrapper.urlpattern");

        public bool ShowDemoMessages =>
            (Opt("ntech.precredit.showdemomessages") ?? "false").ToLowerInvariant() == "true";

        public SignatureProviderCode? SignatureProvider
        {
            get
            {
                var s = Opt("ntech.eidsignatureprovider")?.Trim().ToLowerInvariant();
                if (s == null)
                    return null;
                return (SignatureProviderCode)Enum.Parse(typeof(SignatureProviderCode), s, true);
            }
        }

        public bool IsAdditionalLoanScoringRuleDisabled =>
            OptBool("ntech.precredit.isAdditionalLoanScoringRuleDisabled");

        public bool IsCoApplicantScoringRuleDisabled => OptBool("ntech.precredit.isCoApplicantScoringRuleDisabled");
        public bool IsTranslationCacheDisabled => OptBool("ntech.precredit.disabletranslationcache");

        public DirectoryInfo LogFolder
        {
            get
            {
                var v = Opt("ntech.logfolder");
                return v == null ? null : new DirectoryInfo(v);
            }
        }

        public List<string> DisabledScoringRuleNames
        {
            get
            {
                var v = Opt("ntech.precredit.disabledScoringRuleNames");
                return v == null ? new List<string>() : v.Replace(" ", "").Replace(";", ",").Split(',').ToList();
            }
        }
    }
}