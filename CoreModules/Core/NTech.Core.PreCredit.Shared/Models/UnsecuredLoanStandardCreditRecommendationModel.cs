using Newtonsoft.Json;
using nPreCredit.Code.Services.SharedStandard;
using nPreCredit.Code.StandardPolicyFilters;

namespace nPreCredit.Code
{
    /// <summary>
    /// This is seralized and stored in KeyValueItem under the KeySpace CreditRecommendationDetails
    /// 
    /// The random string is then referenced from the CreditDecisionItem recommendationKey so it can be used also from view details
    /// </summary>
    public class UnsecuredLoanStandardCreditRecommendationModel : StandardCreditRecommendationModelBase
    {
        public static int CurrentVersion = 2021092901;
        public static string KeyValueStoreKeySpace = "StandardUnsecuredLoanApplicationRecommendation";
        /// <summary>
        /// Increase this if the model changes in a way that makes to old model+ui and the new not compatible 
        /// so we can either have separate handlings of them or just choose not to render/use the old ones.
        /// </summary>
        public int Version { get; set; }

        public LoanStandardLtlResult LeftToLiveOn { get; set; }

        public decimal? DebtBurdenRatio { get; set; }
        public string DebtBurdenRatioMissingReasonMessage { get; set; }

        public decimal? ProbabilityOfDefaultPercent { get; set; }
        public string ProbabilityOfDefaultMissingReasonMessage { get; set; }

        public static UnsecuredLoanStandardCreditRecommendationModel Create(
            LoanStandardLtlResult leftToLiveOn,
            decimal? debtBurdenRatio, string debtBurdenRatioMissingReasonMessage, EngineResult policyFilterResult,
            decimal? probabilityOfDefaultPercent, string probabilityOfDefaultMissingReasonMessage,
            string clientCountry, string displayLanguage)
        {
            return new UnsecuredLoanStandardCreditRecommendationModel
            {
                Version = CurrentVersion,
                LeftToLiveOn = leftToLiveOn,
                DebtBurdenRatio = debtBurdenRatio,
                DebtBurdenRatioMissingReasonMessage = debtBurdenRatioMissingReasonMessage,
                PolicyFilterResult = policyFilterResult,
                PolicyFilterDetailsDisplayItems = CreateRuleDisplayItems(policyFilterResult, clientCountry, displayLanguage),
                ProbabilityOfDefaultPercent = probabilityOfDefaultPercent,
                ProbabilityOfDefaultMissingReasonMessage = probabilityOfDefaultMissingReasonMessage
            };
        }

        public static UnsecuredLoanStandardCreditRecommendationModel ParseJson(string rawJson)
        {
            return JsonConvert.DeserializeObject<UnsecuredLoanStandardCreditRecommendationModel>(rawJson);
        }
    }
}