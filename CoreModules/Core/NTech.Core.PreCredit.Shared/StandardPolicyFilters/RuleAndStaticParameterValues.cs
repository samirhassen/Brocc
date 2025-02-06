namespace nPreCredit.Code.StandardPolicyFilters
{
    public class RuleAndStaticParameterValues
    {
        public RuleAndStaticParameterValues(string ruleName, StaticParameterSet staticParameterValues, string rejectionReasonName)
        {
            RuleName = ruleName;
            StaticParameterValues = staticParameterValues;
            RejectionReasonName = rejectionReasonName;
        }
        public string RuleName { get; set; }
        public StaticParameterSet StaticParameterValues { get; set; }
        public string RejectionReasonName { get; }
    }
}