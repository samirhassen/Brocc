using System.Collections.Generic;

namespace nPreCredit.Code.StandardPolicyFilters
{
    public class RuleResult
    {
        public RuleResult(string ruleName, int? forApplicantNr, bool? isRejectedByRule,
            StaticParameterSet staticParameters)
        {
            RuleName = ruleName;
            ForApplicantNr = forApplicantNr;
            IsRejectedByRule = isRejectedByRule;
            StaticParameters = staticParameters;
        }

        public string RuleName { get; }
        public int? ForApplicantNr { get; }
        public StaticParameterSet StaticParameters { get; }
        public ScopedVariableSet GetScopedVariables(VariableSet variables) =>
            variables != null ? new ScopedVariableSet(variables, ForApplicantNr) : null;
        public bool? IsRejectedByRule { get; }
        public bool IsSkipped { get; set; }
        public bool IsMissingApplicationLevelVariable { get; set; }
        public bool IsMissingApplicantLevelVariable { get; set; }
        public string MissingVariableName { get; set; }
        public ISet<int> MissingApplicantLevelApplicantNrs { get; set; }
    }
}