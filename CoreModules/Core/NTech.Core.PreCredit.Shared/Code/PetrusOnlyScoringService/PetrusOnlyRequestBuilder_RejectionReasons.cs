using Newtonsoft.Json;
using nPreCredit;
using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.PreCredit.Shared.Code.PetrusOnlyScoringService
{
    public static partial class PetrusOnlyRequestBuilder
    {
        private static string GetMostImportantRejectionReason(ScoringDataModel s, int applicantNr)
        {
            var latestApplicationRejectionReasonsRaw = s.GetString("latestApplicationRejectionReasons", applicantNr);
            
            if (latestApplicationRejectionReasonsRaw == null)
                return null;

            List<string> latestApplicationRejectionReasons = null;
            if (latestApplicationRejectionReasonsRaw != null)
                latestApplicationRejectionReasons = JsonConvert.DeserializeObject<List<string>>(latestApplicationRejectionReasonsRaw);

            return GetMostImportantRejectionReason(latestApplicationRejectionReasons);
        }

        private static string GetMostImportantRejectionReason(List<string> rejectionReasons)
        {
            if (rejectionReasons != null && rejectionReasons.Count > 0)
                return rejectionReasons.OrderBy(GetRejectionOrdinalNr).First();
            else
                return null;
        }

        private static int GetRejectionOrdinalNr(string rejectionReason) => 
            OrderedRejectionReasons.OptS(rejectionReason) ?? OrderedRejectionReasons.Count;

        //Ordered in the sense that the first one in the list that an application was rejected by is the most important so that one will be used.
        private static Dictionary<string, int> OrderedRejectionReasons = new List<string>()
        {
            "paymentRemark",
            "score",
            "priorHistory",
            "socialStatus",
            "dbrOrLtl",
            "negativeCompanyConnection",
            "alreadyApplied",
            "paused",
            "address",
            "differentAddress",
            "sat60",
            "satLoans",
            "additionalLoan",
            "requestedVsOfferedDifference",
            "minimumDemands"
        }.Select((x, i) => new { reason = x, index = i }).ToDictionary(x => x.reason, x => x.index);
    }
}
