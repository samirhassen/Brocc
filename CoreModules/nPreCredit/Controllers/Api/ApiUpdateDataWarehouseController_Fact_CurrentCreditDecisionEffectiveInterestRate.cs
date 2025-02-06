using nPreCredit.Code;
using nPreCredit.DbModel;
using System;
using System.Dynamic;
using System.Linq;

namespace nPreCredit.Controllers.Api
{
    public partial class ApiUpdateDataWarehouseController
    {
        private class CurrentCreditDecisionEffectiveInterestRateModelItem : IdentityIntItem
        {
            public string ApplicationNr { get; set; }
            public string AcceptedDecisionModel { get; set; }
            public DateTimeOffset DecisionDate { get; set; }
        }

        private void Merge_Fact_CurrentCreditDecisionEffectiveInterestRate(DateTime transactionDate)
        {
            var cl = CreditApplicationTypeCode.companyLoan.ToString();
            MergeUsingIdentityInt(
                c =>
                {
                    return c
                        .CreditDecisions
                        .Where(x => !x.CreditApplication.ArchivedDate.HasValue && x.CreditApplication.ApplicationType != cl)
                        .OfType<AcceptedCreditDecision>()
                        .Where(x => c.CreditApplicationHeaders.Any(y => y.CurrentCreditDecisionId == x.Id))
                        .Select(x => new CurrentCreditDecisionEffectiveInterestRateModelItem
                        {
                            MergeId = x.Id,
                            AcceptedDecisionModel = x.AcceptedDecisionModel,
                            DecisionDate = x.DecisionDate,
                            ApplicationNr = x.ApplicationNr
                        });
                },
                SystemItemCode.DwLatestMergedTimestamp_Fact_CurrentCreditDecisionEffectiveInterestRate,
                "CurrentCreditDecisionEffectiveInterestRate",
                (items, context) =>
                {
                    return items
                        .Select(x =>
                        {
                            decimal? effectiveInterestRate = null;

                            if (x?.AcceptedDecisionModel != null)
                            {
                                effectiveInterestRate = CreditDecisionModelParser.ParseOfferEffectiveInterestRate(x.AcceptedDecisionModel, NEnv.CreditsUse360DayInterestYear);
                            }

                            var e = new ExpandoObject();
                            e.SetValues(d =>
                            {
                                d["ApplicationNr"] = x.ApplicationNr;
                                d["EffectiveInterestRate"] = effectiveInterestRate;
                                d["DecisionDate"] = x.DecisionDate.Date;
                                d["DecisionId"] = x.MergeId;
                                d["DwUpdatedDate"] = DateTime.Now; //Dont use the timemachine here even if implemented in precredit
                            });
                            return e;
                        })
                        .ToList();
                },
                true,
                500);
        }
    }
}