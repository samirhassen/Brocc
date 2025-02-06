using nCredit.DbModel.Repository;
using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.Code.Trapets
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

        public static TrapetsDomainModel GetChangesSinceLastExport(int currentUserId, string informationMetadata, Code.Trapets.TrapetsKycConfiguration config)
        {
            var d = new TrapetsDomainModel();
            d.currentUserId = currentUserId;
            d.informationMetadata = informationMetadata;

            var repo = new SystemItemRepository(currentUserId, informationMetadata);
            using (var context = new CreditContext())
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
                    new CreditCustomerClient());

                d.Transactions = GetTransactionsAfter(
                    repo.GetTimestamp(SystemItemCode.TrapetsAml_LatestTimestamp_Transaction, context),
                    out d.newTransactionTimestamp,
                    context);

                d.KycQuestionsAndAnswers = GetQuestionsAndAnswersWithChangesAfter(
                    repo.GetTimestamp(SystemItemCode.TrapetsAml_LatestTimestamp_KycQuestionsAndAnswers, context),
                    out d.newKycQuestionsAndAnswersTimestamp,
                    context,
                    new PreCreditClient(),
                    config);
            }

            return d;
        }

        public void UpdateChangeTrackingSystemItems()
        {
            var repo = new SystemItemRepository(currentUserId, informationMetadata);
            using (var context = new CreditContext())
            {
                if (newAccountTimestamp != null)
                    repo.SetTimestamp(SystemItemCode.TrapetsAml_LatestTimestamp_Account, newAccountTimestamp, context);

                if (newAssetTimestamp != null)
                    repo.SetTimestamp(SystemItemCode.TrapetsAml_LatestTimestamp_Asset, newAssetTimestamp, context);

                if (newCustomerTimestamp != null)
                    repo.SetTimestamp(SystemItemCode.TrapetsAml_LatestTimestamp_Customer, newCustomerTimestamp, context);

                if (newTransactionTimestamp != null)
                    repo.SetTimestamp(SystemItemCode.TrapetsAml_LatestTimestamp_Transaction, newTransactionTimestamp, context);

                if (newKycQuestionsAndAnswersTimestamp != null)
                    repo.SetTimestamp(SystemItemCode.TrapetsAml_LatestTimestamp_KycQuestionsAndAnswers, newKycQuestionsAndAnswersTimestamp, context);

                context.SaveChanges();
            }
        }

        private static List<CustomerModel> GetCustomersWithChangesAfter(byte[] latestSeenTimestamp, out byte[] maxTimestamp, CreditContext context, ICustomerClient customerClient)
        {
            var creditCustomersByCustomerId = context
                .CreditHeaders
                .SelectMany(x => x.CreditCustomers.Select(y => new
                {
                    x.CreditNr,
                    x.NrOfApplicants,
                    y.ApplicantNr,
                    y.CustomerId
                }))
                .GroupBy(x => x.CustomerId)
                .ToDictionary(x => x.Key, x => x.Select(y => new CustomerModel.CreditCustomer { ApplicantNr = y.ApplicantNr, CreditNr = y.CreditNr, NrOfApplicants = y.NrOfApplicants }).ToList());

            var customerData = customerClient.FetchTrapetsAmlData(latestSeenTimestamp, creditCustomersByCustomerId.Keys.ToList());
            maxTimestamp = customerData.Item1;

            var result = new List<CustomerModel>();
            foreach (var c in customerData.Item2)
            {
                result.Add(new CustomerModel
                {
                    Item = c,
                    Credits = creditCustomersByCustomerId[c.CustomerId]
                });
            }

            return result;
        }

        private static List<TransactionModel> GetTransactionsAfter(byte[] lastSeenTimestamp, out byte[] maxTimestamp, CreditContext context)
        {
            var q = context
                .Transactions
                .AsNoTracking()
                .Where(x => x.CreditNr != null && ((x.OutgoingPaymentId.HasValue && x.AccountCode == TransactionAccountType.ShouldBePaidToCustomer.ToString() && x.BusinessEvent.EventType == BusinessEventType.NewOutgoingPaymentFile.ToString()) || (x.IncomingPaymentId.HasValue && x.AccountCode == TransactionAccountType.CapitalDebt.ToString())))
                .AsQueryable();

            if (lastSeenTimestamp != null)
                q = q.Where(x => BinaryComparer.Compare(x.Timestamp, lastSeenTimestamp) > 0);

            maxTimestamp = q.OrderByDescending(x => x.Timestamp).Select(x => x.Timestamp).FirstOrDefault();

            return q.Select(x => new TransactionModel
            {
                CreditNr = x.CreditNr,
                Id = x.Id,
                Amount = x.Amount,
                IsConnectedToIncomingPayment = x.IncomingPaymentId.HasValue,
                IsConnectedToOutgoingPayment = x.OutgoingPaymentId.HasValue,
                TransactionDate = x.TransactionDate
            }).ToList();
        }

        private static List<AccountModel> GetAccountsWithChangesAfter(byte[] lastSeenTimestamp, out byte[] maxTimestamp, CreditContext context)
        {
            var q = context
                .CreditHeaders
                .AsNoTracking()
                .AsQueryable();

            var r = q.Select(x => new
            {
                x.CreditNr,
                x.StartDate,
                x.Status,
                Timestamp = x.Timestamp,
                x.ChangedDate,
                StatusDateItem = x
                    .DatedCreditStrings
                    .Where(y => y.Name == DatedCreditStringCode.CreditStatus.ToString())
                    .OrderByDescending(y => y.TransactionDate)
                    .ThenByDescending(y => y.Timestamp)
                    .FirstOrDefault()
            })
            .Select(x => new
            {
                x.CreditNr,
                x.StartDate,
                x.Status,
                StatusDate = x.StatusDateItem.TransactionDate,
                Timestamps = new[] { x.Timestamp, x.StatusDateItem.Timestamp },
                ChangedDates = new[] { x.ChangedDate, x.StatusDateItem.ChangedDate }
            })
            .Select(x => new
            {
                x.CreditNr,
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
                CreditNr = x.CreditNr,
                StartDate = x.StartDate,
                Status = x.Status,
                StatusDate = x.StatusDate
            }).ToList();
        }

        private static List<AssetModel> GetAssetsWithChangesAfter(byte[] lastSeenTimestamp, out byte[] maxTimestamp, CreditContext context)
        {
            var q = context
                .CreditHeaders
                .AsNoTracking()
                .AsQueryable();

            var r = q.Select(x => new
            {
                x.CreditNr,
                CapitalDebtTransactions = x.Transactions.Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
            })
            .Select(x => new
            {
                x.CreditNr,
                CapitalBalance = x.CapitalDebtTransactions.Sum(y => y.Amount),
                Timestamp = x.CapitalDebtTransactions.OrderByDescending(y => y.Timestamp).Select(y => y.Timestamp).FirstOrDefault(),
                TransactionDate = x.CapitalDebtTransactions.OrderByDescending(y => y.TransactionDate).Select(y => y.TransactionDate).FirstOrDefault(),
            });

            if (lastSeenTimestamp != null)
                r = r.Where(x => BinaryComparer.Compare(x.Timestamp, lastSeenTimestamp) > 0);

            maxTimestamp = r.OrderByDescending(x => x.Timestamp).Select(x => x.Timestamp).FirstOrDefault();

            return r.Select(x => new AssetModel
            {
                TransactionDate = x.TransactionDate,
                CreditNr = x.CreditNr,
                CapitalBalance = x.CapitalBalance
            }).ToList();
        }

        private static List<KycQuestionAnswerModel> GetQuestionsAndAnswersWithChangesAfter(byte[] latestSeenTimestamp, out byte[] maxTimestamp, CreditContext context, PreCreditClient client, TrapetsKycConfiguration config)
        {
            var creditCustomersByCustomerId = context
                .CreditHeaders
                .SelectMany(x => x.CreditCustomers.Select(y => new
                {
                    x.CreditNr,
                    x.NrOfApplicants,
                    y.ApplicantNr,
                    y.CustomerId
                }))
                .GroupBy(x => x.CustomerId)
                .ToDictionary(x => x.Key, x => x.Select(y => new CustomerModel.CreditCustomer { ApplicantNr = y.ApplicantNr, CreditNr = y.CreditNr, NrOfApplicants = y.NrOfApplicants }).ToList());

            var customerData = client.FetchAmlMonitoringKycQuestions(
                latestSeenTimestamp,
                config.ApplicationKycQuestionNamesToTransfer,
                creditCustomersByCustomerId.Keys.ToList());
            maxTimestamp = customerData.Item1;

            var result = new List<KycQuestionAnswerModel>();
            foreach (var c in customerData.Item2)
            {
                result.Add(new KycQuestionAnswerModel
                {
                    Item = c
                });
            }

            return result;
        }

        public class AccountModel
        {
            public DateTimeOffset ChangedDate { get; set; }
            public string CreditNr { get; set; }
            public DateTimeOffset StartDate { get; set; }
            public string Status { get; set; }
            public DateTime StatusDate { get; set; }
        }

        public class AssetModel
        {
            public DateTime TransactionDate { get; set; }
            public string CreditNr { get; set; }
            public decimal CapitalBalance { get; set; }
        }

        public class CustomerModel
        {
            public TrapetsAmlItem Item { get; set; }
            public class CreditCustomer
            {
                public string CreditNr { get; set; }
                public int ApplicantNr { get; set; }
                public int NrOfApplicants { get; set; }
            }
            public List<CreditCustomer> Credits { get; set; }
        }

        public class KycQuestionAnswerModel
        {
            public PreCreditClient.KycQuestionAnswerSet Item { get; set; }
        }

        public class TransactionModel
        {
            public string CreditNr { get; set; }
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