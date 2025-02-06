using System;
using System.Collections.Generic;
using System.IO;

namespace nCustomerPages.Code
{
    public class CustomerLockedCreditClient : AbstractSystemUserServiceClient
    {
        protected override string ServiceName => "nCredit";
        private int customerId;

        public CustomerLockedCreditClient(int customerId)
        {
            this.customerId = customerId;
        }

        public bool HasOrHasEverHadACredit()
        {
            return Begin()
                .PostJson("Api/CustomerPages/HasOrHasEverHadACredit", new
                {
                    customerId
                })
                .ParseJsonAsAnonymousType(new
                {
                    hasOrHasEverHadACredit = (bool?)null,
                })
                .hasOrHasEverHadACredit.Value;
        }

        public bool HasActiveCredit()
        {
            return Begin()
                .PostJson("Api/CustomerPages/HasOrHasEverHadACredit", new
                {
                    customerId
                })
                .ParseJsonAsAnonymousType(new
                {
                    hasActiveCredit = (bool?)null,
                })
                .hasActiveCredit.Value;
        }

        public class CreditCapitalTransactionCustomerPagesModel
        {
            public long Id { get; set; }
            public int CreatedByEventId { get; set; }
            public string BusinessEventType { get; set; }
            public DateTime? TransactionDate { get; set; }
            public decimal? Amount { get; set; }
            public decimal? BalanceAfterAmount { get; set; }
        }

        public class CreditDetailsResult
        {
            public class CreditDetails
            {
                public string CreditNr { get; set; }
                public DateTimeOffset StartDate { get; set; }
                public string Status { get; set; }
                public DateTime? StatusDate { get; set; }
                public decimal CurrentCapitalDebtAmount { get; set; }
                public decimal CurrentTotalInterestRatePercent { get; set; }
                public decimal MonthlyPaymentAmount { get; set; }
                public int? RepaymentTimeInMonths { get; set; }
            }
            public CreditDetails Details { get; set; }
            public IList<CreditCapitalTransactionCustomerPagesModel> Transactions { get; set; }
        }

        public CreditDetailsResult GetCreditDetails(string creditNr, int? maxTransactionsCount, int? startBeforeTransactionId)
        {
            return Begin()
                .PostJson("Api/CustomerPages/CreditDetails", new { customerId, creditNr, maxTransactionsCount, startBeforeTransactionId })
                .ParseJsonAs<CreditDetailsResult>();
        }

        public IList<CreditCapitalTransactionCustomerPagesModel> GetCreditTransactions(string creditNr, int? maxTransactionsCount, int? startBeforeTransactionId)
        {
            return Begin()
                .PostJson("Api/CustomerPages/EventOrderedCreditTransactions", new { customerId, creditNr, maxTransactionsCount, startBeforeTransactionId })
                .ParseJsonAsAnonymousType(new { Transactions = (IList<CreditCapitalTransactionCustomerPagesModel>)null })
                ?.Transactions;
        }

        public GetCreditsResult GetCredits()
        {
            return Begin()
                .PostJson("Api/CustomerPages/Credits", new
                {
                    customerId
                })
                .ParseJsonAs<GetCreditsResult>();
        }

        public class GetCreditsResult
        {
            public class Credit
            {
                public string CreditNr { get; set; }
                public DateTimeOffset StartDate { get; set; }
                public string Status { get; set; }
                public DateTime? StatusDate { get; set; }
                public decimal CurrentCapitalDebtAmount { get; set; }
                public decimal CurrentTotalInterestRatePercent { get; set; }
                public decimal MonthlyPaymentAmount { get; set; }
            }
            public IList<Credit> Credits { get; set; }
        }

        public class OpenNotificationsResult
        {
            public class Notification
            {
                public int Id { get; set; }
                public DateTime DueDate { get; set; }
                public bool IsOverdue { get; set; }
                public string CreditNr { get; set; }
                public decimal TotalUnpaidNotifiedAmount { get; set; }
                public string OcrPaymentReference { get; set; }
                public string PaymentIban { get; set; }
                public IList<Document> Documents { get; set; }
                public decimal InitialNotifiedAmount { get; set; }
                public DateTime? LatestPaymentDate { get; set; }
                public bool IsOpen { get; set; }

            }
            public class Document
            {
                public string DocumentType { get; set; }
                public string DocumentId { get; set; }
            }

            public IList<Notification> Notifications { get; set; }
        }

        public OpenNotificationsResult GetOpenNotifications()
        {
            return Begin()
                .PostJson("Api/CustomerPages/OpenNotifications", new { customerId = customerId })
                .ParseJsonAs<OpenNotificationsResult>();
        }

        public bool TryFetchCreditDocument(string documentType, string documentId, out string contentType, out string fileName, out byte[] fileBytes)
        {
            using (var ms = new MemoryStream())
            {
                var r = Begin().Get($"Api/CustomerPages/CreditDocument?documentType={documentType}&documentId={documentId}&customerId={customerId}");

                if (r.IsNotFoundStatusCode)
                {
                    fileName = null;
                    fileBytes = null;
                    contentType = null;
                    return false;
                }

                r.DownloadFile(ms, out contentType, out fileName);

                fileBytes = ms.ToArray();

                return true;
            }
        }

        public CreditsAccountDocumentsResult GetCreditsAccountDocuments()
        {
            return Begin()
                .PostJson("Api/CustomerPages/GetDocuments", new { customerId })
                .ParseJsonAs<CreditsAccountDocumentsResult>();
        }

        public byte[] GetAmortizationPlanPdf(string creditNr)
        {
            using (var ms = new MemoryStream())
            {
                var r = Begin().Get($"Api/Credit/AmortizationPlanPdfWithCustomerCheck?creditNr={creditNr}&customerId={customerId}");

                r.DownloadFile(ms, out _, out _);

                return ms.ToArray();
            }
        }


        public byte[] GetAmortizationBasisPdf(string creditNr)
        {
            using (var ms = new MemoryStream())
            {
                var r = Begin().Get($"Api/Credit/MortageLoanAmortizationBasisPdfWithCustomerCheck?creditNr={creditNr}&customerId={customerId}");

                r.DownloadFile(ms, out _, out _);

                return ms.ToArray();
            }
        }

        public class CreditsAccountDocumentsResult
        {
            public List<Document> Documents { get; set; }
            public class Document
            {
                public string CreditNr { get; set; }
                public string DocumentType { get; set; }
                public DateTimeOffset DocumentDate { get; set; }
                public string DownloadUrl { get; set; }
                public string ArchiveKey { get; set; }

            }
        }
    }
}