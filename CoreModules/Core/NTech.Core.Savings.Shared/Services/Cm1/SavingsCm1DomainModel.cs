using System;
using System.Collections.Generic;
using System.Linq;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Services;
using NTech.Core.Savings.Shared.Database;
using NTech.Core.Savings.Shared.DbModel;

namespace NTech.Core.Savings.Shared.Services.Cm1
{
    public class SavingsCm1DomainModel
    {
        public List<PerProductCmlExportFileRequest.TransactionModel> Transactions { get; set; }
        private long? newTransactionId;

        private readonly int currentUserId;
        private readonly string informationMetadata;
        private readonly SavingsContextFactory contextFactory;
        private Lazy<NTechSimpleSettingsCore> cm1Settings;
        private readonly ICustomerClient customerClient;
        private readonly EncryptionService encryptionService;

        private SavingsCm1DomainModel(int currentUserId, string informationMetadata, SavingsContextFactory contextFactory, 
            Lazy<NTechSimpleSettingsCore> cm1Settings, ICustomerClient customerClient, EncryptionService encryptionService)
        {
            this.currentUserId = currentUserId;
            this.informationMetadata = informationMetadata;
            this.contextFactory = contextFactory;
            this.cm1Settings = cm1Settings;
            this.customerClient = customerClient;
            this.encryptionService = encryptionService;
        }

        public static SavingsCm1DomainModel GetChangesSinceLastExport(int currentUserId, string informationMetadata, 
            SavingsContextFactory contextFactory, Lazy<NTechSimpleSettingsCore> cm1Settings, ICustomerClient customerClient, 
            EncryptionService encryptionService)
        {
            var d = new SavingsCm1DomainModel(currentUserId, informationMetadata, contextFactory, cm1Settings, customerClient, encryptionService);

            var repo = new SavingsCoreSystemItemRepository(currentUserId, informationMetadata);
            using (var context = contextFactory.CreateContext())
            {
                context.IsChangeTrackingEnabled = false;

                d.Transactions = GetTransactionsAfter(
                    repo.GetLong(SystemItemCode.Cm1Aml_LatestId_Transaction, context),
                    out d.newTransactionId,
                    context, customerClient, encryptionService);
            }

            return d;
        }

        public void UpdateChangeTrackingSystemItems()
        {
            var repo = new SavingsCoreSystemItemRepository(currentUserId, informationMetadata);
            using (var context = contextFactory.CreateContext())
            {
                UpdateChangeTrackingSystemItems(context);
                context.SaveChanges();
            }
        }

        public void UpdateChangeTrackingSystemItems(ISavingsContext context)
        {
            var repo = new SavingsCoreSystemItemRepository(currentUserId, informationMetadata);
            if (newTransactionId != null && newTransactionId != 0)
                repo.SetLong(SystemItemCode.Cm1Aml_LatestId_Transaction, long.Parse(newTransactionId.ToString()), context);
        }

        private static List<PerProductCmlExportFileRequest.TransactionModel> GetTransactionsAfter(long? lastId, out long? maxId, ISavingsContext context, ICustomerClient customerClient, EncryptionService encryptionService)
        {
            if (!lastId.HasValue)
                lastId = 0;

            var qPre = context
               .LedgerAccountTransactionsQueryable
               .Where(x => x.Id > lastId && x.SavingsAccountNr != null && ((x.OutgoingPaymentId.HasValue && x.AccountCode == LedgerAccountTypeCode.ShouldBePaidToCustomer.ToString() && x.BusinessEvent.EventType == BusinessEventType.OutgoingPaymentFileExport.ToString()) || (x.IncomingPaymentId.HasValue && x.AccountCode == LedgerAccountTypeCode.Capital.ToString())))
               .Select(x => new
               {
                   PayerNameItem = x.IncomingPayment.Items.Where(y => y.Name == IncomingPaymentHeaderItemCode.CustomerName.ToString()).Select(y => new { y.IncomingPaymentHeaderId, y.Value, y.IsEncrypted }).FirstOrDefault(),
                   TransactionPre = new PerProductCmlExportFileRequest.TransactionModel
                   {
                       Id = x.Id,
                       Amount = x.Amount,
                       IsConnectedToIncomingPayment = x.IncomingPaymentId.HasValue,
                       IsConnectedToOutgoingPayment = x.OutgoingPaymentId.HasValue,
                       TransactionDate = x.TransactionDate,
                       CustomerId = x.SavingsAccount.MainCustomerId
                   }
               }).ToList();

            Dictionary<int, string> payerNameByPaymentId = GetPayerNameByPaymentId(context, qPre.Where(x => x.PayerNameItem != null).Select(x =>
                (x.PayerNameItem.IncomingPaymentHeaderId, x.PayerNameItem.Value, x.PayerNameItem.IsEncrypted)).ToList(), encryptionService);

            var q = qPre.Select(x => x.TransactionPre).ToList();
            foreach (var transaction in qPre.Where(x => x.PayerNameItem != null))
            {
                transaction.TransactionPre.TransactionCustomerName = payerNameByPaymentId.Opt(transaction.PayerNameItem.IncomingPaymentHeaderId);
            }

            maxId = q.OrderByDescending(x => x.Id).Select(x => x.Id).FirstOrDefault();

            return q.Select(x => new PerProductCmlExportFileRequest.TransactionModel
            {
                Id = x.Id,
                Amount = x.Amount,
                IsConnectedToIncomingPayment = x.IsConnectedToIncomingPayment,
                IsConnectedToOutgoingPayment = x.IsConnectedToOutgoingPayment,
                TransactionDate = x.TransactionDate,
                CustomerId = x.CustomerId,
                TransactionCustomerName = x.TransactionCustomerName
            }).ToList();
        }

        private static Dictionary<int, string> GetPayerNameByPaymentId(ISavingsContext context, List<(int IncomingPaymentHeaderId, string Value, bool IsEncrypted)> customerNameItems, EncryptionService encryptionService)
        {
            var result = new Dictionary<int, string>();
            foreach (var item in customerNameItems.Where(x => !x.IsEncrypted))
            {
                result[item.IncomingPaymentHeaderId] = item.Value;
            }
            var encryptedItems = customerNameItems.Where(x => x.IsEncrypted).ToList();
            if (encryptedItems.Count > 0)
            {
                var decryptedValues = encryptionService.DecryptEncryptedValues(context, encryptedItems.Select(x => long.Parse(x.Value)).ToArray());
                foreach (var item in encryptedItems)
                {
                    result[item.IncomingPaymentHeaderId] = decryptedValues[long.Parse(item.Value)];
                }
            }
            return result;
        }
    }
}