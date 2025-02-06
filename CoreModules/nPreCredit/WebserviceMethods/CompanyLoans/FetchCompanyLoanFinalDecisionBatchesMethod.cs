using Newtonsoft.Json;
using nPreCredit.Code.Services.CompanyLoans;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class FetchCompanyLoanFinalDecisionBatchesMethod : TypedWebserviceMethod<FetchCompanyLoanFinalDecisionBatchesMethod.Request, FetchCompanyLoanFinalDecisionBatchesMethod.Response>
    {
        public override string Path => "CompanyLoan/FinalDecision/Fetch-Historical-Application-Batches";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var batches = requestContext.Resolver().Resolve<ICompanyLoanApplicationApprovalService>().FetchHistoricalDecisionBatches(request.FromDate.Value, request.ToDate.Value);

            return new Response
            {
                Batches = batches
            };
        }

        public class Request
        {
            [Required]
            public DateTime? FromDate { get; set; }

            [Required]
            public DateTime? ToDate { get; set; }
        }

        public class Response
        {
            public List<HistoricalCompanyLoanFinalDecisionBatchModel> Batches { get; set; }
        }
    }
}