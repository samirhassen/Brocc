using Newtonsoft.Json;
using NTech.Banking.ScoringEngine;
using NTech.Core.Module.Shared.Clients;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace nPreCredit.Code
{
    public class DoCompleteCreditCheckLegacyCreditHistoryModel
    {
        public Dictionary<int, List<HistoricalCreditExtended>> CustomerCreditHistoryItemsByApplicantNr;

        public string ToJson()
        {
            var e = (IDictionary<string, object>)new ExpandoObject();
            foreach (var applicantNr in CustomerCreditHistoryItemsByApplicantNr.Keys.OrderBy(x => x))
            {
                e[$"applicant{applicantNr}"] = CustomerCreditHistoryItemsByApplicantNr[applicantNr];
            }
            return JsonConvert.SerializeObject(e);
        }
    }
}