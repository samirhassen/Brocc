using Newtonsoft.Json;
using nPreCredit.Code.Services.CompanyLoans;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class FetchCompanyLoanFinalDecisionBatchItemsMethod : TypedWebserviceMethod<FetchCompanyLoanFinalDecisionBatchItemsMethod.Request, FetchCompanyLoanFinalDecisionBatchItemsMethod.Response>
    {
        public override string Path => "CompanyLoan/FinalDecision/Fetch-Historical-Application-Batch-Items";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var items = requestContext.Resolver().Resolve<ICompanyLoanApplicationApprovalService>().FetchHistoricalDecisionBatchItems(request.BatchId.Value);

            return new Response
            {
                BatchId = request.BatchId.Value,
                Items = items
            };
        }

        public class Request
        {
            [Required]
            public int? BatchId { get; set; }
        }

        public class Response
        {
            public List<HistoricalCompanyLoanFinalDecisionBatchItemModel> Items { get; set; }
            public int BatchId { get; set; }
        }
    }
}