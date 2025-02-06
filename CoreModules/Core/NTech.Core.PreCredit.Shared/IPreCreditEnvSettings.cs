using nPreCredit.Code;
using NTech.Core.Module;
using NTech.Core.Module.Shared;
using System.Collections.Generic;
using System.IO;

namespace nPreCredit
{
    public interface IPreCreditEnvSettings : ISharedEnvSettings
    {
        string DefaultScoringVersion { get; }
        int CreditApplicationWorkListIsNewMinutes { get; }
        bool IsMortgageLoansEnabled { get; }
        bool IsStandardMortgageLoansEnabled { get; }
        bool IsOnlyNonStandardMortgageLoansEnabled { get; }
        bool IsUnsecuredLoansEnabled { get; }
        bool IsStandardUnsecuredLoansEnabled { get; }
        bool IsCompanyLoansEnabled { get; }
        DirectoryInfo AffiliatesFolder { get; }
        AffiliateModel GetAffiliateModel(string providerName, bool allowMissing = false);
        List<AffiliateModel> GetAffiliateModels();

        string CreditReportProviderName { get; }

        /// <summary>
        /// Which creditreportproviders that we should list creditreports from in the system. 
        /// Comma-separated string, or will return the creditreportprovider if not set. 
        /// </summary>
        string[] ListCreditReportProviders { get; }
        bool CreditsUse360DayInterestYear { get; }
        string CurrentServiceName { get; }
        CampaignCodeSettingsModel CampaignCodeSettings { get; }
        ScoringSetupModel ScoringSetup { get; }
        int PersonCreditReportReuseDays { get; }
        int CompanyCreditReportReuseDays { get; }
        string AdditionalQuestionsUrlPattern { get; }
        string ApplicationWrapperUrlPattern { get; }
        bool ShowDemoMessages { get; }
        SignatureProviderCode? SignatureProvider { get; }
        bool IsAdditionalLoanScoringRuleDisabled { get; }
        bool IsCoApplicantScoringRuleDisabled { get; }
        bool IsTranslationCacheDisabled { get; }
        DirectoryInfo LogFolder { get; }
        List<string> DisabledScoringRuleNames { get; }
    }

    public class CampaignCodeSettingsModel
    {
        public bool DisableRemoveInitialFee { get; set; }
        public bool DisableForceManualControl { get; set; }
    }
}
