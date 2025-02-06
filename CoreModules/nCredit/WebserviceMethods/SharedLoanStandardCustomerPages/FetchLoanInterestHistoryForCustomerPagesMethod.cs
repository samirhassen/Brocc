using Newtonsoft.Json;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCredit.WebserviceMethods.SharedLoanStandardCustomerPages
{
    public class FetchLoanInterestHistoryForCustomerPagesMethod : TypedWebserviceMethod<FetchLoanInterestHistoryForCustomerPagesMethod.Request, FetchLoanInterestHistoryForCustomerPagesMethod.Response>
    {
        public override string Path => "LoanStandard/CustomerPages/Fetch-Interest-History";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled || NEnv.IsStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            using (var context = new CreditContext())
            {
                var customerId = request.CustomerId;
                var rateNames = new string[]
                {
                    DatedCreditValueCode.MarginInterestRate.ToString(),
                    DatedCreditValueCode.ReferenceInterestRate.ToString(),
                };
                var interestChanges = context
                    .CreditHeaders
                    .Where(x => x.CreditNr == request.CreditNr && x.CreditCustomers.Any(y => y.CustomerId == customerId))
                    .SelectMany(x => x
                        .DatedCreditValues
                        .Where(y => rateNames.Contains(y.Name))
                        .Select(y => new { y.Id, y.TransactionDate, y.Value, y.Name }))
                    .ToList();

                decimal GetRateForDate(DateTime date, DatedCreditValueCode rateCode) => interestChanges
                        .Where(x => x.Name == rateCode.ToString() && x.TransactionDate <= date)
                        .OrderByDescending(x => x.TransactionDate)
                        .OrderByDescending(x => x.Id)
                        .FirstOrDefault()?.Value ?? 0m;

                var interestChangeDates = interestChanges.Select(x => x.TransactionDate).Distinct().OrderByDescending(x => x).ToList();

                return new Response
                {
                    InterestChanges = interestChangeDates.Select(x => new Response.InterestChange
                    {
                        TransactionDate = x,
                        InterestRatePercent = GetRateForDate(x, DatedCreditValueCode.MarginInterestRate) + GetRateForDate(x, DatedCreditValueCode.ReferenceInterestRate)
                    }).ToList()
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
            public List<InterestChange> InterestChanges { get; set; }
            public class InterestChange
            {
                public DateTime TransactionDate { get; set; }
                public decimal? InterestRatePercent { get; set; }
            }
        }
    }
}