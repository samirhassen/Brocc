using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring
{
    public class InitialActiveLoanRule : MinimumDemandScoringRule
    {
        protected override RuleContext.MimumDemandsResultCode CheckMinimumDemand(RuleContext context)
        {
            return context.RequireInt("nrOfActiveLoans", null) > 0 
                ? RuleContext.MimumDemandsResultCode.Rejected 
                : RuleContext.MimumDemandsResultCode.Accepted;
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet();
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("nrOfActiveLoans");
        }
    }
}
