using NTech.Banking.ScoringEngine;
using System.Collections.Generic;

namespace BalanziaSe.Scoring
{
    public class ExternalCreditReportCompanyKeyNumbersRule : MinimumDemandScoringRule
    {
        protected override RuleContext.MimumDemandsResultCode CheckMinimumDemand(RuleContext context)
        {
            if (context.RequireString("creditReportSummaEgetKapital") == "missing" || context.RequireString("creditReportSoliditetProcent") == "missing")
                return RuleContext.MimumDemandsResultCode.Rejected;

            if (context.RequireDecimal("creditReportSummaEgetKapital", null) < 0)
                return RuleContext.MimumDemandsResultCode.Rejected;

            if (context.RequireDecimal("creditReportSoliditetProcent", null) < 0)
                return RuleContext.MimumDemandsResultCode.Rejected;

            return RuleContext.MimumDemandsResultCode.Accepted;
        }
        
        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet();
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("creditReportSummaEgetKapital", "creditReportSoliditetProcent");
        }
    }
}