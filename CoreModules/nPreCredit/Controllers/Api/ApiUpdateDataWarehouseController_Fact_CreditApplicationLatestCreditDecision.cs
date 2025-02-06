using nPreCredit.Code;
using nPreCredit.DbModel;
using System;
using System.Linq;

namespace nPreCredit.Controllers.Api
{

    public partial class ApiUpdateDataWarehouseController
    {
        private void Merge_Fact_CreditApplicationLatestCreditDecision(DateTime transactionDate)
        {
            const string FactName = "CreditApplicationLatestCreditDecision";
            MergeUsingIdentityInt(x => CreditApplicationLatestCreditDecisionQuery(x),
                SystemItemCode.DwLatestMergedTimestamp_Fact_CreditApplicationLatestCreditDecision2,
                FactName,
                (items, context) =>
                    items.Select(x =>
                    {
                        decimal? offeredAmount = null;
                        if (x.IsAccepted)
                        {
                            var ad = (AcceptedCreditDecision)x.AcceptedDecision;
                            var nd = CreditDecisionModelParser.ParseAcceptedNewCreditOffer(ad.AcceptedDecisionModel);
                            if (nd != null)
                                offeredAmount = nd.amount;
                            else
                                offeredAmount = CreditDecisionModelParser.ParseAcceptedAdditionalLoanOffer(ad.AcceptedDecisionModel)?.amount;
                        }
                        return new
                        {
                            x.ApplicationNr,
                            DecisionDate = x.DecisionDate.DateTime.Date,
                            x.IsAccepted,
                            OfferedAmount = offeredAmount,
                            x.WasAutomated,
                            DwUpdatedDate = DateTime.Now
                        };
                    }).ToList(), true, 1000);
        }

        private class CreditApplicationLatestCreditDecisionModel : IdentityIntItem
        {
            public string ApplicationNr { get; set; }
            public DateTimeOffset DecisionDate { get; set; }
            public bool IsAccepted { get; set; }
            public bool WasAutomated { get; set; }
            public CreditDecision AcceptedDecision { get; set; }
        }

        private IQueryable<CreditApplicationLatestCreditDecisionModel> CreditApplicationLatestCreditDecisionQuery(PreCreditContext context)
        {
            var cl = CreditApplicationTypeCode.companyLoan.ToString();
            var activeDecisions = context.CreditDecisions.Where(x => !x.CreditApplication.ArchivedDate.HasValue && x.CreditApplication.ApplicationType != cl);

            var q = activeDecisions
                .Where(x => x.CreditApplication.CurrentCreditDecisionId == x.Id)
                .AsQueryable();

            return q.Select(x => new CreditApplicationLatestCreditDecisionModel
            {
                ApplicationNr = x.ApplicationNr,
                DecisionDate = x.DecisionDate,
                WasAutomated = x.WasAutomated,
                IsAccepted = (x is AcceptedCreditDecision),
                AcceptedDecision = (x is AcceptedCreditDecision) ? x : null,
                MergeId = x.Id
            });
        }
    }
}