using NTech.Banking.ScoringEngine;
using System.Collections.Generic;

namespace nPreCredit.Code.Scoring.BalanziaScoringRules
{
    public class ActivePendingApplicationRule : MinimumDemandScoringRule
    {
        protected override RuleContext.MimumDemandsResultCode CheckMinimumDemand(RuleContext context)
        {
            return context.RejectIfForAnyApplicant(applicantNr =>
                context.RequireInt("activeApplicationCount", applicantNr) > 0 &&
                context.RequireInt("maxActiveApplicationAgeInDays", applicantNr) <= 3);
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet("activeApplicationCount", "maxActiveApplicationAgeInDays");
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("nrOfApplicants");
        }
    }
}