using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring
{
    public class ExternalKFMRiskRule : MinimumDemandScoringRule
    {
        protected override RuleContext.MimumDemandsResultCode CheckMinimumDemand(RuleContext context)
        {
            var nrOfRemarks = 0;
            if(context.RequireString("creditReportAntalAnmarkningar") != "missing") 
            {
                //This whole section will be left out of the creditreport sometimes which we choose to interpret as zero
                nrOfRemarks = context.RequireInt("creditReportAntalAnmarkningar", null);
            }

            var isBadUcRisk = true; //letter or <= 3
            if(int.TryParse(context.RequireString("creditReportRiskklassForetag"), out var ucRisk))
            {
                isBadUcRisk = ucRisk <= 3;
            }

            return nrOfRemarks > 0 && isBadUcRisk ? RuleContext.MimumDemandsResultCode.AcceptedWithManualAttention : RuleContext.MimumDemandsResultCode.Accepted;
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet();
        }

        //Note that the business rule claims to talk about KFM balance but all the actual samples we got use remarks as the source
        //so we mirror that. This feels really sketchy though as there is a separate section with explicit KFM balance.
        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("creditReportAntalAnmarkningar", "creditReportRiskklassForetag");
        }
    }
}
