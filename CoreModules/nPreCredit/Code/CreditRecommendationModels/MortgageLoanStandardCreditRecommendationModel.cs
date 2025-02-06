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
    public class MortgageLoanStandardCreditRecommendationModel : StandardCreditRecommendationModelBase
    {
        public static int CurrentVersion = 2022042801;
        public static string KeyValueStoreKeySpace = "StandardMortgageLoanApplicationRecommendation";

        /// <summary>
        /// Increase this if the model changes in a way that makes to old model+ui and the new not compatible 
        /// so we can either have separate handlings of them or just choose not to render/use the old ones.
        /// </summary>
        public int Version { get; set; }
        public LoanStandardLtlResult LeftToLiveOn { get; set; }
        public decimal? LoanToIncome { get; set; }
        public string LoanToIncomeMissingReason { get; set; }
        public decimal? LoanToValue { get; set; }
        public string LoanToValueMissingReason { get; set; }

        public static MortgageLoanStandardCreditRecommendationModel Create(
            EngineResult policyFilterResult,
            LoanStandardLtlResult leftToLiveOn,
            decimal? loanToIncome, string loanToIncomeMissingReason,
            decimal? loanToValue, string loanToValueMissingReason,
            string clientCountry, string displayLanguage)
        {
            return new MortgageLoanStandardCreditRecommendationModel
            {
                Version = CurrentVersion,
                LoanToIncome = loanToIncome,
                LoanToIncomeMissingReason = loanToIncomeMissingReason,
                LoanToValue = loanToValue,
                LoanToValueMissingReason = loanToValueMissingReason,
                PolicyFilterResult = policyFilterResult,
                PolicyFilterDetailsDisplayItems = CreateRuleDisplayItems(policyFilterResult, clientCountry, displayLanguage),
                LeftToLiveOn = leftToLiveOn
            };
        }

        public static MortgageLoanStandardCreditRecommendationModel ParseJson(string rawJson)
        {
            return JsonConvert.DeserializeObject<MortgageLoanStandardCreditRecommendationModel>(rawJson);
        }
    }
}