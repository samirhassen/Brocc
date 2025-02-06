using Newtonsoft.Json;
using NTech.Banking.ScoringEngine;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace nPreCredit.Code
{
    public class DoCompleteCreditCheckLegacyOtherApplicationsModel
    {
        public IDictionary<int, IList<HistoricalApplication>> OtherApplicationsByApplicantNr { get; set; }

        public string ToJson()
        {
            var e = (IDictionary<string, object>)new ExpandoObject();
            foreach (var applicantNr in OtherApplicationsByApplicantNr.Keys.OrderBy(x => x))
            {
                e[$"applicant{applicantNr}"] = OtherApplicationsByApplicantNr[applicantNr];
            }
            return JsonConvert.SerializeObject(e);
        }
    }
}