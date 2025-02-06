using nPreCredit.Code.Services;
using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;

namespace nPreCredit.Code.Scoring.BalanziaScoringRules
{
    public class RandomlyRejectedRule : MinimumDemandScoringRule
    {
        protected override RuleContext.MimumDemandsResultCode CheckMinimumDemand(RuleContext context) =>
            context.RequireInt("randomNr", null) < context.RequireInt("randomNrRejectBelowLimit", null) ? RuleContext.MimumDemandsResultCode.Rejected : RuleContext.MimumDemandsResultCode.Accepted;
        protected override ISet<string> DeclareRequiredApplicantItems() => ToSet("");
        protected override ISet<string> DeclareRequiredApplicationItems() => ToSet("randomNr", "randomNrRejectBelowLimit");
    }

    public interface IRandomNrScoringVariableProvider
    {
        int GenerateRandomNrBetweenOneAndOneHundred(string applicationNr);
        int GetRejectBelowLimit();
    }

    public class RandomNrScoringVariableProvider : IRandomNrScoringVariableProvider
    {
        public const int RejectHalfCutOff = 51; //1 -> 50 rejected, 51 -> 100 accepted so 50 of each
        public RandomNrScoringVariableProvider(IPreCreditContextFactoryService contextFactoryService, int rejectBelowLimit)
        {
            this.contextFactoryService = contextFactoryService;
            this.rejectBelowLimit = rejectBelowLimit;
        }

        private static Lazy<Random> random = new Lazy<Random>(() => new Random());
        private static object lockObject = new object();
        private readonly IPreCreditContextFactoryService contextFactoryService;
        private readonly int rejectBelowLimit;

        public int GenerateRandomNrBetweenOneAndOneHundred(string applicationNr)
        {
            //We cache the random nr per application so redoing the scoring doesn't keep randomly cycling accept/reject.
            const string keySpace = "scoringRandomNr";
            string randomNr;
            using(var context = contextFactoryService.CreateExtended())
            {
                randomNr = KeyValueStoreService.GetValueComposable(context, applicationNr, keySpace);
                if (randomNr == null)
                {
                    lock (lockObject)
                    {
                        randomNr = random.Value.Next(1, 101).ToString();
                    }
                    KeyValueStoreService.SetValueComposable(context, applicationNr, keySpace, randomNr);
                    context.SaveChanges();
                }
                return int.Parse(randomNr);
            }
        }

        public int GetRejectBelowLimit() => rejectBelowLimit;
    }
}