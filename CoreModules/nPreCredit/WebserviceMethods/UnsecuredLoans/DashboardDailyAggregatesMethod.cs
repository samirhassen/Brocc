using Newtonsoft.Json;
using nPreCredit.Code;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods
{
    public class DashboardDailyAggregatesMethod : TypedWebserviceMethod<DashboardDailyAggregatesMethod.Request, DashboardDailyAggregatesMethod.Response>
    {
        public override string Path => "Dashboard/Daily-Aggregate-Data";

        public override bool IsEnabled => !NEnv.IsMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            using (var context = new PreCreditContext())
            {
                var forDate = request.ForDate.Value.Date;
                var dayAfterForDate = forDate.AddDays(1);
                var decisions = context
                    .CreditApplicationHeaders
                    .Where(x => x.IsPartiallyApproved && x.PartiallyApprovedDate >= forDate && x.PartiallyApprovedDate < dayAfterForDate)
                    .Select(x => x.CurrentCreditDecision)
                    .ToList();

                var totalAmount = 0m;

                foreach (var decision in decisions)
                {
                    var model = (decision as AcceptedCreditDecision)?.AcceptedDecisionModel;
                    totalAmount += GetOfferedAmountFromCreditDecision(model).GetValueOrDefault();
                }

                return new Response
                {
                    ApprovedAmount = totalAmount
                };
            }
        }

        private decimal? GetOfferedAmountFromCreditDecision(string decisionModel)
        {
            var newCreditOffer = CreditDecisionModelParser.ParseAcceptedNewCreditOffer(decisionModel);
            if (newCreditOffer != null)
                return newCreditOffer.amount;

            var additionalLoanoffer = CreditDecisionModelParser.ParseAcceptedAdditionalLoanOffer(decisionModel);
            if (additionalLoanoffer != null)
                return additionalLoanoffer.amount;

            return null;
        }

        public class Request
        {
            [Required]
            public DateTime? ForDate { get; set; }
        }

        public class Response
        {
            public decimal ApprovedAmount { get; set; }
        }
    }
}