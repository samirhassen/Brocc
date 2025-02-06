using nCredit.DbModel.Repository;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.Code.Cm1
{
    public class CreditCm1DomainModel
    {
        public List<PerProductCmlExportFileRequest.TransactionModel> Transactions { get; set; }
        private long? newTransactionId;
        private readonly CreditContextFactory contextFactory;
        private readonly EncryptionService encryptionService;

        private CreditCm1DomainModel(CreditContextFactory contextFactory, EncryptionService encryptionService)
        {
            this.contextFactory = contextFactory;
            this.encryptionService = encryptionService;
        }

        public static CreditCm1DomainModel GetChangesSinceLastExport(INTechCurrentUserMetadata user, CreditContextFactory contextFactory, EncryptionService encryptionService)
        {
            var d = new CreditCm1DomainModel(contextFactory, encryptionService);

            var repo = new CoreSystemItemRepository(user);
            using (var context = contextFactory.CreateContext())
            {
                context.IsChangeTrackingEnabled = false;

                d.Transactions = d.GetTransactionsAfter(
                    repo.GetLong(SystemItemCode.Cm1Aml_LatestId_Transaction, context),
                    out d.newTransactionId,
                    context);
            }

            return d;
        }

        public void UpdateChangeTrackingSystemItems(INTechCurrentUserMetadata currentUser)
        {
            var repo = new CoreSystemItemRepository(currentUser);
            using (var context = contextFactory.CreateContext())
            {
                UpdateChangeTrackingSystemItems(context);
                context.SaveChanges();
            }
        }

        public void UpdateChangeTrackingSystemItems(ICreditContextExtended context)
        {
            var repo = new CoreSystemItemRepository(context.CurrentUser);
            if (newTransactionId != null && newTransactionId != 0)
                repo.SetLong(SystemItemCode.Cm1Aml_LatestId_Transaction, long.Parse(newTransactionId.ToString()), context);
        }

        private List<PerProductCmlExportFileRequest.TransactionModel> GetTransactionsAfter(long? lastId, out long? maxId, ICreditContextExtended context)
        {
            if (!lastId.HasValue)
                lastId = 0;

            var qPre = context
               .TransactionsQueryable
               .Where(x => x.Id > lastId && x.CreditNr != null && ((x.OutgoingPaymentId.HasValue && x.AccountCode == TransactionAccountType.ShouldBePaidToCustomer.ToString() && x.BusinessEvent.EventType == BusinessEventType.NewOutgoingPaymentFile.ToString()) || (x.IncomingPaymentId.HasValue && x.AccountCode == TransactionAccountType.CapitalDebt.ToString())))
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
                       CustomerId = x.Credit.CreditCustomers.FirstOrDefault().CustomerId
                   }
               })
               .ToList();

            Dictionary<int, string> payerNameByPaymentId = GetPayerNameByPaymentId(context, qPre.Where(x => x.PayerNameItem != null).Select(x => 
                (x.PayerNameItem.IncomingPaymentHeaderId, x.PayerNameItem.Value, x.PayerNameItem.IsEncrypted)).ToList());            

            var q = qPre.Select(x => x.TransactionPre).ToList();
            foreach(var transaction in qPre.Where(x => x.PayerNameItem != null))
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

        private Dictionary<int, string> GetPayerNameByPaymentId(ICreditContextExtended context, List<(int IncomingPaymentHeaderId, string Value, bool IsEncrypted)> customerNameItems)
        {
            var result = new Dictionary<int, string>();
            foreach (var item in customerNameItems.Where(x => !x.IsEncrypted))
            {
                result[item.IncomingPaymentHeaderId] = item.Value;
            }
            var encryptedItems = customerNameItems.Where(x => x.IsEncrypted).ToList();
            if(encryptedItems.Count > 0)
            {
                var decryptedValues = encryptionService.DecryptEncryptedValues(context, encryptedItems.Select(x => long.Parse(x.Value)).ToArray());
                foreach(var item in encryptedItems)
                {
                    result[item.IncomingPaymentHeaderId] = decryptedValues[long.Parse(item.Value)];
                }
            }
            return result;
        }
    }
}