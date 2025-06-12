using System;
using System.Collections.Generic;
using System.Linq;
using nSavings.DbModel;
using nSavings.DbModel.Repository;
using NTech.Banking.CivicRegNumbers;
using NTech.Core.Savings.Shared.DbModel;

namespace nSavings.Code.Trapets
{
    public class TrapetsDomainModel
    {
        public List<AccountModel> Accounts { get; set; }
        private byte[] newAccountTimestamp;

        public List<AssetModel> Assets { get; set; }
        private byte[] newAssetTimestamp;

        public List<CustomerModel> Customers { get; set; }
        private byte[] newCustomerTimestamp;

        public List<TransactionModel> Transactions { get; set; }
        private byte[] newTransactionTimestamp;

        public List<KycQuestionAnswerModel> KycQuestionsAndAnswers { get; set; }
        private byte[] newKycQuestionsAndAnswersTimestamp;

        private int currentUserId;
        private string informationMetadata;

        private TrapetsDomainModel()
        {
        }

        private class CustomerCivicRegNrCache
        {
            private readonly CustomerClient client;
            private readonly CivicRegNumberParser civicRegNumberParser;

            private readonly Dictionary<int, ICivicRegNumber> civicRegNrByCustomerId =
                new Dictionary<int, ICivicRegNumber>();

            public CustomerCivicRegNrCache(CustomerClient client, CivicRegNumberParser civicRegNumberParser)
            {
                this.client = client;
                this.civicRegNumberParser = civicRegNumberParser;
            }

            public void Set(int customerId, string civicRegNr)
            {
                civicRegNrByCustomerId[customerId] = civicRegNumberParser.Parse(civicRegNr);
            }

            public IDictionary<int, ICivicRegNumber> GetCivicRegNumbersForCustomers(ISet<int> customerIds)
            {
                var missingIds = customerIds.Except(civicRegNrByCustomerId.Keys).ToList();
                if (!missingIds.Any()) return customerIds.ToDictionary(x => x, x => civicRegNrByCustomerId[x]);
                var result = client.BulkFetchPropertiesByCustomerIds(new HashSet<int>(missingIds), "civicRegNr");
                foreach (var r in result)
                {
                    civicRegNrByCustomerId[r.Key] = civicRegNumberParser.Parse(r.Value.Properties.Single().Value);
                }

                return customerIds.ToDictionary(x => x, x => civicRegNrByCustomerId[x]);
            }
        }

        public static TrapetsDomainModel GetChangesSinceLastExport(int currentUserId, string informationMetadata,
            TrapetsKycConfiguration config)
        {
            var d = new TrapetsDomainModel
            {
                currentUserId = currentUserId,
                informationMetadata = informationMetadata
            };

            var customerClient = new CustomerClient();
            var civicRegNrs = new CustomerCivicRegNrCache(customerClient, NEnv.BaseCivicRegNumberParser);

            var repo = new SystemItemRepository(currentUserId, informationMetadata);
            using (var context = new SavingsContext())
            {
                d.Accounts = GetAccountsWithChangesAfter(
                    repo.GetTimestamp(SystemItemCode.TrapetsAml_LatestTimestamp_Account, context),
                    out d.newAccountTimestamp,
                    context);

                d.Assets = GetAssetsWithChangesAfter(
                    repo.GetTimestamp(SystemItemCode.TrapetsAml_LatestTimestamp_Asset, context),
                    out d.newAssetTimestamp,
                    context);

                d.Customers = GetCustomersWithChangesAfter(
                    repo.GetTimestamp(SystemItemCode.TrapetsAml_LatestTimestamp_Customer, context),
                    out d.newCustomerTimestamp,
                    context,
                    customerClient,
                    civicRegNrs);

                d.Transactions = GetTransactionsAfter(
                    repo.GetTimestamp(SystemItemCode.TrapetsAml_LatestTimestamp_Transaction, context),
                    out d.newTransactionTimestamp,
                    context);

                d.KycQuestionsAndAnswers = GetQuestionsAndAnswersWithChangesAfter(
                    repo.GetTimestamp(SystemItemCode.TrapetsAml_LatestTimestamp_KycQuestionsAndAnswers, context),
                    out d.newKycQuestionsAndAnswersTimestamp,
                    context,
                    config,
                    civicRegNrs.GetCivicRegNumbersForCustomers);
            }

            return d;
        }

        public void UpdateChangeTrackingSystemItems()
        {
            var repo = new SystemItemRepository(currentUserId, informationMetadata);
            using (var context = new SavingsContext())
            {
                if (newAccountTimestamp != null)
                    repo.SetTimestamp(SystemItemCode.TrapetsAml_LatestTimestamp_Account, newAccountTimestamp, context);

                if (newAssetTimestamp != null)
                    repo.SetTimestamp(SystemItemCode.TrapetsAml_LatestTimestamp_Asset, newAssetTimestamp, context);

                if (newCustomerTimestamp != null)
                    repo.SetTimestamp(SystemItemCode.TrapetsAml_LatestTimestamp_Customer, newCustomerTimestamp,
                        context);

                if (newTransactionTimestamp != null)
                    repo.SetTimestamp(SystemItemCode.TrapetsAml_LatestTimestamp_Transaction, newTransactionTimestamp,
                        context);

                if (newKycQuestionsAndAnswersTimestamp != null)
                    repo.SetTimestamp(SystemItemCode.TrapetsAml_LatestTimestamp_KycQuestionsAndAnswers,
                        newKycQuestionsAndAnswersTimestamp, context);

                context.SaveChanges();
            }
        }

        private static List<KycQuestionAnswerModel> GetQuestionsAndAnswersWithChangesAfter(byte[] lastSeenTimestamp,
            out byte[] maxTimestamp, SavingsContext context, TrapetsKycConfiguration config,
            Func<ISet<int>, IDictionary<int, ICivicRegNumber>> getCivicRegNumbers)
        {
            const string savingsQuestionPrefix = "savings_";

            var questionNames = config.ApplicationKycQuestionNamesToTransfer.Select(StripSavingsPrefix).ToList();
            var q = context
                .SavingsAccountKycQuestions
                .AsNoTracking()
                .Where(x => questionNames.Contains(x.Name));

            if (lastSeenTimestamp != null)
                q = q.Where(x => BinaryComparer.Compare(x.Timestamp, lastSeenTimestamp) > 0);

            maxTimestamp = q.OrderByDescending(x => x.Timestamp).Select(x => x.Timestamp).FirstOrDefault();

            var result = q.Select(x => new KycQuestionAnswerModel
            {
                Date = x.BusinessEvent.TransactionDate,
                AnswerCode = x.Value,
                QuestionCode = x.Name,
                CustomerId = x.SavingsAccount.MainCustomerId,
                SavingsAccountNr = x.SavingsAccountNr,
            }).ToList();

            var civicRegNrsByCustomerId = getCivicRegNumbers(new HashSet<int>(result.Select(x => x.CustomerId)));
            foreach (var r in result)
            {
                r.CivicRegNumber = civicRegNrsByCustomerId[r.CustomerId];
            }

            return result;

            string StripSavingsPrefix(string x) =>
                x.StartsWith(savingsQuestionPrefix) ? x.Substring(savingsQuestionPrefix.Length) : x;
        }

        private static List<CustomerModel> GetCustomersWithChangesAfter(byte[] latestSeenTimestamp,
            out byte[] maxTimestamp, SavingsContext context, CustomerClient customerClient,
            CustomerCivicRegNrCache civicRegNrs)
        {
            var savingaAccountNrsByCustomerId = context
                .SavingsAccountHeaders
                .Select(x => new
                {
                    x.SavingsAccountNr,
                    x.MainCustomerId,
                })
                .GroupBy(x => x.MainCustomerId)
                .ToDictionary(x => x.Key, x => x.Select(y => y.SavingsAccountNr).ToList());

            var customerData =
                customerClient.FetchTrapetsAmlData(latestSeenTimestamp, savingaAccountNrsByCustomerId.Keys.ToList());
            maxTimestamp = customerData.Item1;

            var result = new List<CustomerModel>();
            foreach (var c in customerData.Item2)
            {
                result.Add(new CustomerModel
                {
                    Item = c,
                    SavingsAccounts = savingaAccountNrsByCustomerId[c.CustomerId].Select(x =>
                        new CustomerModel.SavingsCustomer
                        {
                            IsMainCustomer = true,
                            SavingsAccountNr = x,
                            NrOfCustomers = 1
                        }).ToList()
                });
            }

            foreach (var c in customerData.Item2)
            {
                civicRegNrs.Set(c.CustomerId, c.CivicRegNr);
            }

            return result;
        }

        private static List<TransactionModel> GetTransactionsAfter(byte[] lastSeenTimestamp, out byte[] maxTimestamp,
            SavingsContext context)
        {
            var q = context
                .LedgerAccountTransactions
                .AsNoTracking()
                .Where(x => x.SavingsAccountNr != null &&
                            ((x.OutgoingPaymentId.HasValue &&
                              x.AccountCode == LedgerAccountTypeCode.ShouldBePaidToCustomer.ToString() &&
                              x.BusinessEvent.EventType == BusinessEventType.OutgoingPaymentFileExport.ToString()) ||
                             (x.IncomingPaymentId.HasValue &&
                              x.AccountCode == LedgerAccountTypeCode.Capital.ToString())))
                .AsQueryable();

            if (lastSeenTimestamp != null)
                q = q.Where(x => BinaryComparer.Compare(x.Timestamp, lastSeenTimestamp) > 0);

            maxTimestamp = q.OrderByDescending(x => x.Timestamp).Select(x => x.Timestamp).FirstOrDefault();

            return q.Select(x => new TransactionModel
            {
                SavingsAccountNr = x.SavingsAccountNr,
                Id = x.Id,
                Amount = x.Amount,
                IsConnectedToIncomingPayment = x.IncomingPaymentId.HasValue,
                IsConnectedToOutgoingPayment = x.OutgoingPaymentId.HasValue,
                TransactionDate = x.TransactionDate
            }).ToList();
        }

        private static List<AccountModel> GetAccountsWithChangesAfter(byte[] lastSeenTimestamp, out byte[] maxTimestamp,
            SavingsContext context)
        {
            var q = context
                .SavingsAccountHeaders
                .AsNoTracking()
                .AsQueryable();

            var r = q.Select(x => new
                {
                    x.SavingsAccountNr,
                    StartDate = x.CreatedByEvent.TransactionDate,
                    x.Status,
                    Timestamp = x.Timestamp,
                    x.ChangedDate,
                    StatusDateItem = x
                        .DatedStrings
                        .Where(y => y.Name == DatedSavingsAccountStringCode.SavingsAccountStatus.ToString())
                        .OrderByDescending(y => y.TransactionDate)
                        .ThenByDescending(y => y.Timestamp)
                        .FirstOrDefault()
                })
                .Select(x => new
                {
                    x.SavingsAccountNr,
                    x.StartDate,
                    x.Status,
                    StatusDate = x.StatusDateItem.TransactionDate,
                    Timestamps = new[] { x.Timestamp, x.StatusDateItem.Timestamp },
                    ChangedDates = new[] { x.ChangedDate, x.StatusDateItem.ChangedDate }
                })
                .Select(x => new
                {
                    x.SavingsAccountNr,
                    x.StartDate,
                    x.StatusDate,
                    x.Status,
                    Timestamp = x.Timestamps.OrderByDescending(y => y).FirstOrDefault(),
                    ChangedDate = x.ChangedDates.OrderByDescending(y => y).FirstOrDefault()
                });

            if (lastSeenTimestamp != null)
                r = r.Where(x => BinaryComparer.Compare(x.Timestamp, lastSeenTimestamp) > 0);

            maxTimestamp = r.OrderByDescending(x => x.Timestamp).Select(x => x.Timestamp).FirstOrDefault();

            return r.Select(x => new AccountModel
            {
                ChangedDate = x.ChangedDate,
                SavingsAccountNr = x.SavingsAccountNr,
                StartDate = x.StartDate,
                Status = x.Status,
                StatusDate = x.StatusDate
            }).ToList();
        }

        private static List<AssetModel> GetAssetsWithChangesAfter(byte[] lastSeenTimestamp, out byte[] maxTimestamp,
            SavingsContext context)
        {
            var q = context
                .SavingsAccountHeaders
                .AsNoTracking()
                .Where(x => x.Transactions.Any());

            var r = q.Select(x => new
                {
                    x.SavingsAccountNr,
                    CapitalBalanceTransactions =
                        x.Transactions.Where(y => y.AccountCode == LedgerAccountTypeCode.Capital.ToString())
                })
                .Select(x => new
                {
                    x.SavingsAccountNr,
                    CapitalBalance = x.CapitalBalanceTransactions.Sum(y => y.Amount),
                    Timestamp = x.CapitalBalanceTransactions.OrderByDescending(y => y.Timestamp)
                        .Select(y => y.Timestamp).FirstOrDefault(),
                    TransactionDate = x.CapitalBalanceTransactions.OrderByDescending(y => y.TransactionDate)
                        .Select(y => y.TransactionDate).FirstOrDefault(),
                });

            if (lastSeenTimestamp != null)
                r = r.Where(x => BinaryComparer.Compare(x.Timestamp, lastSeenTimestamp) > 0);

            maxTimestamp = r.OrderByDescending(x => x.Timestamp).Select(x => x.Timestamp).FirstOrDefault();

            return r.Select(x => new AssetModel
            {
                TransactionDate = x.TransactionDate,
                SavingsAccountNr = x.SavingsAccountNr,
                CapitalBalance = x.CapitalBalance
            }).ToList();
        }

        public class AccountModel
        {
            public DateTimeOffset ChangedDate { get; set; }
            public string SavingsAccountNr { get; set; }
            public DateTimeOffset StartDate { get; set; }
            public string Status { get; set; }
            public DateTime StatusDate { get; set; }
        }

        public class KycQuestionAnswerModel
        {
            public DateTime Date { get; set; }
            public ICivicRegNumber CivicRegNumber { get; set; }
            public int CustomerId { get; set; }
            public string SavingsAccountNr { get; set; }
            public string QuestionCode { get; set; }
            public string AnswerCode { get; set; }
        }

        public class AssetModel
        {
            public DateTime TransactionDate { get; set; }
            public string SavingsAccountNr { get; set; }
            public decimal CapitalBalance { get; set; }
        }

        public class CustomerModel
        {
            public CustomerClient.TrapetsAmlItem Item { get; set; }

            public class SavingsCustomer
            {
                public string SavingsAccountNr { get; set; }
                public bool IsMainCustomer { get; set; }
                public int NrOfCustomers { get; set; }
            }

            public List<SavingsCustomer> SavingsAccounts { get; set; }
        }

        public class TransactionModel
        {
            public string SavingsAccountNr { get; set; }
            public DateTime TransactionDate { get; set; }
            public long Id { get; set; }
            public bool IsConnectedToOutgoingPayment { get; set; }
            public bool IsConnectedToIncomingPayment { get; set; }
            public decimal Amount { get; set; }
        }

        private static class BinaryComparer
        {
            public static int Compare(byte[] b1, byte[] b2)
            {
                throw new NotImplementedException();
            }
        }
    }
}