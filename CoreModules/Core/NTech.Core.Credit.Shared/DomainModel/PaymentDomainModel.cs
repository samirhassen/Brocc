using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.DomainModel
{
    public class PaymentDomainModel
    {
        private List<AccountTransaction> transactions { get; set; }
        private IncomingPaymentHeader payment;
        private IDictionary<string, string> items;

        private PaymentDomainModel(List<AccountTransaction> transactions, IncomingPaymentHeader payment, IDictionary<string, string> items)
        {
            this.transactions = transactions;
            this.payment = payment;
            this.items = items;
        }

        public static IncomingPaymentHeaderItemCode[] AllCodes
        {
            get
            {
                return (IncomingPaymentHeaderItemCode[])Enum.GetValues(typeof(IncomingPaymentHeaderItemCode));
            }
        }

        public static List<PaymentDomainModel> CreateForAllNotFullyPlaced(ICreditContextExtended context, EncryptionService encryptionService, params IncomingPaymentHeaderItemCode[] itemsToInclude)
        {
            var itemNames = itemsToInclude.Select(x => x.ToString()).ToArray();
            var tmp = context
                .IncomingPaymentHeadersQueryable
                .Where(x => !x.IsFullyPlaced)
                .Select(x => new
                {
                    payment = x,
                    transactions = x.Transactions,
                    items = x.Items.Where(y => itemNames.Contains(y.Name))
                })
                .ToList();

            var encryptedItems = tmp.SelectMany(x => x.items.Where(y => y.IsEncrypted)).ToList();

            IDictionary<long, string> clearTexts = null;
            if (encryptedItems.Count > 0)
            {
                clearTexts = encryptionService.DecryptEncryptedValues(context, encryptedItems.Select(x => long.Parse(x.Value)).ToArray());
            }
            return tmp
                .Select(x => new PaymentDomainModel(
                    x.transactions,
                    x.payment,
                    x.items.ToDictionary(y => y.Name, y => y.IsEncrypted ? clearTexts[long.Parse(y.Value)] : y.Value)))
                .ToList();
        }

        public static PaymentDomainModel CreateForSinglePayment(int paymentId, ICreditContextExtended context, EncryptionService encryptionService, params IncomingPaymentHeaderItemCode[] itemsToInclude)
        {
            var itemNames = itemsToInclude == null ? new string[] { } : itemsToInclude.Select(x => x.ToString()).ToArray();
            var r = context
                .IncomingPaymentHeadersQueryable
                .Where(x => x.Id == paymentId)
                .Select(x => new
                {
                    payment = x,
                    transactions = x.Transactions,
                    items = x.Items.Where(y => itemNames.Contains(y.Name))
                })
                .Single();

            var encryptedItems = r.items.Where(y => y.IsEncrypted).ToList();

            IDictionary<long, string> clearTexts = null;
            if (encryptedItems.Count > 0)
            {
                clearTexts = encryptionService.DecryptEncryptedValues(context, encryptedItems.Select(x => long.Parse(x.Value)).ToArray());
            }

            return new PaymentDomainModel(
                r.transactions,
                r.payment,
                r.items.ToDictionary(y => y.Name, y => y.IsEncrypted ? clearTexts[long.Parse(y.Value)] : y.Value));
        }

        public int PaymentId
        {
            get
            {
                return payment.Id;
            }
        }

        public DateTime PaymentDate
        {
            get
            {
                return payment.TransactionDate;
            }
        }

        public string GetItem(IncomingPaymentHeaderItemCode code)
        {
            if (this.items.ContainsKey(code.ToString()))
                return this.items[code.ToString()];
            else
                return null;
        }

        public decimal GetUnplacedAmount(DateTime transactionDate)
        {
            return GetTransactions(transactionDate)
                .Where(x => x.AccountCode == TransactionAccountType.UnplacedPayment.ToString())
                .Sum(x => (decimal?)x.Amount) ?? 0m;
        }

        private IEnumerable<AccountTransaction> GetTransactions(DateTime transactionDate)
        {
            return transactions.Where(x => x.TransactionDate <= transactionDate);
        }
    }
}