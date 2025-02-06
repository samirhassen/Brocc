using Newtonsoft.Json;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCredit.WebserviceMethods.SharedLoanStandardCustomerPages
{
    public class FetchLoanCapitalTransactionsForCustomerPagesMethod : TypedWebserviceMethod<FetchLoanCapitalTransactionsForCustomerPagesMethod.Request, FetchLoanCapitalTransactionsForCustomerPagesMethod.Response>
    {
        public override string Path => "LoanStandard/CustomerPages/Fetch-Capital-Transactions";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled || NEnv.IsStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            using (var context = new CreditContext())
            {
                var customerId = request.CustomerId;
                var capitalTransactions = context
                    .CreditHeaders
                    .Where(x => x.CreditNr == request.CreditNr && x.CreditCustomers.Any(y => y.CustomerId == customerId))
                    .SelectMany(x => x.Transactions.Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString()))
                    .OrderByDescending(x => x.Id)
                    .Select(x => new Response.CapitalTransaction
                    {
                        TransactionDate = x.TransactionDate,
                        Amount = x.Amount,
                        BusinessEventType = x.BusinessEvent.EventType,
                        BusinessEventRoleCode = x.BusinessEventRoleCode,
                        SubAccountCode = x.SubAccountCode
                    })
                    .ToList();

                var totalAmount = 0m;
                foreach (var t in capitalTransactions.AsEnumerable().Reverse())
                {
                    totalAmount += t.Amount;
                    t.TotalAmountAfter = totalAmount;
                }

                return new Response
                {
                    Transactions = capitalTransactions
                };
            }
        }

        public class Request
        {
            [Required]
            public int? CustomerId { get; set; }

            [Required]
            public string CreditNr { get; set; }
        }

        public class Response
        {
            public List<CapitalTransaction> Transactions { get; set; }
            public class CapitalTransaction
            {
                public DateTime TransactionDate { get; set; }
                public decimal Amount { get; set; }
                public string BusinessEventType { get; set; }
                public string BusinessEventRoleCode { get; set; }
                public string SubAccountCode { get; set; }
                public decimal TotalAmountAfter { get; set; }
            }
        }
    }
}