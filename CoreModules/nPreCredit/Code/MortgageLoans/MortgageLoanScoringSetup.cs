using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.MortgageLoans
{
    public class MortgageLoanScoringSetup
    {
        public int? MaxOfferedRepaymentTimeInMonths { get; set; }
        public List<RejectionReason> RejectionReasons { get; set; }

        public class RejectionReason
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public List<string> ScoringRules { get; set; }
        }

        public Func<string, bool> GetIsKnownRejectionReason()
        {
            var r = this.RejectionReasons.Select(x => x.Name).ToHashSet();
            return x => x != null && r.Contains(x, StringComparer.OrdinalIgnoreCase);
        }

        public string GetRejectionReasonNameByScoringRuleName(string rejectionRuleName)
        {
            var rejectionReason = RejectionReasons
                .Where(x => x.ScoringRules.Contains(rejectionRuleName, StringComparer.InvariantCultureIgnoreCase))
                .Select(x => x.Name)
                .FirstOrDefault();
            if (string.IsNullOrWhiteSpace(rejectionReason))
                throw new Exception($"Missing rejection reason for scoring rule '{rejectionRuleName}'");
            return rejectionReason;
        }
    }
}