namespace nPreCredit.Code.StandardPolicyFilters
{
    public class EvaluateRuleContext
    {
        public EvaluateRuleContext(ScopedVariableSet variables, StaticParameterSet staticParameters)
        {
            StaticParameters = staticParameters;
            Variables = variables;
        }

        public StaticParameterSet StaticParameters { get; }
        public ScopedVariableSet Variables { get; }
    }
}