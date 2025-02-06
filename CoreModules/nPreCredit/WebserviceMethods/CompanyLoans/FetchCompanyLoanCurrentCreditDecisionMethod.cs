using Newtonsoft.Json;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class FetchCompanyLoanCurrentCreditDecisionMethod : TypedWebserviceMethod<FetchCompanyLoanCurrentCreditDecisionMethod.Request, FetchCompanyLoanCurrentCreditDecisionMethod.Response>
    {
        public override string Path => "CompanyLoan/Fetch-Current-CreditDecision";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            return GetDecisionShared(request.ApplicationNr);
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
        }

        public class Response
        {
            public Code.Services.CompanyLoans.CompanyLoanCreditDecisionModel Decision { get; set; }
            public int? DecisionId { get; set; }
            public DateTime? DecisionDate { get; set; }
            public int? DecisionByUserId { get; set; }
        }

        //TODO: Move to service
        public static Response GetDecisionShared(string applicationNr)
        {
            using (var context = new PreCreditContext())
            {
                var h = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == applicationNr && x.ApplicationType == CreditApplicationTypeCode.companyLoan.ToString())
                    .Select(x => new
                    {
                        x.ApplicationNr,
                        x.CurrentCreditDecision
                    })
                    .SingleOrDefault();

                if (h == null)
                    throw new NTechWebserviceMethodException("No such application")
                    {
                        ErrorCode = "noSuchApplication",
                        IsUserFacing = true
                    };

                if (h.CurrentCreditDecision == null)
                    throw new NTechWebserviceMethodException("No currenct credit decision")
                    {
                        ErrorCode = "noCurrentCreditDecision",
                        IsUserFacing = true
                    };

                var d = h.CurrentCreditDecision;
                var decisionModel = ((d as AcceptedCreditDecision)?.AcceptedDecisionModel) ?? ((d as RejectedCreditDecision)?.RejectedDecisionModel);
                if (string.IsNullOrWhiteSpace(decisionModel))
                    throw new NTechWebserviceMethodException("No currenct credit decision")
                    {
                        ErrorCode = "noCurrentCreditDecision2",
                        IsUserFacing = true
                    };

                var pm = Code.CreditDecisionModelParser.ParseCompanyLoanCreditDecision(decisionModel);

                return new Response
                {
                    Decision = pm,
                    DecisionByUserId = h.CurrentCreditDecision.DecisionById,
                    DecisionDate = h.CurrentCreditDecision.DecisionDate.DateTime,
                    DecisionId = h.CurrentCreditDecision.Id
                };
            }
        }
    }
}