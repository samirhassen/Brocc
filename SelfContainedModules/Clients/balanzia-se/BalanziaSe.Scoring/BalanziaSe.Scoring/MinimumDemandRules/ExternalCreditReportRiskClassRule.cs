using NTech.Banking.ScoringEngine;
using System.Collections.Generic;

namespace BalanziaSe.Scoring
{
    public class ExternalCreditReportRiskClassRule : MinimumDemandScoringRule
    {
        /*
         Uc riskklass företag (W11005) can be:
    Sifferklass = 1-5
    Bokstavsriskklass =
    A = Avregistrerat
    B = Ej bestämd
    C = Kommission
    F = Fusion
    I = Inaktivt
    K = Konkurs
    L = Likvidation
    N = Nummerbolag
    O = Obestånd
    U = Utmätning
    Ö = Överfört
    X =  Ej klassad
             */
        protected override RuleContext.MimumDemandsResultCode CheckMinimumDemand(RuleContext context)
        {
            var rc = (context.RequireString("creditReportRiskklassForetag", null));
            if (!int.TryParse(rc, out var rcInt))
                return RuleContext.MimumDemandsResultCode.Rejected;

            return rcInt > 1 ? RuleContext.MimumDemandsResultCode.Accepted : RuleContext.MimumDemandsResultCode.Rejected;
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet();
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("creditReportRiskklassForetag");
        }
    }
}