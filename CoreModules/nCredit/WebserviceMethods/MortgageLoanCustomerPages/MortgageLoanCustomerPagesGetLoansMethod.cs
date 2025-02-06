using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.WebserviceMethods.MortgageLoanCustomerPages
{
    public class MortgageLoanCustomerPagesGetLoansMethod : MortgageLoanCustomerPagesMethod<MortgageLoanCustomerPagesGetLoansMethod.Request, MortgageLoanCustomerPagesGetLoansMethod.Response>
    {
        protected override string MethodName => "loans";

        protected override Response DoCustomerLockedExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request, int customerPagesUserCustomerId)
        {
            using (var context = new CreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                var loans = Controllers.ApiCustomerPagesController.GetCustomerFacingCreditModels(context, customerPagesUserCustomerId)
                    .OrderByDescending(x => x.StartDate)
                    .ToList()
                    .Select(x => new Response.LoanModel
                    {
                        LoanNr = x.CreditNr,
                        Status = x.Status,
                        StatusDate = x.StatusDate,
                        StartDate = x.StartDate.DateTime,
                        CurrentCapitalDebtAmount = x.CurrentCapitalDebtAmount,
                        CurrentTotalInterestRatePercent = x.CurrentTotalInterestRatePercent,
                        MonthlyAmortizationAmount = x.MonthlyAmortizationAmount
                    })
                    .ToList();

                return new Response
                {
                    Loans = loans
                };
            }
        }

        public class Request : MortgageLoanCustomerPagesRequestBase
        {

        }
        public class Response
        {
            public List<LoanModel> Loans { get; set; }

            public class LoanModel
            {
                public string LoanNr { get; set; }
                public DateTime StartDate { get; set; }
                public string Status { get; set; }
                public DateTime? StatusDate { get; set; }
                public decimal CurrentCapitalDebtAmount { get; set; }
                public decimal? CurrentTotalInterestRatePercent { get; set; }
                public decimal? MonthlyAmortizationAmount { get; set; }
            }
        }
    }
}