using NTech.Core.PreCredit.Shared;
using System;
using System.Collections.Generic;

namespace nPreCredit.Code
{
    public static class CreditCheckCompletionProviderApplicationUpdater
    {
        public static void AddRejectionReasonPauseDayItems(List<string> rejectionReasons, Func<string, int?> getPauseDaysByRejectionReason, ISet<int> customerIds, IPreCreditContextExtended context, CreditDecision decision)
        {
            if (rejectionReasons == null || rejectionReasons.Count == 0) return;

            foreach (var r in rejectionReasons)
            {
                var pauseDays = getPauseDaysByRejectionReason(r);
                if (pauseDays.HasValue && pauseDays.Value > 0)
                {
                    foreach (var customerId in customerIds)
                    {
                        context.AddCreditDecisionPauseItems(context.FillInfrastructureFields(new CreditDecisionPauseItem
                        {
                            Decision = decision,
                            CustomerId = customerId,
                            PausedUntilDate = decision.DecisionDate.Date.AddDays(pauseDays.Value),
                            RejectionReasonName = r
                        }));
                    }
                }
            }
        }

        public static void AddRejectionReasonSearchTerms(List<string> rejectionReasons, Func<string, bool> isKnownRejectionReason, CreditDecision decision, IPreCreditContextExtended context)
        {
            if (rejectionReasons == null || rejectionReasons.Count == 0) return;

            bool hasOther = false;
            foreach (var r in rejectionReasons)
            {
                if (isKnownRejectionReason(r))
                {
                    context.AddCreditDecisionSearchTerms(new CreditDecisionSearchTerm
                    {
                        Decision = decision,
                        TermName = CreditDecisionSearchTerm.CreditDecisionSearchTermCode.RejectionReason.ToString(),
                        TermValue = r
                    });
                }
                else
                    hasOther = true;
            }
            if (hasOther)
            {
                context.AddCreditDecisionSearchTerms(new CreditDecisionSearchTerm
                {
                    Decision = decision,
                    TermName = CreditDecisionSearchTerm.CreditDecisionSearchTermCode.RejectionReason.ToString(),
                    TermValue = "other"
                });
            }
        }
    }
}