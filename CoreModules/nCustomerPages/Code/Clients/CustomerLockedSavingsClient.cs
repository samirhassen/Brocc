using NTech.Banking.CivicRegNumbers;
using System;
using System.Collections.Generic;
using System.IO;

namespace nCustomerPages.Code
{
    public class CustomerLockedSavingsClient : AbstractSystemUserServiceClient
    {
        protected override string ServiceName => "nSavings";
        private int customerId;

        public CustomerLockedSavingsClient(int customerId)
        {
            this.customerId = customerId;
        }

        public class AccountDetailsResult
        {
            public bool Exists { get; set; }
            public AccountDetails Details { get; set; }
            public IList<Transaction> Transactions { get; set; }
        }
        public class AccountDetails
        {
            public string SavingsAccountNr { get; set; }
            public string Status { get; set; }
            public DateTime? StatusDate { get; set; }
            public decimal? CapitalBalanceAmount { get; set; }
            public decimal? CurrentInterestRatePercent { get; set; }
            public decimal? AccumulatedInterestAmount { get; set; }
            public DateTime? AccountOpenedDate { get; set; }
            public string AccountDepositIban { get; set; }
            public string AccountDepositOcrReferenceNr { get; set; }
        }
        public class Transaction
        {
            public long Id { get; set; }
            public int CreatedByEventId { get; set; }
            public string BusinessEventType { get; set; }
            public string BusinessEventRoleCode { get; set; }
            public DateTime? TransactionDate { get; set; }
            public DateTime? BookkeepingDate { get; set; }
            public DateTime? InterestFromDate { get; set; }
            public decimal? Amount { get; set; }
            public decimal? BalanceAfterAmount { get; set; }
        }

        public class SavingsAccountExternalVariable
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        public class YearlySummary
        {
            public string SavingsAccountNr { get; set; }
            public int Year { get; set; }
        }

        public List<YearlySummary> GetYearlySummaries()
        {
            return Begin()
                .PostJson("Api/CustomerPages/YearlySummaries", new { customerId })
                .ParseJsonAs<List<YearlySummary>>();
        }

        public List<SavingsAccountExternalVariable> GetExternalVariables(string savingsAccountNr)
        {
            return Begin()
                .PostJson("Api/CustomerPages/SavingsAccountExternalVariables", new
                {
                    customerId,
                    savingsAccountNr
                })
                .ParseJsonAsAnonymousType(new { externalVariables = (List<SavingsAccountExternalVariable>)null })?.externalVariables;
        }

        public AccountDetailsResult GetSavingsAccountDetails(string savingsAccountNr, int? maxTransactionsCount, int? startBeforeTransactionId)
        {
            return Begin()
                .PostJson("Api/CustomerPages/SavingsAccountDetails", new
                {
                    customerId,
                    savingsAccountNr,
                    maxTransactionsCount,
                    startBeforeTransactionId
                })
                .ParseJsonAs<AccountDetailsResult>();
        }

        public AccountDetailsResult GetAccountDetails(string savingsAccountNr, int? maxTransactionsCount, int? startBeforeTransactionId)
        {
            return Begin()
                .PostJson("Api/CustomerPages/SavingsAccountDetails", new
                {
                    customerId,
                    savingsAccountNr,
                    maxTransactionsCount,
                    startBeforeTransactionId
                })
                .ParseJsonAs<AccountDetailsResult>();
        }

        public IList<Transaction> GetEventOrderedSavingsAccountTransactions(string savingsAccountNr, int? maxCountTransactions, int? startBeforeTransactionId)
        {
            return Begin()
                .PostJson("Api/CustomerPages/EventOrderedSavingsAccountTransactions", new
                {
                    customerId,
                    savingsAccountNr,
                    maxCountTransactions,
                    startBeforeTransactionId
                })
                .ParseJsonAsAnonymousType(new { Transactions = (IList<Transaction>)null })
                ?.Transactions;
        }

        public class SavingsAccount
        {
            public string SavingsAccountNr { get; set; }
            public string Status { get; set; }
            public DateTime? StatusDate { get; set; }
            public decimal? CapitalBalanceAmount { get; set; }
            public decimal? CurrentInterestRatePercent { get; set; }
            public DateTime? AccountOpenedDate { get; set; }
            public string AccountTypeCode { get; set; }
        }
        public (IList<SavingsAccount> Accounts, bool AreWithdrawalsSuspended) GetSavingsAccounts()
        {
            var result = Begin()
                .PostJson("Api/CustomerPages/SavingsAccounts", new
                {
                    customerId
                })
                .ParseJsonAsAnonymousType(new { SavingsAccounts = (IList<SavingsAccount>)null, AreWithdrawalsSuspended = (bool)false });
            return (Accounts: result?.SavingsAccounts, AreWithdrawalsSuspended: result?.AreWithdrawalsSuspended ?? false);

        }

        public class CreateWithdrawalResult
        {
            public decimal? WithdrawableAmountAfter { get; set; }
            public string NewUniqueOperationToken { get; set; }
        }
        public bool TryCreateWithdrawal(
            string savingsAccountNr,
            string expectedToIban,
            decimal? withdrawalAmount,
            string uniqueOperationToken,
            string customCustomerMessageText,
            string customTransactionText,
            string requestAuthenticationMethod,
            string requestIpAddress,
            decimal? penaltyFees,
            out string failedMessage, out CreateWithdrawalResult result)
        {
            var r = Begin()
                .PostJson("Api/CustomerPages/CreateWithdrawal", new
                {
                    customerId,
                    savingsAccountNr,
                    expectedToIban,
                    withdrawalAmount,
                    uniqueOperationToken,
                    customCustomerMessageText,
                    customTransactionText,
                    requestAuthenticationMethod,
                    requestIpAddress,
                    penaltyFees
                });
            if (r.StatusCode == 400)
            {
                failedMessage = r.ReasonPhrase;
                result = null;
                return false;
            }
            else
            {
                result = r.ParseJsonAs<CreateWithdrawalResult>();
                failedMessage = null;
                return true;
            }
        }

        public bool TryCloseAccount(
                string savingsAccountNr, string expectedToIban,
                string uniqueOperationToken, string customCustomerMessageText, string customTransactionText,
                string requestAuthenticationMethod, string requestIpAddress, out string failedMessage, out string newUniqueOperationToken)
        {
            var r = Begin()
                    .PostJson("Api/CustomerPages/CloseAccount", new
                    {
                        customerId,
                        savingsAccountNr,
                        expectedToIban,
                        uniqueOperationToken,
                        customCustomerMessageText,
                        customTransactionText,
                        requestAuthenticationMethod,
                        requestIpAddress
                    });
            if (r.StatusCode == 400)
            {
                failedMessage = r.ReasonPhrase;
                newUniqueOperationToken = null;
                return false;
            }
            else
            {
                newUniqueOperationToken = r.ParseJsonAsAnonymousType(new { NewUniqueOperationToken = (string)null })?.NewUniqueOperationToken;
                failedMessage = null;
                return true;
            }
        }

        public class DepositsInitialDataResult
        {
            public List<Account> Accounts { get; set; }
            public class Account
            {
                public string SavingsAccountNr { get; set; }
                public string AccountTypeCode { get; set; }
                public string AccountDepositIban { get; set; }
                public string AccountDepositOcrReferenceNr { get; set; }
            }
        }
        public DepositsInitialDataResult GetDepositsInitialData()
        {
            return Begin()
                .PostJson("Api/CustomerPages/DepositsInitialData", new
                {
                    customerId
                })
                .ParseJsonAs<DepositsInitialDataResult>();
        }

        public class WithdrawalsInitialDataResult
        {
            public List<Account> Accounts { get; set; }
            public string UniqueOperationToken { get; set; }
            public bool AreWithdrawalsSuspended { get; set; }
            public class Account
            {
                public string SavingsAccountNr { get; set; }
                public string AccountTypeCode { get; set; }
                public string ToIban { get; set; }
                public decimal? WithdrawableAmount { get; set; }
                public DateTime? MaturesAt { get; set; }

            }
        }
        public WithdrawalsInitialDataResult GetWithdrawalsInitialData()
        {
            return Begin()
                .PostJson("Api/CustomerPages/WithdrawalsInitialData", new
                {
                    customerId
                })
                .ParseJsonAs<WithdrawalsInitialDataResult>();
        }

        public class CloseAccountPreviewData
        {
            public string UniqueOperationToken { get; set; }
            public string SavingsAccountNr { get; set; }
            public string ToIban { get; set; }
            public string CapitalBalanceAmount { get; set; }
            public decimal AccumulatedInterestAmount { get; set; }
            public decimal TaxAmount { get; set; }
            public decimal PaidOutAmount { get; set; }
        }

        public CloseAccountPreviewData GetCloseAccountPreviewData(string savingsAccountNr)
        {
            return Begin()
                .PostJson("Api/CustomerPages/CloseAccountPreviewData", new
                {
                    customerId,
                    savingsAccountNr
                })
                .ParseJsonAs<CloseAccountPreviewData>();
        }

        public class SavingsAccountLedgerTransactionDetailsResult
        {
            public int Id { get; set; }
            public DateTime TransactionDate { get; set; }
            public DateTime BookKeepingDate { get; set; }
            public DateTime InterestFromDate { get; set; }
            public bool IsConnectedToOutgoingPayment { get; set; }
            public bool IsConnectedToIncomingPayment { get; set; }
            public string OutgoingPaymentCustomTransactionMessage { get; set; }
        }
        public SavingsAccountLedgerTransactionDetailsResult GetSavingsAccountLedgerTransactionDetails(int transactionId)
        {
            return Begin()
                .PostJson("Api/CustomerPages/SavingsAccountLedgerTransactionDetails", new
                {
                    customerId,
                    transactionId = transactionId
                })
                .ParseJsonAs<SavingsAccountLedgerTransactionDetailsResult>();
        }

        public class GetWithdrawalAccountsResult
        {
            public IList<Item> SavingsAccounts { get; set; }
            public class Item
            {
                public string SavingsAccountNr { get; set; }
                public string AccountTypeCode { get; set; }
                public string Status { get; set; }
                public string WithdrawalIban { get; set; }
                public DateTime? WithdrawalIbanDate { get; set; }
            }
        }
        public GetWithdrawalAccountsResult GetWithdrawalAccounts()
        {
            return Begin()
                .PostJson("Api/CustomerPages/WithdrawalAccounts", new { customerId })
                .ParseJsonAs<GetWithdrawalAccountsResult>();
        }

        public GetInterestHistoryResult GetInterestHistory(string savingsAccountNr)
        {
            return Begin()
                .PostJson("Api/CustomerPages/SavingsAccountInterestHistory", new { customerId, savingsAccountNr })
                .ParseJsonAs<GetInterestHistoryResult>();
        }

        public class GetInterestHistoryResult
        {
            public class Item
            {
                public int? Id { get; set; }
                public string BusinessEventId { get; set; }
                public DateTime? TransactionDate { get; set; }
                public DateTime? ValidFromDate { get; set; }
                public decimal? InterestRatePercent { get; set; }
                public bool? WasCreatedAfterClose { get; set; }
            }

            public List<Item> InterestRates { get; set; }
        }

        public class SavingsAccountDocumentsResult
        {
            public List<Document> Documents { get; set; }
            public class Document
            {
                public int DocumentId { get; set; }
                public string SavingsAccountNr { get; set; }
                public string DocumentType { get; set; }
                public DateTimeOffset DocumentDate { get; set; }
            }
        }

        public SavingsAccountDocumentsResult GetSavingsAccountDocuments()
        {
            return Begin()
                .PostJson("Api/CustomerPages/SavingsAccountDocuments", new { customerId })
                .ParseJsonAs<SavingsAccountDocumentsResult>();
        }

        public bool TryFetchSavingsAccountDocument(int documentId, out string contentType, out string fileName, out byte[] fileBytes)
        {
            using (var ms = new MemoryStream())
            {
                var r = Begin().Get($"Api/CustomerPages/SavingsAccountDocument?documentId={documentId}&customerId={customerId}");

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

        public bool TryFetchYearlySummary(string savingsAccountNr, int year, out string contentType, out string fileName, out byte[] fileBytes)
        {
            using (var ms = new MemoryStream())
            {
                var r = Begin().Get($"Api/CustomerPages/SavingsAccountYearlySummaryDocument?savingsAccountNr={savingsAccountNr}&year={year}&customerId={customerId}");

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

        public class FetchCustomerAddressFromTrustedSourceResult
        {
            public bool IsSuccess { get; set; }
            public Dictionary<string, string> Items { get; set; }
        }

        public FetchCustomerAddressFromTrustedSourceResult FetchCustomerAddressFromTrustedSource(ICivicRegNumber civicRegNumber, params string[] itemNames)
        {
            return Begin()
                .PostJson("Api/CustomerPages/FetchCustomerAddressFromTrustedSource", new { civicRegNumber = civicRegNumber.NormalizedValue, itemNames, customerId })
                .ParseJsonAs<FetchCustomerAddressFromTrustedSourceResult>();
        }
    }
}