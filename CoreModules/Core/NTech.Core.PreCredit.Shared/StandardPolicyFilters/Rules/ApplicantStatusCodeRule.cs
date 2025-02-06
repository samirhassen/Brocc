namespace nPreCredit.Code.StandardPolicyFilters.Rules
{
    public class ApplicantStatusCodeRule : Rule
    {
        public override string Name => "ApplicantStatusCode";

        public override bool IsEvaluatedPerApplicant => true;

        public override string[] RequestedApplicationLevelVaribles => CreateVariables();

        public override string[] RequestedApplicantLevelVaribles => CreateVariables("applicantStatusCode");

        public override StaticRuleParameter[] StaticParameters => CreateParameters();

        //NOTE: Didnt use != here since this is intended to be read by somewhat math inclined people like risk 
        //who are not programmers so things like = < > are likely familiar but "!=" is probably not.
        public override string GetDescription(string country, string language) => "not(v:applicantStatusCode = normal)";

        public override string GetDisplayName(string country, string language) => "Applicant status code";

        protected override bool? IsRejectedByRule(EvaluateRuleContext context) =>
            context.Variables.GetString("applicantStatusCode", true) != "normal";
    }
}