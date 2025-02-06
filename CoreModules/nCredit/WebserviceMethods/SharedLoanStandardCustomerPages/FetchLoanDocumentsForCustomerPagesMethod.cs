using Newtonsoft.Json;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCredit.WebserviceMethods.SharedLoanStandardCustomerPages
{
    public class FetchLoanDocumentsForCustomerPagesMethod : TypedWebserviceMethod<FetchLoanDocumentsForCustomerPagesMethod.Request, FetchLoanDocumentsForCustomerPagesMethod.Response>
    {
        public override string Path => "LoanStandard/CustomerPages/Fetch-Documents";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled || NEnv.IsStandardMortgageLoansEnabled;

        private const string NonCreditDocumentPrefix = "Other_";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            using (var context = new CreditContext())
            {
                var customerId = request.CustomerId;

                var documentTypes = NEnv.IsStandardMortgageLoansEnabled
                   ? new string[] { "InitialAgreement", "DirectDebitConsent", "TerminationLetter", "MortgageLoanChangeTermsAgreement" }
                   : new string[] { "InitialAgreement", "DirectDebitConsent", "TerminationLetter" };
                var credits = context
                    .CreditHeaders
                    .Where(x => x.CreditCustomers.Any(y => y.CustomerId == customerId))
                    .Select(x => new
                    {
                        x.CreditNr,
                        ApplicantNr = x.CreditCustomers.Where(y => y.CustomerId == customerId).Select(y => y.ApplicantNr).FirstOrDefault(),
                        Documents = x.Documents.Where(y => documentTypes.Contains(y.DocumentType)).Select(y => new
                        {
                            y.ChangedDate,
                            y.ArchiveKey,
                            y.DocumentType,
                            y.CustomerId,
                            y.ApplicantNr
                        }),
                        AnnualStatements = x.AnnualStatements.Where(y => y.CustomerId == customerId).Select(y => new
                        {
                            y.ChangedDate,
                            y.Year,
                            y.StatementDocumentArchiveKey
                        })
                    })
                    .ToList();

                var documents = new List<(DateTimeOffset SortDate, Response.Document Document)>();

                foreach (var credit in credits)
                {
                    foreach (var document in credit.Documents)
                    {
                        if (document.ApplicantNr.HasValue && document.ApplicantNr.Value != credit.ApplicantNr)
                            continue;

                        if (document.CustomerId.HasValue && document.CustomerId.Value != customerId)
                            continue;

                        documents.Add((SortDate: document.ChangedDate, Document: new Response.Document
                        {
                            DocumentArchiveKey = document.ArchiveKey,
                            CreditNr = credit.CreditNr,
                            DocumentContext = null,
                            DocumentTypeCode = document.DocumentType,
                            DocumentDate = document.ChangedDate.Date
                        }));
                    }
                    foreach (var document in credit.AnnualStatements)
                    {
                        documents.Add((SortDate: document.ChangedDate, Document: new Response.Document
                        {
                            DocumentArchiveKey = document.StatementDocumentArchiveKey,
                            CreditNr = credit.CreditNr,
                            DocumentContext = document.Year.ToString(),
                            DocumentTypeCode = $"{NonCreditDocumentPrefix}AnnualStatement",
                            DocumentDate = document.ChangedDate.Date
                        }));
                    }
                }

                return new Response
                {
                    Documents = documents.OrderByDescending(x => x.SortDate).Select(x => x.Document).ToList()
                };
            }
        }

        public class Request
        {
            [Required]
            public int? CustomerId { get; set; }
        }

        public class Response
        {
            public List<Document> Documents { get; set; }
            public class Document
            {
                public DateTime DocumentDate { get; set; }
                public string CreditNr { get; set; }
                public string DocumentTypeCode { get; set; }
                public string DocumentContext { get; set; }
                public string DocumentArchiveKey { get; set; }
            }
        }
    }
}