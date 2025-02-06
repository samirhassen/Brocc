using nCredit.Code;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCredit.WebserviceMethods.MortgageLoanCustomerPages
{
    public class MortgageLoanCustomerPagesGetLoanDetailsMethod : MortgageLoanCustomerPagesMethod<MortgageLoanCustomerPagesGetLoanDetailsMethod.Request, MortgageLoanCustomerPagesGetLoanDetailsMethod.Response>
    {
        protected override string MethodName => "loan-details";

        protected override Response DoCustomerLockedExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request, int customerPagesUserCustomerId)
        {
            using (var context = new CreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                var loan = Controllers.ApiCustomerPagesController.GetCustomerFacingCreditModels(context, customerPagesUserCustomerId)
                    .Where(x => x.CreditNr == request.LoanNr)
                    .SingleOrDefault();
                if (loan == null)
                    return Error("No such loan", httpStatusCode: 400, errorCode: "noSuchLoan");

                var transactions = Controllers.ApiCustomerPagesController.GetCapitalTransactionsOrderedByEvent(context, customerPagesUserCustomerId, request.LoanNr, null, null);

                var sharedDocumentTypes = new List<string> { "ProxyAuthorization", "InitialAgreement" };
                var creditLevelDocuments = context
                    .Documents
                    .Where(x => x.CreditNr == request.LoanNr && x.Credit.CreditCustomers.Any(y => y.CustomerId == customerPagesUserCustomerId) && sharedDocumentTypes.Contains(x.DocumentType))
                    .Select(x => new
                    {
                        CustomerApplicantNr = x.Credit.CreditCustomers.Where(y => y.CustomerId == customerPagesUserCustomerId).Select(y => y.ApplicantNr).FirstOrDefault(),
                        DocumentApplicantNr = x.ApplicantNr,
                        x.CreditNr,
                        DocumentType = x.DocumentType,
                        DocumentDate = x.ChangedDate,
                        x.Id
                    })
                    .Where(x => (!x.DocumentApplicantNr.HasValue || x.DocumentApplicantNr == x.CustomerApplicantNr))
                    .ToList()
                    .GroupBy(x => x.DocumentType)
                    .Select(x => x.OrderByDescending(y => y.Id).First())
                    .Select(x => new Response.DocumentModel
                    {
                        DocumentId = x.Id.ToString(),
                        DocumentDate = x.DocumentDate.DateTime,
                        DocumentType = x.DocumentType
                    })
                    .OrderByDescending(x => x.DocumentDate)
                    .ThenByDescending(x => x.DocumentType)
                    .ToList();

                var response = new Response
                {
                    Details = new Response.DetailsModel
                    {
                        CurrentCapitalDebtAmount = loan.CurrentCapitalDebtAmount,
                        MonthlyAmortizationAmount = loan.MonthlyAmortizationAmount,
                        CurrentTotalInterestRatePercent = loan.CurrentTotalInterestRatePercent,
                        LoanNr = loan.CreditNr,
                        StartDate = loan.StartDate.DateTime,
                        Status = loan.Status,
                        StatusDate = loan.StatusDate
                    },
                    Transactions = transactions?.Select(x => new Response.TransactionModel
                    {
                        Amount = x.Amount,
                        BalanceAfterAmount = x.BalanceAfterAmount,
                        BusinessEventType = x.BusinessEventType,
                        CreatedByEventId = x.CreatedByEventId,
                        Id = x.Id,
                        TransactionDate = x.TransactionDate
                    })?.ToList(),
                    Documents = creditLevelDocuments
                };

                if (!request.ExcludeCoApplicantDetails)
                {
                    var coApplicants = new List<Response.CoApplicantModel>();
                    var cc = new CreditCustomerClient();
                    var customerIds = loan.CustomerIds.Where(x => x != customerPagesUserCustomerId).ToHashSet();
                    var d = cc.BulkFetchPropertiesByCustomerIdsD(customerIds, "firstName", "lastName");
                    foreach (var customerId in customerIds)
                    {
                        coApplicants.Add(new Response.CoApplicantModel
                        {
                            CustomerId = customerId,
                            FirstName = d.Opt(customerId)?.Opt("firstName"),
                            LastName = d.Opt(customerId)?.Opt("lastName"),
                        });
                    }

                    response.CoApplicants = coApplicants;
                }

                return response;
            }
        }

        public class Request : MortgageLoanCustomerPagesRequestBase
        {
            [Required]
            public string LoanNr { get; set; }
            public bool ExcludeCoApplicantDetails { get; set; }
        }

        public class Response
        {
            public DetailsModel Details { get; set; }
            public List<TransactionModel> Transactions { get; set; }
            public List<CoApplicantModel> CoApplicants { get; set; }
            public List<DocumentModel> Documents { get; set; }

            public class DocumentModel
            {
                public string DocumentType { get; set; }
                public string DocumentId { get; set; }
                public DateTime DocumentDate { get; set; }
            }

            public class DetailsModel
            {
                public string LoanNr { get; set; }
                public DateTime StartDate { get; set; }
                public string Status { get; set; }
                public DateTime? StatusDate { get; set; }
                public decimal CurrentCapitalDebtAmount { get; set; }
                public decimal CurrentTotalInterestRatePercent { get; set; }
                public decimal? MonthlyAmortizationAmount { get; set; }
            }

            public class TransactionModel
            {
                public long Id { get; set; }
                public int CreatedByEventId { get; set; }
                public string BusinessEventType { get; set; }
                public DateTime? TransactionDate { get; set; }
                public decimal? Amount { get; set; }
                public decimal? BalanceAfterAmount { get; set; }
            }

            public class CoApplicantModel
            {
                public int CustomerId { get; set; }
                public string FirstName { get; set; }
                public string LastName { get; set; }
            }
        }
    }
}