using nPreCredit.Code;
using nPreCredit.DbModel;
using System;
using System.Linq;

namespace nPreCredit.Controllers.Api
{
    public partial class ApiUpdateDataWarehouseController
    {
        private void Merge_Fact_CreditApplicationFinalDecision(DateTime transactionDate)
        {
            const string FactName = "CreditApplicationFinalDecision";
            MergeFact(
                x => CreditApplicationFinalDecisionFactQuery(x),
                SystemItemCode.DwLatestMergedTimestamp_Fact_CreditApplicationFinalDecision,
                FactName,
                (items, context) =>
                    items.Select(x =>
                    {
                        var newLoan = CreditDecisionModelParser.ParseAcceptedNewCreditOffer(x.AcceptedDecisionModel);
                        var additionalLoan = CreditDecisionModelParser.ParseAcceptedAdditionalLoanOffer(x.AcceptedDecisionModel);
                        return new
                        {
                            ApplicationNr = x.ApplicationNr,
                            FinalDecisionDate = x.FinalDecisionDate.Value.Date.Date,
                            IsAdditionalLoan = additionalLoan != null,
                            Amount = additionalLoan != null ? additionalLoan.amount.Value : newLoan.amount,
                            CreditNr = additionalLoan != null ? additionalLoan.creditNr : x.CreditNr,
                            DwUpdatedDate = DateTime.Now
                        };
                    }).ToList()
                , 300);
        }

        private class CreditApplicationFinalDecisionFactModel : TimestampedItem
        {
            public string ApplicationNr { get; set; }
            public DateTimeOffset? FinalDecisionDate { get; set; }
            public string AcceptedDecisionModel { get; set; }
            public string CreditNr { get; set; }
        }

        private IQueryable<CreditApplicationFinalDecisionFactModel> CreditApplicationFinalDecisionFactQuery(PreCreditContext context)
        {
            var cl = CreditApplicationTypeCode.companyLoan.ToString();

            var q = context
                .CreditApplicationHeaders
                .Where(x => !x.ArchivedDate.HasValue && x.FinalDecisionDate.HasValue && x.IsFinalDecisionMade && x.ApplicationType != cl);

            return q.Select(x => new CreditApplicationFinalDecisionFactModel
            {
                ApplicationNr = x.ApplicationNr,
                FinalDecisionDate = x.FinalDecisionDate,
                AcceptedDecisionModel = x.CreditDecisions.OfType<AcceptedCreditDecision>().Where(y => y.Id == x.CurrentCreditDecisionId).FirstOrDefault().AcceptedDecisionModel,
                CreditNr = x.Items.Where(y => y.GroupName == "application" && y.Name == "creditnr").Select(y => y.Value).FirstOrDefault(),
                Timestamp = x.Timestamp
            });
        }
    }
}